using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using NetPublic;
using CloudServer.Views;
using Avalonia.Threading;
using System.Diagnostics;
using log4net;

namespace Cloud
{
    internal class ServiceManager
    {
        private TcpListener tcpListener;
        private readonly int Port;
        private readonly string serStorePath = "./ServerFiles/";
        private readonly string connectionString;
        private const int resLength = 1025;
        private const int reqLength = 256;
        public static HashSet<string> onlineUser = new();
        public event Action<string> RealTimeItemAdded;  //Real time info更新

        private static ILog log = LogManager.GetLogger("Log");

        public ServiceManager(int p, string con)  //以端口号为参数的构造函数
        {
            Port = p;
            connectionString = con;
        }

        public void AddRealTimeInfoItem(string item)
        {
            RealTimeItemAdded?.Invoke(item);
        }

        public void InitProcess()
        {
            DataBaseManager dbm = new(connectionString);
            dbm.InitProcess();

            DirectoryInfo dir = new(serStorePath);
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (FileSystemInfo i in fileinfo)
            {
                if (i is DirectoryInfo)
                {
                    DirectoryInfo subdir = new(i.FullName);
                    subdir.Delete(true);
                }
                else
                {
                    File.Delete(i.FullName);
                }
            }
            AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  云端初始化完成");

            log.Info("云端初始化完成");
        }

