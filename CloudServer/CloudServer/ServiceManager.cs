using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using NetPublic;

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

        public event Action<string> RealTimeItemAdded;

        public void AddRealTimeInfoItem(string item)
        {
            RealTimeItemAdded?.Invoke(item);
        }

        public void InitProcess()
        {
            DataBaseManager dbm = new DataBaseManager(connectionString);
            dbm.InitProcess();

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
            AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  云端初始化完成");
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
                    AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  拉取用户" + np.userName + "文件列表");
                    break;

                case DefindedCode.UPLOAD:
                    DataBaseManager dm = new DataBaseManager(connectionString);
                    FileCrypto fc = new FileCrypto(serverComHelper, dm, np.userName);

                    serverComHelper.MakeResponsePacket(DefindedCode.OK);
                    serverComHelper.SendMsg();

                    try
                    {
                        fc.FileUpload();
                        AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + np.userName + "上传文件" + np.fileName + "中...");
                    }
                    catch (Exception e)
                    {
                        AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + np.userName + "上传文件" + np.fileName + "失败");
                        break;
                    }
                    break;

                case DefindedCode.DOWNLOAD:
                    DataBaseManager dm2 = new DataBaseManager(connectionString);
                    string serPath = dm2.GetFilePath(np.userName, np.fileName); //获取物理地址
                    if (serPath == "")
                    {
                        serverComHelper.MakeResponsePacket(DefindedCode.ERROR);
                        break;
                    }
                    NetPacket npD = new NetPacket();
                    npD.code = DefindedCode.FILEDOWNLOAD;
                    npD.enKey = dm2.GetEnKey(np.userName, np.fileName);  //获取解密密钥
                    serverComHelper.MakeResponsePacket(npD);

                    FileCrypto fc2 = new FileCrypto(serverComHelper, dm2, np.userName, serPath);

                    serverComHelper.SendMsg();
                    fc2.FileDownload();

                    AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + np.userName + "下载文件" + np.fileName + "完成");
                    break;

                case DefindedCode.DELETE:
                    res = DeleteRequest(np.userName, np.fileName);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + np.userName + "删除文件" + np.fileName + "完成");
                    break;

                case DefindedCode.RENAME:
                    res = RenameRequest(np.userName, np.fileName, np.newName);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + np.userName + "重命名文件" + np.fileName + "完成");
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
                AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + userName + "登录");
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
            AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + userName + "注销");
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
