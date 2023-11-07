using System;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using NetPublic;
using System.Text;
using System.Diagnostics;
using log4net;

namespace Cloud
{
    abstract class Communication
    {
        protected readonly int MSG_LENGTH = 1024;
        protected readonly int DATA_LENGTH = 1024 * 1024;

        protected NetPublic.NetPacket np;
        protected TcpClient tcpClient;             //子类中给tcpClient赋值
        protected byte[] message;                  //子类Make方法后存储message
        protected NetworkStream nstream;           //子类中指定stream
        private static ILog log = LogManager.GetLogger("Log");

        IPAddress targetIP;
        int targetPort;

        public Communication()
        {
            message = new byte[MSG_LENGTH];
        }

        public void SendMsg()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
#pragma warning disable SYSLIB0011
            bf.Serialize(ms, np);
#pragma warning disable SYSLIB0011
            try
            {
                nstream.Write(ms.GetBuffer(), 0, (int)ms.Length);
            }
            catch (Exception e)
            {
                //连接中断
                Debug.WriteLine(e.Message);
                log.Error("连接中断");
            }
            np = null;
        }

        public NetPublic.NetPacket RecvMsg()
        {
            byte[] resMsg = new byte[MSG_LENGTH];
            int len = 0;
            try
            {
                len = nstream.Read(resMsg, 0, MSG_LENGTH);
            }
            catch (Exception e)
            {
                //连接中断
                Debug.WriteLine(e.Message);
            }
            MemoryStream memory = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            memory.Write(resMsg, 0, len);
            memory.Flush();
            memory.Position = 0;

#pragma warning disable SYSLIB0011
            NetPublic.NetPacket np = bf.Deserialize(memory) as NetPublic.NetPacket;
#pragma warning disable SYSLIB0011
            return np;
        }

        public virtual void SendFile(string sendPath)
        {
            using (FileStream fs = new FileStream(sendPath, FileMode.Open, FileAccess.Read))
            {
                byte[] sendData = new byte[DATA_LENGTH];
                long leftSize = fs.Length;
                int start = 8;

                Buffer.BlockCopy(BitConverter.GetBytes(leftSize), 0, sendData, 0, 8);
                int readLength;

                while ((readLength = fs.Read(sendData, start, DATA_LENGTH - start)) > 0)
                {
                    leftSize -= readLength;
                    nstream.Write(sendData, 0, start + readLength);
                    start = 0;
                }
            }
        }

        public virtual void RecvFile(string storePath)
        {
            using (FileStream fs = new FileStream(storePath, FileMode.Create, FileAccess.Write))
            {
                byte[] fileData = new byte[DATA_LENGTH];
                int readLength;

                readLength = nstream.Read(fileData, 0, DATA_LENGTH);

                long fileSize = BitConverter.ToInt64(fileData, 0);

                long recvLength = readLength - 8;
                fs.Write(fileData, 8, readLength - 8);
                while (recvLength < fileSize)
                {
                    readLength = nstream.Read(fileData, 0, DATA_LENGTH);
                    recvLength += readLength;
                    fs.Write(fileData, 0, readLength);
                }
            }
        }
    }
}