        public void Start()   //启动
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.IPv6Any, Port);
                tcpListener.Start();
                Thread th = new(ListenTh);  //分出负责监听连接的线程
                th.IsBackground = true;
                th.Start();
            }
            catch (Exception ex)
            {
                //MessageBox.Show("启动失败，检查端口是否被占用。错误信息： \r\n" + ex.ToString());
                log.Error("启动失败： " + ex.ToString());
            }
        }

        private void ListenTh()  //监听连接
        {
            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();

                if (client.Connected)
                {
                    Thread recvTh = new(RecvClient);  //为每个客户单独分出线程处理
                    recvTh.IsBackground = true;
                    recvTh.Start(client);
                    recvTh.Join();
                }
            }
        }

        private void ClearDetailedParaList()
        {
            Dispatcher.UIThread.Post(MainWindow.lb.Items.Clear);
        }

        private void RecvClient(object obj)
        {
            TcpClient client = obj as TcpClient;
            ServerComHelper serverComHelper = new(client);

            NetPacket np = serverComHelper.RecvMsg();

            if (!CheckLogin(np.userName) && np.code != DefindedCode.LOGIN && np.code != DefindedCode.SIGNUP)
            {
                serverComHelper.MakeResponsePacket(DefindedCode.UNLOGIN); //没有登录
                serverComHelper.SendMsg();
                return;
            }

            byte res;
            switch (np.code)
            {
                case DefindedCode.SIGNUP:
                    res = SignUpRequest(np.userName, np.password);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    break;

                case DefindedCode.LOGIN:
                    res = LoginRequest(np.userName, np.password);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    break;

                case DefindedCode.LOGOUT:
                    res = LogoutRequest(np.userName);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    ClearDetailedParaList();    //每次登出清空Detailed Alg Para列表
                    break;

                case DefindedCode.GETLIST:
                    serverComHelper.SendFileList(GetListRequest(np.userName));
                    AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  拉取用户" + np.userName + "文件列表");
                    log.Info("拉取用户" + np.userName + "文件列表");
                    break;

                case DefindedCode.UPLOAD:
                    DataBaseManager dm = new(connectionString);

                    AddDetailedParaItem("---文件名：" + np.fileName + "---");
                    FileCrypto fc = new(serverComHelper, dm, np.userName);

                    serverComHelper.MakeResponsePacket(DefindedCode.OK);
                    serverComHelper.SendMsg();

                    try
                    {
                        fc.FileUpload();
                        AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + np.userName + "上传文件" + np.fileName + "完成");
                        log.Info("用户" + np.userName + "上传文件" + np.fileName + "完成");
                    }
                    catch (Exception e)
                    {
                        AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + np.userName + "上传文件" + np.fileName + "失败");
                        log.Error("用户" + np.userName + "上传文件" + np.fileName + "失败");
                        Console.WriteLine(e);
                        break;
                    }
                    break;

                case DefindedCode.DOWNLOAD:
                    DataBaseManager dm2 = new(connectionString);
                    string serPath = dm2.GetFilePath(np.userName, np.fileName); //获取物理地址
                    if (serPath == "")
                    {
                        serverComHelper.MakeResponsePacket(DefindedCode.ERROR);
                        break;
                    }
                    NetPacket npD = new();
                    npD.code = DefindedCode.FILEDOWNLOAD;
                    npD.enKey = dm2.GetEnKey(np.userName, np.fileName);  //获取解密密钥
                    serverComHelper.MakeResponsePacket(npD);

                    FileCrypto fc2 = new(serverComHelper, dm2, np.userName, serPath);

                    serverComHelper.SendMsg();
                    fc2.FileDownload();

                    AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + np.userName + "下载文件" + np.fileName + "完成");
                    log.Info("用户" + np.userName + "下载文件" + np.fileName + "完成");
                    break;

                case DefindedCode.DELETE:
                    res = DeleteRequest(np.userName, np.fileName);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + np.userName + "删除文件" + np.fileName + "完成");
                    log.Info("用户" + np.userName + "删除文件" + np.fileName + "完成");
                    break;

                case DefindedCode.RENAME:
                    res = RenameRequest(np.userName, np.fileName, np.newName);
                    serverComHelper.MakeResponsePacket(res);
                    serverComHelper.SendMsg();
                    AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + np.userName + "重命名文件" + np.fileName + "完成");
                    log.Info("用户" + np.userName + "重命名文件" + np.fileName + "完成");
                    break;

                default:
                    break;
            }

        }

        private byte SignUpRequest(string userName,string userPass)
        {
            DataBaseManager dm = new(connectionString);
            int res = dm.CreateUser(userName, userPass, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            if (res == DefindedCode.OK)
            {
                return DefindedCode.SIGNUPSUCCESS;
            }
            else
            {
                return DefindedCode.ERROR;
            }
        }

        private byte LoginRequest(string userName, string userPass)
        {
            DataBaseManager dm = new(connectionString);
            int result = dm.LoginAuthentication(userName, userPass);

            if (result > 0&&CheckLogin(userName)==false)  //登录成功
            {
                _ = onlineUser.Add(userName);
                AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + userName + "登录");
                log.Info("用户" + userName + "登录");
                dm.UpdateUserLoginTime(userName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                return DefindedCode.LOGSUCCESS;
            }
            else if(CheckLogin(userName)==true)
            {
                return DefindedCode.ERROR ;
            }
            else if (result == 0)
            {
                return DefindedCode.PASSERROR;
            }
            else
            {
                return DefindedCode.USERMISS;
            }
        }

        //检查是否有userName这个用户
        private bool CheckLogin(string userName)
        {
            return onlineUser.Contains(userName);
        }

        private byte LogoutRequest(string userName)
        {
            _ = onlineUser.Remove(userName);
            AddRealTimeInfoItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ":  用户" + userName + "注销");
            log.Info("用户" + userName + "注销");
            return DefindedCode.OK;
        }

        //后端：和数据库交互，获得该用户的文件列表
        private FileInfoList GetListRequest(string userName)
        {
            DataBaseManager dm = new(connectionString);
            FileInfoList result = dm.GetFileList(userName);
            return result;
        }

        private byte DownloadRequest(string userName, string userFile, ref string serFile, ref string enKey, ref string enMd5)
        {
            DataBaseManager dm = new(connectionString);

            //获取云端的物理地址
            string serPath = dm.GetFilePath(userName, userFile);
            if (serPath == "")
            {
                return DefindedCode.ERROR;
            }

            serFile = serPath;
            enKey = dm.GetEnKey(userName, userFile);
            enMd5 = dm.GetEnMd5(userName, userFile);
            return DefindedCode.FILEDOWNLOAD;
        }

        private byte DeleteRequest(string userName, string userFile)
        {
            DataBaseManager dm = new(connectionString);
            return dm.RemoveFile(userName, userFile) > 0 ? DefindedCode.OK : DefindedCode.DENIED;
        }
        private byte RenameRequest(string userName, string fileName, string newName)
        {
            DataBaseManager dm = new(connectionString);
            return dm.RenameFile(userName, fileName, newName) > 0 ? DefindedCode.OK : DefindedCode.DENIED;
        }

        //前端：Detailed Alg Para列表更新
        private void AddDetailedParaItem(string str)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _ = MainWindow.lb.Items.Add(str);
            });
        }
    }
}
