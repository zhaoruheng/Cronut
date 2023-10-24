using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using NetPublic;
using System.Runtime.Serialization.Formatters.Binary;

namespace Cloud
{
    class ServerComHelper : Communication
    {
        public ServerComHelper(TcpClient client) : base()
        {
            tcpClient = client;
            nstream = tcpClient.GetStream();
        }

        public void MakeResponsePacket(byte code)
        {
            np = new NetPacket(code, null, null, 0, null, null, null, null, -1);
        }

        public void MakeResponsePacket(NetPacket np)
        {
            this.np = np;
        }

        //public void MakeResponsePacket(byte code, string enKey, string enMd5) //用于下载
        //{
        //    np = new NetPacket(code, null, null, 0, null, null, null, null, -1);
        //    np.enMd5 = enMd5;
        //    np.enKey = enKey;
        //}

        public override void RecvFile(string fname)
        {
            string storePath = "./ServerFiles/" + fname;
            base.RecvFile(storePath);
        }

        //通信：发送文件列表
        public void SendFileList(FileInfoList fileList)
        {
            //byte[] bFileList = Encoding.Default.GetBytes(fileList);
            byte[] sendData = new byte[DATA_LENGTH];
            FileInfoList fil = new FileInfoList();
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();

#pragma warning disable SYSLIB0011
            bf.Serialize(ms, fileList);
#pragma warning restore SYSLIB0011
            nstream.Write(ms.GetBuffer(), 0, (int)ms.Length);

            //	int bFileListLength = bFileList.Length;
            //	if (bFileListLength == 0)
            //	{
            //		sendData[0] = 0x62;
            //		nstream.Write(sendData, 0, 1);
            //		return;
            //	}
            //	int offset = 0;
            //	while(bFileListLength - offset > DATA_LENGTH - 1)
            //	{
            //		if (bFileListLength - offset == DATA_LENGTH - 1)
            //			sendData[0] = 0x62;
            //		else
            //			sendData[0] = 0x61;
            //		Buffer.BlockCopy(bFileList, offset, sendData, 1, DATA_LENGTH - 1);
            //		nstream.Write(sendData, 0, DATA_LENGTH);
            //		offset += DATA_LENGTH - 1;
            //	}
            //	if (offset < bFileListLength)
            //	{
            //		sendData[0] = 0x62;
            //		Buffer.BlockCopy(bFileList, offset, sendData, 1, bFileListLength - offset);
            //		nstream.Write(sendData, 0, 1 + bFileListLength - offset);
            //	}
        }
    }
}
