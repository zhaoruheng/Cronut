using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Shapes;
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
        private string fileKey; //加密文件的密钥
        private string userKey; //加密fileKey的密钥   写错了！！！！！！！！
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

            //fileKey = FileCrypto.GetMD5(userName + salt);
            //userKey = FileCrypto.GetMD5(salt + userName);
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
                if (fname.Length > 2 && fname.Substring(0, 2) == "~$" || fextname == ".tmp") //word临时文件
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
                //MessageBox.Show(eventQueue.Count.ToString());
                we = eventQueue.Dequeue();

                if (we.fileEvent == 1)
                {
                    //MessageBox.Show("upload:" + we.filePath);
                    //UploadFileProcess(we.filePath); 不必处理新建的空文件
                    return;
                }
                if (we.fileEvent == 2)
                {
                    //处理上传文件操作
                    //MessageBox.Show("change:" + we.filePath);
                    UploadFileProcess(we.filePath);
                    return;
                }
                if (we.fileEvent == 3)
                {
                    //处理删除文件操作
                    //MessageBox.Show("delete:" + we.filePath);
                    DeleteFileProcess(System.IO.Path.GetFileName(we.filePath));
                    return;
                }
                if (we.fileEvent == 4)
                {
                    //处理重命名操作
                    //MessageBox.Show("rename:" + we.filePath);
                    RenameFileProcess(System.IO.Path.GetFileName(we.filePath), System.IO.Path.GetFileName(we.oldFilePath));
                    return;
                }
            }
        }

        /// <summary>
        /// 上传文件 和 下载文件 的同步进程
        /// </summary>
        public void SyncProcess()
        {
            Console.WriteLine("In SyncProcess:");
            GetFileListProcess();
            downFileList = fileInfoList.nameList;
            //MessageBox.Show(workPath);
            Director(workPath);

            //上传文件
            foreach (string file in upFileList)
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
            Console.WriteLine("Sync over");
        }

        /// <summary>
        /// 遍历工作目录下的所有文件
        /// </summary>
        /// <param name="dir"></param>
        private void Director(string dir) 
        {
            Console.WriteLine("Now director in " + dir);
            DirectoryInfo d = new DirectoryInfo(dir);

            FileInfo[] files = d.GetFiles();//当前文件夹下文件的名称
            DirectoryInfo[] directs = d.GetDirectories();//文件夹

            foreach (FileInfo f in files)
            {
                string tmp = f.Name.Replace(workPath, "");
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
                    int res = DateTime.Compare(cloudTime, localTime);
                    Console.WriteLine("res:" + res);
                    if (res > 0)
                    {
                        File.Delete(f.FullName);
                    }
                    else if (res < 0)
                    {
                        upFileList.Add(f.Name);
                        downFileList.Remove(f.Name);
                    }
                    else
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

            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.UPLOAD, userName, 0, null, null);
            clientComHelper.SendMsg();

            FileCrypto fc = new FileCrypto(filePath,clientComHelper,userName);

            return fc.FileUpload();
            /*
			Console.WriteLine("Upload: " + filePath);

            //new一个ClientComHelper对象
            ClientComHelper clientComHelper = new ClientComHelper(ipString, port, workPath);
            long fileSize;

            //通过文件路径找到文件，打开文件，进行读操作
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            fileSize = fs.Length;

            //获取文件MD5
            string fileMd5 = FileCrypto.GetMD5(fs);
            fs.Position = 0;

            //获取文件SHA1
            string fileSha1 = FileCrypto.GetSHA1(fs);
            fs.Close();

            //加密filekey，然后用userkey加密fileMd5
            string enMd5 = FileCrypto.AESEncryptString(fileMd5, userKey);  //加密的文件摘要
            string enKey = FileCrypto.AESEncryptString(fileKey, fileMd5);//加密访问文件的密钥
            
            //发送上传请求
            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.UPLOAD, userName, enMd5, fileSha1, enKey, fileSize, Path.GetFileName(filePath), null);
            clientComHelper.SendMsg();

            //检测服务器是否收到消息
            NetPacket np = clientComHelper.RecvMsg();
            if (np.code == NetPublic.DefindedCode.AGREEUP)
                //如果同意上传
            {
                clientComHelper.SetCryptor(fileKey);
                clientComHelper.SendFile(filePath);
            }
            */

           
            
        }

        //下载文件
        public byte DownloadFileProcess(string fileName)
        {
            string downloadPath = workPath + "/";
            ClientComHelper clientComHelper = new ClientComHelper(ipString, port, workPath);
            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.DOWNLOAD, userName, null, 0, null, fileName, null, null, 0);
            
            clientComHelper.SendMsg();

            NetPacket np = clientComHelper.RecvMsg();
            if (np.code == NetPublic.DefindedCode.FILEDOWNLOAD)
            {
                //string deMd5 = FileCrypto.AESDecryptString(np.enMd5, userKey);
                //string deKey = FileCrypto.AESDecryptString(np.enKey, deMd5);
                //clientComHelper.SetCryptor(deKey);
                string enkey = np.enKey;
                clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.READY);
                clientComHelper.SendMsg();
                clientComHelper.RecvFile(downloadPath + fileName);
                FileCrypto fc = new FileCrypto(downloadPath + fileName,clientComHelper,userName,enkey);

                fc.FileDownload();
            }

            return np.code;
        }

        //删除文件
        public byte DeleteFileProcess(string fileName)
        {
            //MessageBox.Show("DEL:" + fileName);
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
            //MessageBox.Show("RENAME:" + oldName + " to " + fileName);
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
