using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using log4net;
using NetPublic;

namespace Cloud
{
    public class ClientManager
    {
        public string workPath;
        public delegate void DelegateEventHander();
        public DelegateEventHander ReturnMsg;

        private Queue<WatchEvent> eventQueue;
        private string userName;
        private string salt;
        private int port;
        private string ipString;
        private bool protect = false;
        List<string> upFileList;    //上传文件列表
        List<string> downFileList;  //下载文件列表
        FileInfoList fileInfoList;
        private static ILog log = LogManager.GetLogger("Log");

        //构造函数
        public ClientManager(string ip, int p)
        {
            port = p;
            ipString = ip;
            workPath = ConfigurationManager.AppSettings["TargetDir"].ToString();
            salt = ConfigurationManager.AppSettings["Salt"].ToString();
            if (!Directory.Exists(workPath))
                workPath = string.Empty;

            eventQueue = new Queue<WatchEvent>();
            System.Timers.Timer t = new System.Timers.Timer(2000);
            t.Elapsed += new System.Timers.ElapsedEventHandler(HandleQueue);
            t.AutoReset = true;
            t.Enabled = true;

            upFileList = new List<string>();
            downFileList = new List<string>();
        }

        public void AnalysesEvent(object sender, WatchEvent we)
        {
            if (we.fileEvent == 0)
                return;
            if (we.fileEvent != 3) //如果是删除操作，文件可能已经不存在
            {
                string fname = System.IO.Path.GetFileName(we.filePath);
                string fextname = System.IO.Path.GetExtension(we.filePath);
                if (fname.Length > 2 && fname.Substring(0, 2) == "~$" || fextname == ".tmp" || fname.Substring(0, 1) == ".") //word临时文件
                    return;
                if (File.GetAttributes(we.filePath) == FileAttributes.Hidden) //隐藏文件
                    return;
                FileInfo fi = new FileInfo(we.filePath);
                if (fi.Length == 0)  //空文件不做处理
                    return;
            }
            eventQueue.Enqueue(we);
        }

        //处理一系列对文件的操作：上传、删除、重命名
        private void HandleQueue(object source, System.Timers.ElapsedEventArgs e)
        {
            if (eventQueue.Count <= 0)
                return;
            WatchEvent we;
            while (eventQueue.Count > 0)
            {
                we = eventQueue.Dequeue();
                //如果文件名以.开头，直接return
                if (System.IO.Path.GetFileName(we.filePath).Substring(0, 1) == "." || protect == true)
                {
                    return;
                }
                if (we.fileEvent == 1)
                {
                    return;
                }
                if (we.fileEvent == 2)
                {
                    //处理上传文件操作
                    UploadFileProcess(we.filePath);
                    return;
                }
                if (we.fileEvent == 3)
                {
                    //处理删除文件操作
                    DeleteFileProcess(System.IO.Path.GetFileName(we.filePath));
                    return;
                }
                if (we.fileEvent == 4)
                {
                    //处理重命名操作
                    RenameFileProcess(System.IO.Path.GetFileName(we.filePath), System.IO.Path.GetFileName(we.oldFilePath));
                    return;
                }
            }
        }

        public List<string> GetUpFileList()
        {
            foreach (string fileName in fileInfoList.nameList)
            {
                if (upFileList.Contains(fileName))
                {
                    upFileList.Remove(fileName);
                }
            }
            return upFileList;
        }

        public void SyncProcess()
        {
            protect = true;
            GetFileListProcess();   //该用户现有的文件列表，存入fileInfoList

            downFileList = fileInfoList.nameList;   //获取downFileList

            Director(workPath);     //获取upFileList

            //上传文件
            foreach (string file in upFileList) //upFileList为指定文件夹下的所有文件
            {
                UploadFileProcess(workPath + "/" + file);
            }

            //下载文件
            foreach (string file in downFileList)
            {
                DownloadFileProcess(file);
            }
            protect = false;
        }


        //遍历指定文件夹下的所有文件和子文件夹
        private void Director(string dir)
        {
            DirectoryInfo d = new DirectoryInfo(dir);

            FileInfo[] files = d.GetFiles();//当前文件夹下文件的名称
            DirectoryInfo[] directs = d.GetDirectories();//文件夹

            foreach (FileInfo f in files)
            {
                string tmp = f.Name.Replace(workPath, "");
                if (tmp.Substring(0, 1) == ".")
                {
                    continue;
                }
                int index;
                if ((index = fileInfoList.nameList.IndexOf(tmp)) < 0)
                {
                    upFileList.Add(tmp);
                    downFileList.Remove(tmp);
                }
                else
                {
                    FileInfo fi = new FileInfo(f.FullName);
                    string localT = fi.LastWriteTime.ToString();
                    DateTime cloudTime = Convert.ToDateTime(fileInfoList.upTimeList[index]);
                    DateTime localTime = Convert.ToDateTime(localT);
                    int res = DateTime.Compare(cloudTime, localTime);   //比较同一份文件的云端时间和本地时间

                    Console.WriteLine("res:" + res);

                    if (res > 0) //如果 res 大于0，表示cloudTime> localTime，云端是新的文件
                    {
                        
                    }
                    else if (res < 0) //如果 res 小于0，本地的文件新
                    {
                        upFileList.Add(f.Name);
                        downFileList.Remove(f.Name);
                    }
                    else //如果 res 等于0，表示两者时间相同
                        downFileList.Remove(f.Name);
                }
            }

            //获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo dd in directs)
            {
                Director(dd.FullName);
            }
        }

