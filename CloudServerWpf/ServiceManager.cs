using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;
using System.IO;
using NetPublic;
using System.Windows;
using System.Windows.Shapes;
using System.Security.Cryptography;

namespace Cloud
{
    class ServiceManager
    {
        private TcpListener tcpListener;
        private int Port;
        private string serStorePath = "./ServerFiles/";
        string connectionString;
        const int resLength = 1025;
        const int reqLength = 256;
        public HashSet<string> onlineUser;
        public delegate void ReturnMsgDelegate(string val);
        public ReturnMsgDelegate ReturnMsg;

        public ServiceManager(int p, string con)  //以端口号为参数的构造函数
        {
            onlineUser = new HashSet<string>();
            Port = p;
            connectionString = con;
        }

        public void InitProcess()
        {
            //删除原有数据库
            DataBaseManager dbm = new DataBaseManager(connectionString);
            dbm.InitProcess();

            //删除云端物理地址下的所有文件和子目录
            DirectoryInfo dir = new DirectoryInfo(serStorePath);
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (FileSystemInfo i in fileinfo)
            {
                if (i is DirectoryInfo)
                {
                    DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                    subdir.Delete(true);
                }
                else
                {
                    File.Delete(i.FullName);
                }
            }

            ReturnMsg?.Invoke("云端初始化完成");
        }

