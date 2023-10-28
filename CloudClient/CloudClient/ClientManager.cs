using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
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
        //WatchEvent是一个类，在FileWatcher里定义
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
                ////MessageBox.Show(eventQueue.Count.ToString());
                we = eventQueue.Dequeue();
                //如果文件名以.开头，直接return
                if (System.IO.Path.GetFileName(we.filePath).Substring(0, 1) == "." || protect == true)
                {
                    return;
                }
                if (we.fileEvent == 1)
                {
                    //MessageBox.Show("watch event: upload:" + we.filePath);
                    //UploadFileProcess(we.filePath); 不必处理新建的空文件
                    return;
                }
                if (we.fileEvent == 2)
                {
                    //处理上传文件操作
                    //MessageBox.Show("watch event: change:" + we.filePath);
                    UploadFileProcess(we.filePath);
                    return;
                }
                if (we.fileEvent == 3)
                {
                    //处理删除文件操作
                    //MessageBox.Show("watch event: delete:" + we.filePath);
                    DeleteFileProcess(System.IO.Path.GetFileName(we.filePath));
                    return;
                }
                if (we.fileEvent == 4)
                {
                    //处理重命名操作
                    //MessageBox.Show("watch event: rename:" + we.filePath);
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

        /// <summary>
        /// 上传文件 和 下载文件 的同步进程
        /// </summary>
        public void SyncProcess()
        {
            protect = true;
            Console.WriteLine("In SyncProcess:");
            GetFileListProcess();   //该用户现有的文件列表，存入fileInfoList

            downFileList = fileInfoList.nameList;   //获取downFileList

            Director(workPath);     //获取upFileList

            //上传文件
            foreach (string file in upFileList) //upFileList为指定文件夹下的所有文件
            {
                Console.WriteLine("upFile:" + file);
                UploadFileProcess(workPath + "/" + file);
            }

            //下载文件
            foreach (string file in downFileList)
            {
                Console.WriteLine("downFile:" + file);
                DownloadFileProcess(file);
            }

            //MessageBox.Show("上传和下载文件结束！");
            Console.WriteLine("Sync over");
            protect = false;
        }


        //遍历指定文件夹下的所有文件和子文件夹
        private void Director(string dir)
        {
            Console.WriteLine("Now director in " + dir);
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
                        //File.Delete(f.FullName);
                        //MessageBox.Show("云端新，删除" + f.FullName);
                    }
                    else if (res < 0) //如果 res 小于0，本地的文件新
                    {
                        upFileList.Add(f.Name);
                        downFileList.Remove(f.Name);
                        //MessageBox.Show("本地新，上传" + f.FullName);
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

            //通信：给服务器发送登录请求
            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.LOGIN, userName, md5, 0, null, null, null, null, 0);
            clientComHelper.SendMsg();

            //通信：接收服务器的登录响应
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

            //通信：请求获取用户文件列表
            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.GETLIST, userName, null, 0, null, null, null, null, 0);
            clientComHelper.SendMsg();

            //通信：接收服务器发来的用户文件列表
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

            //通信：发送上传文件请求
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

            //通信：发送下载文件请求
            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.DOWNLOAD, userName, null, 0, null, fileName, null, null, 0);
            //MessageBox.Show("下载" + fileName);
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

            //MessageBox.Show("此时的np.code:"+np.code);
            return np.code;
        }

        //删除文件
        public byte DeleteFileProcess(string fileName)
        {
            ////MessageBox.Show("DEL:" + fileName);
            //MessageBox.Show("删除" + fileName);
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
            ////MessageBox.Show("RENAME:" + oldName + " to " + fileName);
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

        //public int UploadFileProcess(string filePath)
        //{
        //	byte[] requestMsg = new byte[256];
        //	if (!File.Exists(filePath))
        //		return 1;   
        //	FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        //	if (fs.Length == 0)
        //	{
        //		fs.Close();
        //		return 2; //文件为空，上传取消
        //	}	
        //	MD5 md5 = new MD5CryptoServiceProvider();
        //	byte[] fileMd5 = md5.ComputeHash(fs);
        //	fs.Close();
        //	string fileName = Path.GetFileName(filePath);
        //	string enFileName = string.Empty;
        //	foreach (var i in fileMd5)
        //		enFileName += i.ToString("x2");
        //	string enFilePath = "./tmp/" + enFileName;
        //	FileCrypto fc = new FileCrypto(enFilePath, filePath, "admin");
        //	fc.FileEncrypt();
        //	FileStream enFs = new FileStream(enFilePath, FileMode.Open, FileAccess.Read);
        //	long fsize = enFs.Length;
        //	enFs.Close();
        //	requestMsg[0] = 0x73;
        //	{
        //		byte[] tmp = Encoding.Default.GetBytes(userName);
        //		Buffer.BlockCopy(BitConverter.GetBytes(tmp.Length), 0, requestMsg, 1, 4);
        //		Buffer.BlockCopy(tmp, 0, requestMsg, 5, tmp.Length);
        //		Buffer.BlockCopy(fileMd5, 0, requestMsg, 15, 16);
        //	}
        //	{
        //		byte[] tmp = Encoding.Default.GetBytes(fileName);
        //		Buffer.BlockCopy(BitConverter.GetBytes(fsize), 0, requestMsg, 31, 8);
        //		Buffer.BlockCopy(BitConverter.GetBytes(tmp.Length), 0, requestMsg, 39, 4);
        //		Buffer.BlockCopy(tmp, 0, requestMsg, 43, tmp.Length);
        //	}
        //	//byte res = SendRequestMsgForUp(requestMsg, enFilePath);
        //	File.Delete(enFilePath);
        //	//if (res == 0x60)
        //	//	//上传成功
        //	//	return 1;
        //	//if (res == 0x83)
        //	//	//需要登录
        //	//	return 0;
        //	return -1;  //未知错误

        //}

        //public int DownloadFileProcess(string fileName)
        //{
        //	byte[] requestMsg = new byte[256];
        //	requestMsg[0] = 0x74;
        //	{
        //		byte[] tmp = Encoding.Default.GetBytes(userName);
        //		Buffer.BlockCopy(BitConverter.GetBytes(tmp.Length), 0, requestMsg, 1, 4);
        //		Buffer.BlockCopy(tmp, 0, requestMsg, 5, tmp.Length);
        //	}
        //	{
        //		byte[] tmp = Encoding.Default.GetBytes(fileName);
        //		Buffer.BlockCopy(BitConverter.GetBytes(tmp.Length), 0, requestMsg, 39, 4);
        //		Buffer.BlockCopy(tmp, 0, requestMsg, 43, tmp.Length);
        //	}
        //	string deFilePath = "./DownloadFiles/" + fileName;
        //	//byte res = SendRequestMsgForDown(requestMsg, deFilePath);
        //	//if (res == 0x62) //下载完成
        //	//	return 1;
        //	//if (res == 0x83) //需要登录
        //	//	return 0;
        //	return 0;
        //}

        //public int DeleteFileProcess(string fileName)
        //{
        //	byte[] requestMsg = new byte[256];
        //	requestMsg[0] = 0x75;
        //	{
        //		byte[] tmp = Encoding.Default.GetBytes(userName);
        //		Buffer.BlockCopy(BitConverter.GetBytes(tmp.Length), 0, requestMsg, 1, 4);
        //		Buffer.BlockCopy(tmp, 0, requestMsg, 5, tmp.Length);
        //	}
        //	{
        //		byte[] tmp = Encoding.Default.GetBytes(fileName);
        //		Buffer.BlockCopy(BitConverter.GetBytes(tmp.Length), 0, requestMsg, 39, 4);
        //		Buffer.BlockCopy(tmp, 0, requestMsg, 43, tmp.Length);
        //	}
        //	//byte res = SendRequestMsg(requestMsg);
        //	//if (res == 0x90)
        //	//	return 1;
        //	//if (res == 0x83)
        //	//	return 0;
        //	return -1;

        //}

        //public int RenameFileProcess(string fileName, string oldFileName)
        //{
        //	byte[] requestMsg = new byte[256];
        //	{
        //		byte[] tmp = Encoding.Default.GetBytes(userName);
        //		requestMsg[0] = 0x76;
        //		Buffer.BlockCopy(BitConverter.GetBytes(tmp.Length), 0, requestMsg, 1, 4);
        //		Buffer.BlockCopy(tmp, 0, requestMsg, 5, tmp.Length);
        //	}
        //	int len;
        //	{
        //		byte[] tmp = Encoding.Default.GetBytes(fileName);
        //		len = tmp.Length;
        //		Buffer.BlockCopy(BitConverter.GetBytes(tmp.Length), 0, requestMsg, 31, 4);
        //		Buffer.BlockCopy(tmp, 0, requestMsg, 35, tmp.Length);
        //	}
        //	{
        //		byte[] tmp = Encoding.Default.GetBytes(oldFileName);
        //		Buffer.BlockCopy(BitConverter.GetBytes(tmp.Length), 0, requestMsg, 35 + len, 4);
        //		Buffer.BlockCopy(tmp, 0, requestMsg, 39 + len, tmp.Length);
        //	}
        //	//byte res = SendRequestMsg(requestMsg);
        //	//if (res == 0x90)
        //	//	return 1; //操作成功
        //	//if (res == 0x83)
        //	//	return 0; //需要登录
        //	return -1; // 未知错误
        //}

        //public int CreateEmptyFileProcess(string filePath)
        //{
        //	byte[] requestMsg = new byte[256];
        //	string fileName = Path.GetFileName(filePath);
        //	requestMsg[0] = 0x73;
        //	{
        //		byte[] tmp = Encoding.Default.GetBytes(fileName);
        //		Buffer.BlockCopy(BitConverter.GetBytes((long)0), 0, requestMsg, 31, 8);
        //		Buffer.BlockCopy(BitConverter.GetBytes(tmp.Length), 0, requestMsg, 39, 4);
        //		Buffer.BlockCopy(tmp, 0, requestMsg, 43, tmp.Length);
        //	}
        //	byte res = SendRequestMsg(requestMsg);
        //	if (res == 0x90)
        //		return 1;
        //	if (res == 0x83)
        //		return 0;
        //	return -1;
        //}
    }
}
