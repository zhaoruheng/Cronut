using NetPublic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Security.Cryptography;
using CloudServer.Views;
using Avalonia.Threading;

namespace Cloud
{
    internal class FileCrypto
    {
        private string fileDir;          //客户端：文件路径
        private string fileName;          //客户端：
        private long fileSize;             //客户端：文件大小(以字节为单位)
        private string fileTag;          //客户端：文件标签   服务器也用了
        private string fileEncryptKey;   //客户端：加密文件的密钥
        private const string keyPrivate = "123456789";  //客户端：用户的K_private。别删谢谢！
        private byte[] fileCiphertext;   //客户端：文件密文
        private const string aesIV = "446C47278157A448F925084476F2A5C2";       //客户端：AES加密的初始化向量
        private List<string> saltsVal;          //服务器：每棵树的盐值
        private readonly List<string> rootNode;          //服务器：每个盐值对应的根节点
        private int leafNodeNum;                //服务器&客户端：文件分块数（叶子节点数）
        private int MHTNum;                     //服务器：文件的MHT数量
        private readonly ServerComHelper serverComHelper;
        private readonly DataBaseManager dataBaseManager;
        private readonly string username;

        //客户端
        public FileCrypto(ServerComHelper serverComHelper, DataBaseManager dataBaseManager, string username)
        {
            rootNode = new List<string>();
            saltsVal = new List<string>();
            this.serverComHelper = serverComHelper;
            this.dataBaseManager = dataBaseManager;
            this.username = username;
        }