        //用户登录
        public byte LoginProcess(string userName, string userPass)
        {
            this.userName = userName;
            ClientComHelper clientComHelper = new ClientComHelper(ipString, port, workPath);
            string md5 = FileCrypto_2.GetMD5(userPass);

            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.LOGIN, userName, md5, 0, null, null, null, null, 0);
            clientComHelper.SendMsg();

            NetPacket np = clientComHelper.RecvMsg();
            return np.code;
        }

        public byte SignUpProcess(string userName,string userPass)
        {
            this.userName = userName;
            ClientComHelper clientComHelper = new ClientComHelper(ipString, port, workPath);
            string md5 = FileCrypto_2.GetMD5(userPass);

            //给服务器发送注册请求
            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.SIGNUP, userName, md5, 0, null, null, null, null, 0);
            clientComHelper.SendMsg();

            NetPacket np = clientComHelper.RecvMsg();
            return np.code;
        }

        //用户登出
        public byte LogoutProcess()
        {
            ClientComHelper clientComHelper = new ClientComHelper(ipString, port, workPath);
            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.LOGOUT, userName, null, 0, null, null, null, null, 0);
            clientComHelper.SendMsg();
            NetPacket np = clientComHelper.RecvMsg();
            clientComHelper = null;
            return np.code;
        }

        public void GetFileListProcess()
        {
            ClientComHelper clientComHelper = new ClientComHelper(ipString, port, workPath);

            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.GETLIST, userName, null, 0, null, null, null, null, 0);
            clientComHelper.SendMsg();

            fileInfoList = clientComHelper.RecvFileList();
        }

        public List<string> GetFileNameList()
        {
            List<string> fileNameList = new List<string>();
            GetFileListProcess();
            foreach (string i in fileInfoList.nameList)
                fileNameList.Add(i);
            return fileNameList;
        }

        //上传文件
        public byte UploadFileProcess(string filePath)
        {
            ClientComHelper clientComHelper = new ClientComHelper(ipString, port, workPath);

            string fileName = Path.GetFileName(filePath);
            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.UPLOAD, userName, 0, fileName, null);
            clientComHelper.SendMsg();
            NetPacket np = clientComHelper.RecvMsg();
            FileCrypto fc = new FileCrypto(filePath, clientComHelper, userName);

            //返回DefindedCode.AGREEUP 或 DefineCode.FILEEXISTED
            return fc.FileUpload();
        }

        //下载文件
        public byte DownloadFileProcess(string fileName)
        {
            string downloadPath = workPath + "/";
            ClientComHelper clientComHelper = new ClientComHelper(ipString, port, workPath);

            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.DOWNLOAD, userName, null, 0, null, fileName, null, null, 0);
            clientComHelper.SendMsg();

            //通信：接收解密密钥
            NetPacket np = clientComHelper.RecvMsg();

            if (np.code == NetPublic.DefindedCode.FILEDOWNLOAD)
            {
                string enkey = np.enKey;

                //通信：接收密文
                clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.READY);
                clientComHelper.SendMsg();

                clientComHelper.RecvFile(downloadPath + "." + fileName);
                FileCrypto fc = new FileCrypto(downloadPath + fileName, downloadPath + "." + fileName, clientComHelper, userName, enkey);

                fc.FileDownload();
            }
            return np.code;
        }

        //删除文件
        public byte DeleteFileProcess(string fileName)
        {
            ClientComHelper clientComHelper = new ClientComHelper(ipString, port, workPath);
            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.DELETE, userName, null, 0, null, fileName, null, null, 0);
            clientComHelper.SendMsg();
            NetPacket np = clientComHelper.RecvMsg();
            ReturnMsg?.Invoke();
            return np.code;
        }

        //重命名
        public byte RenameFileProcess(string fileName, string oldName)
        {
            ClientComHelper clientComHelper = new ClientComHelper(ipString, port, workPath);
            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.RENAME, userName, null, 0, null, oldName, fileName, null, 0);
            clientComHelper.SendMsg();
            NetPacket np = clientComHelper.RecvMsg();
            ReturnMsg?.Invoke();
            return np.code;
        }

        public string getusername()
        {
            return userName;
        }
    }
}
