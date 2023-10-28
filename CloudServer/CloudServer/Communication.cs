using NetPublic;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Cloud
{
    internal abstract class Communication
    {
        protected readonly int MSG_LENGTH = 1024;
        protected readonly int DATA_LENGTH = 1024 * 1024;

        protected NetPacket np;
        protected TcpClient tcpClient;
        protected byte[] message;
        protected NetworkStream nstream;

        public Communication()
        {
            message = new byte[MSG_LENGTH];
        }

        public void SendMsg()
        {
            BinaryFormatter bf = new();
            MemoryStream ms = new();
#pragma warning disable SYSLIB0011
            bf.Serialize(ms, np);
#pragma warning restore SYSLIB0011

            nstream.Write(ms.GetBuffer(), 0, (int)ms.Length);
            np = null;
        }

        public NetPacket RecvMsg()
        {
            byte[] resMsg = new byte[MSG_LENGTH];
            int len = nstream.Read(resMsg, 0, MSG_LENGTH);
            MemoryStream memory = new();
            BinaryFormatter bf = new();
            memory.Write(resMsg, 0, len);
            memory.Flush();
            memory.Position = 0;
#pragma warning disable SYSLIB0011
            NetPacket np = bf.Deserialize(memory) as NetPacket;
#pragma warning restore SYSLIB0011
            return np;
        }

        public virtual void SendFile(string sendPath) //虚函数是因为子类要对发送和接收文件做额外操作
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
