using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using NetPublic;
using System.Diagnostics;

namespace Cloud
{
    class ClientComHelper : Communication
    //Communication是抽象类
    //ClientComHelper继承了Communication抽象类
    {
        IPAddress targetIP;
        FileCrypto fc;
        int targetPort;
        string workPath;

        public ClientComHelper(string ipStr, int port, string workPath) : base()
        {
            this.workPath = workPath;
            tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
            targetIP = IPAddress.Parse(ipStr);  
            targetPort = port;

            try
            {
                tcpClient.Connect(targetIP, targetPort);
                nstream = tcpClient.GetStream(); 
            }
            catch (Exception e)
            {
                //连接失败
            }
               
        }

        ~ClientComHelper()
        {
            tcpClient.Close();  //断开连接
        }

        public void MakeRequestPacket(byte code, string userName, string password, long fileLength, string fileTag, string fileName, string newName, string uploadTime, long userType, string enkey = null)
        {
            FileInfo fi = new FileInfo(workPath + "/" + newName);

            string changeTime = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

            np = new NetPacket(code, userName, password, fileLength, fileTag, fileName, newName, changeTime, userType);
            if (enkey != null)
                np.enKey = enkey;
        }

        public void MakeRequestPacket(NetPacket np)
        {
            this.np = np;
        }
     
        public void MakeRequestPacket(byte code, string userName, long fileLength, string fileName, string newName)
        {
            FileInfo fi = new FileInfo(workPath + "/" + fileName);
            string changeTime = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

            np = new NetPacket(code, userName, null, fileLength, null, fileName, newName, changeTime, 0);
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
#pragma warning disable SYSLIB0011
            FileInfoList fil = bf.Deserialize(ms) as FileInfoList;
#pragma warning restore SYSLIB0011

            return fil;
        }

        public void SetCryptor(string key)
        {
            //FileCrypto是一个类，在FileCrypto.cs文件中
            fc = new FileCrypto("./tmp/", workPath, key);
        }

        //Communication类中重写SendFile
        public override void SendFile(string sendPath)
        {
            base.SendFile(sendPath);
            try
            {
                File.Delete(sendPath);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public override void RecvFile(string storePath)
        {
            base.RecvFile(storePath);
        }
    }
}

