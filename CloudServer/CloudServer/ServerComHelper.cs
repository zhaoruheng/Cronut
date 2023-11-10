using System.Net.Sockets;
using System.IO;
using NetPublic;
using System.Runtime.Serialization.Formatters.Binary;

namespace Cloud
{
    internal class ServerComHelper : Communication
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

        public override void RecvFile(string fname)
        {
            string storePath = "./ServerFiles/" + fname;
            base.RecvFile(storePath);
        }

        //通信：发送文件列表
        public void SendFileList(FileInfoList fileList)
        {
            byte[] sendData = new byte[DATA_LENGTH];
            FileInfoList fil = new();
            BinaryFormatter bf = new();
            MemoryStream ms = new();

#pragma warning disable SYSLIB0011
            bf.Serialize(ms, fileList);
#pragma warning restore SYSLIB0011
            nstream.Write(ms.GetBuffer(), 0, (int)ms.Length);
        }
    }
}
