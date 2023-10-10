using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using NetPublic;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Cloud
{
    class ClientComHelper : Communication
        //Communication是抽象类
        //ClientComHelper继承了Communication抽象类
    {
        IPAddress targetIP;
        FileCrypto_2 fc;
        int targetPort;
        string workPath;

        /// <summary>
        /// 构造函数：
        /// 给成员变量workPath，targetIP,targetPort赋值
        /// </summary>
        /// <param name="ipStr"></param>
        /// <param name="port"></param>
        /// <param name="workPath"></param>
        public ClientComHelper(string ipStr, int port, string workPath) : base()
        {
            this.workPath = workPath;
            tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
            targetIP = IPAddress.Parse(ipStr);  //将string类型的IP地址转化成IPAdress实例
            targetPort = port;

            tcpClient.Connect(targetIP, targetPort);    //客户端连接 IP地址+端口
            nstream = tcpClient.GetStream();    //获得用于发送和接收数据的NetworkStream
        }

        //析构函数
        ~ClientComHelper()
        {
            tcpClient.Close();  //断开连接
        }

        /// <summary>
        /// 对应2个操作：
        /// 登录
        /// 上传文件
        /// 需要对ChangeTime进行更新
        /// </summary>
        /// <param name="code"></param>
        /// <param name="userName"></param>
        /// <param name="md5"></param>
        /// <param name="sha1"></param>
        /// <param name="enkey"></param>
        /// <param name="fileLength"></param>
        /// <param name="fileName"></param>
        /// <param name="newName"></param>
        public void MakeRequestPacket(byte code, string userName, string password, long fileLength, string fileTag, string fileName, string newName, string uploadTime, long userType,string enkey=null)
        {
            FileInfo fi = new FileInfo(workPath + "/" + fileName);

            string changeTime = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

            np = new NetPacket(code, userName, password, fileLength, fileTag, fileName,newName, changeTime, userType);
            if(enkey != null)
                np.enKey = enkey;
            //np.enKey = enkey;
        }
        public void MakeRequestPacket(NetPacket np)
        {
            this.np = np;
        }

        //函数重载

        /// <summary>
        /// 对应5个Request:
        /// 登出
        /// 获取文件List
        /// 下载
        /// 删除
        /// 重命名
        /// </summary>
        /// <param name="code"></param>
        /// <param name="userName"></param>
        /// <param name="fileLength"></param>
        /// <param name="fileName"></param>
        /// <param name="newName"></param>
        public void MakeRequestPacket(byte code, string userName, long fileLength, string fileName, string newName)
        {
            FileInfo fi = new FileInfo(workPath + "/" + fileName);
            string changeTime = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

            np = new NetPacket(code, userName, null, fileLength, null,fileName, newName, null, 0);
            //将enMD5和SHA1置为null
        }

        //初始化
        public void MakeRequestPacket(byte code)
        {
            np = new NetPacket(code, null, null, 0, null, null, null, null, 0);
        }


        public FileInfoList RecvFileList()
        {
            byte[] recvData = new byte[DATA_LENGTH];
            int len = nstream.Read(recvData, 0, DATA_LENGTH);
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            ms.Write(recvData, 0, len);
            ms.Flush();
            ms.Position = 0;
            FileInfoList fil = bf.Deserialize(ms) as FileInfoList;
            return fil;
        }

        public void SetCryptor(string key)
        {
            //FileCrypto是一个类，在FileCrypto.cs文件中
            fc = new FileCrypto_2("./tmp/", workPath, key);
        }

        //Communication类中重写SendFile
        public override void SendFile(string sendPath)
        {
            //if (fc == null)
            //    //MessageBox.Show("加密器没有创建");
            //string enPath = fc.FileEncrypt(sendPath);
            base.SendFile(sendPath);
            File.Delete(sendPath);
        }

        //Communication类中重写RecvFile
        public override void RecvFile(string storePath)
        {
            //if (fc == null)
            //    //MessageBox.Show("加密器没有创建");
            //string enPath = fc.encryptedFileDir + Path.GetFileName(storePath);
            base.RecvFile(storePath);
            //fc.decryptedFileDir = Path.GetDirectoryName(storePath) + "/";
            //fc.FileDecrypt(enPath);
            //File.Delete(enPath);
        }
    }
}

