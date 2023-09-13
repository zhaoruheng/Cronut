using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using NetPublic;

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
            np = new NetPacket(code, null, null, null, -1, null, null, null);
        }
        public void MakeResponsePacket(byte code, string enKey, string enMd5) //用于下载
        {
            np = new NetPacket(code, null, null, null, -1, null, null, null);
            np.enMd5 = enMd5;
            np.enKey = enKey;
        }
        public override void RecvFile(string fname)
        {
            string storePath = "./ServerFiles/" + fname;
            base.RecvFile(storePath);
        }
        public void SendFileList(FileInfoList fileList)
        {
            //byte[] bFileList = Encoding.Default.GetBytes(fileList);
            byte[] sendData = new byte[DATA_LENGTH];
            FileInfoList fil = new FileInfoList();
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, fileList);
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
