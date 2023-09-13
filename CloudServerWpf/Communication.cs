using System;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NetPublic;

namespace Cloud
{
    abstract class Communication
    {


        protected readonly int MSG_LENGTH = 1024;
        protected readonly int DATA_LENGTH = 1024 * 1024;

        protected NetPacket np;
        protected TcpClient tcpClient;             //子类中给tcpClient赋值
        protected byte[] message;                  //子类Make方法后存储message
        protected NetworkStream nstream;           //子类中指定stream

        public Communication()
        {
            message = new byte[MSG_LENGTH];
        }

        public void SendMsg()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, np);
            nstream.Write(ms.GetBuffer(), 0, (int)ms.Length);
            np = null;
        }

        public NetPacket RecvMsg()
        {
            byte[] resMsg = new byte[MSG_LENGTH];
            int len = nstream.Read(resMsg, 0, MSG_LENGTH);
            MemoryStream memory = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            memory.Write(resMsg, 0, len);
            memory.Flush();
            memory.Position = 0;
            NetPacket np = bf.Deserialize(memory) as NetPacket;
            return np;
        }

        public virtual void SendFile(string sendPath) //虚函数是因为子类要对发送和接收文件做额外操作
        {
            using (FileStream fs = new FileStream(sendPath, FileMode.Open, FileAccess.Read))
            {
                byte[] sendData = new byte[DATA_LENGTH];
                long leftSize = fs.Length;
                //MessageBox.Show(leftSize.ToString());
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
                //MessageBox.Show(fileSize.ToString());
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

