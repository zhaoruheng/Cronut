using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NetPublic;

namespace Cloud
{
    abstract class Communication
    //纯虚函数：在基类中仅仅给出定义，不对虚函数进行定义，而是在派生类中实现
     //含有纯虚函数的类：抽象类
     //抽象类只能作为派生类的基类，不能定义对象
    {
        protected readonly int MSG_LENGTH = 1024;
        protected readonly int DATA_LENGTH = 1024 * 1024;

        protected NetPublic.NetPacket np;
        protected TcpClient tcpClient;             //子类中给tcpClient赋值
        protected byte[] message;                  //子类Make方法后存储message
        protected NetworkStream nstream;           //子类中指定stream

        //构造函数
        public Communication()
        {
            //byte的取值范围是0~255，超过就会重头开始算
            //和int类型的区别：int类型占4字节  byte类型占1字节
            //C#编译器会认为byte类型和byte类型运算的结果是int型
            message = new byte[MSG_LENGTH];
        }

        /// <summary>
        /// 将NetPacket对象序列化为MemoryStream
        /// 然后将MemoryStream写到NetworkStream中
        /// </summary>
        public void SendMsg()
        {
            //BinaryFormatter:序列化又称串行化，是.NET运行时环境用来支持用户定义类型的流化的机制
            //目的:以某种存储形式使自定义对象持久化，或者将这种对象从一个地方传输到另一个地方
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(); //创建一个流，其后备存储为内存
            bf.Serialize(ms, np);   //np是NetPacket
            //将对象序列化为给定流ms

            //把ms字符串写到ntream（NetworkStream）流中
            nstream.Write(ms.GetBuffer(), 0, (int)ms.Length);
            np = null;
        }

        //public void SendMsg()
        //{
        //    BinaryFormatter bf = new BinaryFormatter();
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        bf.Serialize(ms, np);
        //        byte[] messageBytes = ms.ToArray();
        //        nstream.Write(messageBytes, 0, messageBytes.Length);
        //    }
        //}


        /// <summary>
        /// 将接收的信息写入内存流MemoryStream
        /// 将MemoryStream反序列化成NetPacket
        /// </summary>
        /// <返回NetPacket></returns>
        /// 
        public NetPublic.NetPacket RecvMsg()
        {
            byte[] resMsg = new byte[MSG_LENGTH];          //接收信息
            int len = nstream.Read(resMsg, 0, MSG_LENGTH);  //获取接收信息的长度

            MemoryStream memory = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            memory.Write(resMsg, 0, len); //将接收的信息写入内存流MemoryStream
            memory.Flush(); //重写

            memory.Position = 0;
            NetPublic.NetPacket np = bf.Deserialize(memory) as NetPublic.NetPacket;
            //将内存流memory反序列化为NetPacket
            return np;
        }

        //public NetPacket RecvMsg()
        //{
        //    // 读取消息长度
        //    byte[] lengthBytes = new byte[sizeof(int)];
        //    int bytesRead = 0;
        //    while (bytesRead < sizeof(int))
        //    {
        //        bytesRead += nstream.Read(lengthBytes, bytesRead, sizeof(int) - bytesRead);
        //    }
        //    int messageLength = BitConverter.ToInt32(lengthBytes, 0);

        //    // 读取消息内容
        //    byte[] messageBytes = new byte[messageLength];
        //    bytesRead = 0;
        //    while (bytesRead < messageLength)
        //    {
        //        bytesRead += nstream.Read(messageBytes, bytesRead, messageLength - bytesRead);
        //    }

        //    // 反序列化消息
        //    using (MemoryStream memory = new MemoryStream(messageBytes))
        //    {
        //        BinaryFormatter bf = new BinaryFormatter();
        //        NetPacket np = (NetPacket)bf.Deserialize(memory);
        //        return np;
        //    }
        //}



        /// <summary>
        /// 将要上传的文件拷贝到SendData数组(byte)
        /// 然后依次写入NetworkStream流中
        /// </summary>
        /// <param name="sendPath"></param>
        public virtual void SendFile(string sendPath) 
            //虚函数是因为子类要对发送和接收文件做额外操作
        {
            using (FileStream fs = new FileStream(sendPath, FileMode.Open, FileAccess.Read))
                //新建一个文件流
            {
                byte[] sendData = new byte[DATA_LENGTH];    //要发送的数据
                long leftSize = fs.Length;
                //MessageBox.Show(leftSize.ToString());
                int start = 8;

                //拷贝到sendData数组中
                //BitConverter.GetBytes(long)将long转化成byte[]数组
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

        /// <summary>
        /// 将接收的数据byte[]写入fileStream流中
        /// </summary>
        /// <param name="storePath"></param>
        public virtual void RecvFile(string storePath)//要存储的路径
        {
            using (FileStream fs = new FileStream(storePath, FileMode.Create, FileAccess.Write))
                //返回要存储路径文件的文件流
            {
                byte[] fileData = new byte[DATA_LENGTH];    //接收的数据
                int readLength;

                readLength = nstream.Read(fileData, 0, DATA_LENGTH);//从NetworkStream中读取数据的字节数

                long fileSize = BitConverter.ToInt64(fileData, 0);//将fileData数组转化成int64

                //MessageBox.Show(fileSize.ToString());
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