        private void AddDetailedParaItem(string str)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _ = MainWindow.lb.Items.Add(str);
            });
        }

        public FileCrypto(ServerComHelper serverComHelper, DataBaseManager dataBaseManager, string username, string path)
        {
            rootNode = new List<string>();
            saltsVal = new List<string>();
            this.serverComHelper = serverComHelper;
            this.dataBaseManager = dataBaseManager;
            this.username = username;
            fileDir = path;
        }

        public event Action<string> AlgParaItemAdded;

        public void AddAlgParaItem(string item)
        {
            AlgParaItemAdded?.Invoke(item);
        }

        public void FileUpload()
        {
            BigInteger alpha_Prime;
            NetPacket np = serverComHelper.RecvMsg();

            AddDetailedParaItem("F': " + np.F_prime);
            alpha_Prime = BlindSign.BlindSignature(np.F_prime);   //客户端和服务器端都有，请点开BlindSignature这个函数，里面有更具体的内容
            AddDetailedParaItem("alpha': " + alpha_Prime);

            NetPacket npR = new()
            {
                F_prime = alpha_Prime,
            };

            serverComHelper.MakeResponsePacket(npR);
            serverComHelper.SendMsg();

            np = serverComHelper.RecvMsg();
            fileTag = np.fileTag;
            fileDir = "./ServerFiles/" + fileTag;
            fileName = np.fileName;
            AddDetailedParaItem("fileTag: " + fileTag);

            int fileID = UserClassify(np);    //服务器：判断用户类型

            //************************后端：查询fileTag是否存在*****************************
            //fileTag fileName重复
            //fileName重复就删了
            if (fileID == 0)
            {
                AddDetailedParaItem("该用户为初始上传者");
                fileEncryptKey = np.enKey;
                serverComHelper.MakeResponsePacket(DefindedCode.AGREEUP);
                serverComHelper.SendMsg();

                InitialUpload();
            }
            else
            {
                AddDetailedParaItem("该用户为后续上传者");
                serverComHelper.MakeResponsePacket(DefindedCode.FILEEXISTED);
                serverComHelper.SendMsg();
                MHTNum = dataBaseManager.GetMHTNum(fileTag);
                SubsequentUpload(fileID);                         //执行后续上传者的操作
            }
        }

        public void FileDownload()
        {
            serverComHelper.SendFile(fileDir);
        }

        public byte[] ReadFileContent()
        {
            FileInfo fileInfo = new(fileDir);
            fileSize = fileInfo.Length;

            byte[] fileContent = new byte[fileInfo.Length + 100];

            try
            {
                using (FileStream fileStream = new(fileDir, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new(fileStream))
                    {
                        int fileSize = (int)new FileInfo(fileDir).Length;
                        fileContent = reader.ReadBytes(fileSize);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("文件读取错误:" + e.Message);
            }
            return fileContent;
        }

        public byte[] ReadFileContent2(string workdir)
        {
            FileInfo fileInfo = new(workdir);
            fileSize = fileInfo.Length;

            byte[] fileContent = new byte[fileInfo.Length + 100];

            try
            {
                using (FileStream fileStream = new(workdir, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new(fileStream))
                    {
                        int fileSize = (int)new FileInfo(workdir).Length;
                        fileContent = reader.ReadBytes(fileSize);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("文件读取错误:" + e.Message);
            }
            return fileContent;
        }

        public int UserClassify(NetPacket np)
        {
            return dataBaseManager.GetFileCountByTag(np.fileTag);
        }

        public void InitialUpload()
        {
            serverComHelper.RecvFile(fileTag);
            fileCiphertext = ReadFileContent();

            List<string> list = MerkleHashTree.FileSplit(fileCiphertext);  //服务器：对文件分块
            leafNodeNum = list.Count;
            AddDetailedParaItem("MHT叶节点数目: " + leafNodeNum);

            MHTNum = MerkleHashTree.CalculateMHTNum(list.Count);   //服务器：计算MHT的数量
            AddDetailedParaItem("MHT数量: " + MHTNum);
            AddDetailedParaItem("随机生成的盐值序列:");

            saltsVal = MerkleHashTree.GenerateSalVal(MHTNum, 8);  //服务器：生成y个随机盐值序列，8指定了每个序列的长度

            for (int i = 0; i < MHTNum; ++i)
            {
                MerkleHashTree mht = new(list, saltsVal[i]);  //将list中的string加盐哈希作为叶子结点，生成MHT
                AddDetailedParaItem("id: " + (i + 1) + " salt: " + saltsVal[i]);
                rootNode.Add(mht.GetRootHash());
            }

            try
            {
                using (FileStream fs = new(fileDir, FileMode.Create))
                {
                    fs.Write(fileCiphertext, 0, fileCiphertext.Length);
                }

                Console.WriteLine("文件写入成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine("写入文件时出错：" + ex.Message);
            }


            int fileID = 0;
            dataBaseManager.InsertFileTable(ref fileID, fileName, fileTag, MHTNum, fileSize, fileDir, fileEncryptKey); //****************服务器：插入FileTable中********************

            for (int i = 0; i < MHTNum; ++i)
            {
                dataBaseManager.InsertMHTTable(fileID, i, saltsVal[i], rootNode[i]);
            }

            string uploadDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            dataBaseManager.InsertUpFileTable(fileID, fileName, uploadDateTime, username);
        }

        public void SubsequentUpload(int fileID)
        {
            int challengeLeafNode = 0;

            int k = PoW.GenerateChallenge(MHTNum, leafNodeNum, ref challengeLeafNode);

            int cchallengeLeafNode;
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())  //随机选择一棵MHT
            {
                byte[] data = new byte[5];
                rng.GetBytes(data);
                int value = BitConverter.ToInt32(data, 0);
                cchallengeLeafNode = Math.Abs(value % 24);
            }

            AddDetailedParaItem("Challenge: 选中第" + (k + 1) + "棵树的第" + (cchallengeLeafNode + 1) + "个叶子节点");

            for (int i = 0; i < MHTNum; ++i)
            {
                saltsVal.Add(dataBaseManager.GetSalt(fileID, i));
            }

            for (int i = 0; i < MHTNum; ++i)
            {
                rootNode.Add(dataBaseManager.GetRootNode(fileID, i));
            }

            NetPacket np = new()
            {
                challengeLeafNode = challengeLeafNode,
                MHTID = k,
                enKey = saltsVal[k]
            };

            serverComHelper.MakeResponsePacket(np);
            serverComHelper.SendMsg();

            string tmpdir = fileTag + "_MHT";
            serverComHelper.RecvFile(tmpdir);
            byte[] ResponseNode = ReadFileContent2("./ServerFiles/" + tmpdir);
            List<string> ResponseNodeSet = ByteArrayToListString(ResponseNode);

            AddDetailedParaItem("Response: 收到的相应集合:");
            foreach (string str in ResponseNodeSet)
            {
                AddDetailedParaItem(str);
            }

            bool isPassPow = PoW.VerifyRepsonse(saltsVal[k], ResponseNodeSet, rootNode[k]); //CSP验证Response正确性
            if (isPassPow)
            {
                AddDetailedParaItem("Verify: 用户通过了所有权验证! 文件去重成功!");
            }
            else
            {
                AddDetailedParaItem("Verify: 用户未通过所有权验证!");
            }

            File.Delete(tmpdir);
            if (isPassPow)
            {
                serverComHelper.MakeResponsePacket(DefindedCode.AGREEUP);
                serverComHelper.SendMsg();
            }
            else
            {
                serverComHelper.MakeResponsePacket(DefindedCode.DENIED);
            }

            if (isPassPow)
            {
                string uploadDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                dataBaseManager.FindUpFileTable(fileID, fileName, uploadDateTime, username); //服务器：更新UploadFileTable
            }
            else
            {
                Console.WriteLine("用户未通过验证!");
            }
        }

        public static List<string> ByteArrayToListString(byte[] byteArray)
        {
            BinaryFormatter formatter = new();
            using (MemoryStream memoryStream = new(byteArray))
            {
#pragma warning disable SYSLIB0011
                return (List<string>)formatter.Deserialize(memoryStream);
#pragma warning restore SYSLIB0011
            }
        }
    }
}
