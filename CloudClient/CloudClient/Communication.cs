using System;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using NetPublic;
using System.Text;
using System.Diagnostics;

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

        IPAddress targetIP;
        int targetPort;

        //构造函数
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

            nstream.Write(ms.GetBuffer(), 0, (int)ms.Length);
            np = null;
        }

        public NetPublic.NetPacket RecvMsg()
        {
            byte[] resMsg = new byte[MSG_LENGTH];
            int len = nstream.Read(resMsg, 0, MSG_LENGTH);
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
        //虚函数是因为子类要对发送和接收文件做额外操作
        {
            using (FileStream fs = new FileStream(sendPath, FileMode.Open, FileAccess.Read))
            {
                byte[] sendData = new byte[DATA_LENGTH];    //要发送的数据
                long leftSize = fs.Length;
                ////MessageBox.Show(leftSize.ToString());
                int start = 8;

                Buffer.BlockCopy(BitConverter.GetBytes(leftSize), 0, sendData, 0, 8);
                int readLength;

                while ((readLength = fs.Read(sendData, start, DATA_LENGTH - start)) > 0)
                //readLength是读入缓冲区的字节数
                {
                    leftSize -= readLength;
                    nstream.Write(sendData, 0, start + readLength);//将SendData中的数据写入NetworkStream中
                    start = 0;  //为什么start每次归0？？？？？？？？？？？？？？
                }
            }
        }

        public virtual void RecvFile(string storePath)//要存储的路径
        {
            using (FileStream fs = new FileStream(storePath, FileMode.Create, FileAccess.Write))
            //返回要存储路径文件的文件流
            {
                byte[] fileData = new byte[DATA_LENGTH];    //接收的数据
                int readLength;

                readLength = nstream.Read(fileData, 0, DATA_LENGTH);//从NetworkStream中读取数据的字节数

                long fileSize = BitConverter.ToInt64(fileData, 0);//将fileData数组转化成int64

                ////MessageBox.Show(fileSize.ToString());
                long recvLength = readLength - 8;
                fs.Write(fileData, 8, readLength - 8);
                while (recvLength < fileSize)
                {
                    readLength = nstream.Read(fileData, 0, DATA_LENGTH);//将fileData写入NetworkStream流中
                    recvLength += readLength;
                    fs.Write(fileData, 0, readLength);//将fileData写入文件流fileStream
                }
            }
        }
    }
}