        public void Start()   //启动
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.IPv6Any, Port);
                tcpListener.Start();
                Thread th = new Thread(ListenTh);  //分出负责监听连接的线程
                th.IsBackground = true;
                th.Start();
            }
            catch (Exception ex)
            {
                //MessageBox.Show("启动失败，检查端口是否被占用。错误信息： \r\n" + ex.ToString());
            }
        }

        private void ListenTh()  //监听连接
        {
            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();

                if (client.Connected)
                {
                    Thread recvTh = new Thread(RecvClient);  //为每个客户单独分出线程处理
                    recvTh.IsBackground = true;
                    recvTh.Start(client);
                }
            }
        }

        private void RecvClient(object obj)
        {
            TcpClient client = obj as TcpClient;
            ServerComHelper serverComHelper = new ServerComHelper(client);

            //通信：接收客户端发来的请求
            NetPacket np = serverComHelper.RecvMsg();

            //没有这个用户 && 不是登录操作
            if (!CheckLogin(np.userName) && np.code != DefindedCode.LOGIN)
            {
                serverComHelper.MakeResponsePacket(DefindedCode.UNLOGIN); //没有登录
                serverComHelper.SendMsg();
                return;
            }

            byte res;
            switch (np.code)
            {
                case DefindedCode.LOGIN:
                    res = LoginRequest(np.userName, np.password);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    break;

                case DefindedCode.LOGOUT:
                    res = LogoutRequest(np.userName);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    break;

                case DefindedCode.GETLIST:
                    serverComHelper.SendFileList(GetListRequest(np.userName));
                    ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + np.userName + "拉取文件列表");
                    break;

                case DefindedCode.UPLOAD:
                    DataBaseManager dm = new DataBaseManager(connectionString);
                    FileCrypto fc = new FileCrypto(serverComHelper,dm,np.userName);

                    fc.FileUpload();
                    ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + np.userName + "上传文件");
                    /*需要修改！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！*/
                    //res = UploadRequest(np.userName, np.fileName, np.fileLength, np.enMd5, np.sha1, np.uploadTime, np.enKey);
                    ////UploadRequest判断是否重复

                    //serverComHelper.MakeResponsePacket(res);
                    //serverComHelper.SendMsg();
                    //if (res == DefindedCode.AGREEUP)
                    //{
                    //    ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ":接收来自" + np.userName + "的文件");
                    //    serverComHelper.RecvFile(np.sha1);
                    //}
                    //else if (res == DefindedCode.FILEEXISTED)
                    //    ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ":" + np.userName + "上传文件已做去重处理");
                    //ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ":" + np.userName + "上传*" + np.fileName + "*成功");

                    break;
                case DefindedCode.DOWNLOAD:
                    DataBaseManager dm2 = new DataBaseManager(connectionString);
                    string serPath = dm2.GetFilePath(np.userName, np.fileName); //获取物理地址
                    if (serPath == "")
                    {
                        serverComHelper.MakeResponsePacket(DefindedCode.ERROR);
                        break;
                    }

                    NetPacket npD= new NetPacket();
                    npD.code = DefindedCode.FILEDOWNLOAD;
                    npD.enKey= dm2.GetEnKey(np.userName, np.fileName);  //获取解密密钥
                    serverComHelper.MakeResponsePacket(npD);

                    FileCrypto fc2 = new FileCrypto(serverComHelper, dm2, np.userName,serPath);

                    serverComHelper.SendMsg();
                    fc2.FileDownload();

                    ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ":" + np.userName + "下载完成");

                    /*不是很懂！*/
                    //string serFilePath = string.Empty;
                    //string enKey = string.Empty;
                    //string enMd5 = string.Empty;
                    //ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ":" + np.userName + "请求下载*" + np.fileName + "*");
                    //res = DownloadRequest(np.userName, np.fileName, ref serFilePath, ref enKey, ref enMd5);
                    //serverComHelper.MakeResponsePacket(res, enKey, enMd5);
                    //serverComHelper.SendMsg(); //发送文件解密的密钥，发送文件被用户加密后的摘要
                    //serverComHelper.RecvMsg();
                    ///*不懂SendFile！！！！！！！！！！！！！！！！！！！*/
                    //serverComHelper.SendFile(serFilePath);
                    //ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ":" + np.userName + "下载完成");
                    break;

                case DefindedCode.DELETE:
                    res = DeleteRequest(np.userName, np.fileName);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ":" + np.userName + "删除");
                    break;

                case DefindedCode.RENAME:
                    res = RenameRequest(np.userName, np.fileName, np.newName);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ":" + np.userName + "重命名");
                    break;
                default:
                    break;
            }

        }

        //通信：响应客户端的登录请求
        private byte LoginRequest(string userName, string userPass)
        {
            DataBaseManager dm = new DataBaseManager(connectionString);
            int result = dm.LoginAuthentication(userName, userPass);
            if (result > 0)  //登录成功
            {
                onlineUser.Add(userName);
                ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ": " + userName + "登录");
                return DefindedCode.LOGSUCCESS;
            }
            else if (result == 0)  //密码错误
                return DefindedCode.PASSERROR;
            else  //用户不存在
                return DefindedCode.USERMISS;
        }

        //检查是否有userName这个用户
        private bool CheckLogin(string userName)
        {
            return onlineUser.Contains(userName);
        }

        private byte LogoutRequest(string userName)
        {
            onlineUser.Remove(userName);
            ReturnMsg?.Invoke(DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss") + ":" + userName + " 用户注销");
            return DefindedCode.OK;
        }

        //后端：和数据库交互，获得该用户的文件列表
        private FileInfoList GetListRequest(string userName)
        {
            DataBaseManager dm = new DataBaseManager(connectionString);
            FileInfoList result = dm.GetFileList(userName);
            return result;
        }

        //private byte UploadRequest(string userName, string userFile, long fileSize, string enMd5, string sha1, string uploadTime, string enKey)
        //{
        //    DataBaseManager dm = new DataBaseManager(connectionString);
        //    int status = dm.InsertFile(userFile, fileSize, userName, enMd5, sha1, uploadTime, enKey);
        //    if (status == 1)
        //        return DefindedCode.AGREEUP;
        //    return DefindedCode.FILEEXISTED;
        //}

        //serFile是服务器的物理路径
        private byte DownloadRequest(string userName, string userFile, ref string serFile, ref string enKey, ref string enMd5)
        {
            DataBaseManager dm = new DataBaseManager(connectionString);

            //获取云端的物理地址
            string serPath = dm.GetFilePath(userName, userFile);
            if (serPath == "")
                return DefindedCode.ERROR;
            serFile = serPath;
            enKey = dm.GetEnKey(userName, userFile);
            enMd5 = dm.GetEnMd5(userName, userFile);
            return DefindedCode.FILEDOWNLOAD;
        }

        private byte DeleteRequest(string userName, string userFile)
        {
            DataBaseManager dm = new DataBaseManager(connectionString);
            if (dm.RemoveFile(userName, userFile) > 0)
                return DefindedCode.OK;
            return DefindedCode.DENIED;
        }
        private byte RenameRequest(string userName, string fileName, string newName)
        {
            DataBaseManager dm = new DataBaseManager(connectionString);
            if (dm.RenameFile(userName, fileName, newName) > 0)
                return DefindedCode.OK;
            return DefindedCode.DENIED;
        }
    }
}

