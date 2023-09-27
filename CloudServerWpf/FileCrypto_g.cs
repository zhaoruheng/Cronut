using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Numerics;
using NetPublic;

///服务器端
namespace Cloud
{
    internal class FileCrypto
    {
        string fileDir;          //客户端：文件路径
        string fileName;          //客户端：文件名
        long fileSize;             //客户端：文件大小(以字节为单位)
        string fileTag;          //客户端：文件标签   服务器也用了
        string fileEncryptKey;   //客户端：加密文件的密钥
        const string keyPrivate = "123456789";  //客户端：用户的K_private。别删谢谢！
        byte[] fileCiphertext;   //客户端：文件密文
        const string aesIV= "446C47278157A448F925084476F2A5C2";       //客户端：AES加密的初始化向量
        List<string> saltsVal;          //服务器：每棵树的盐值
        List<string> rootNode;          //服务器：每个盐值对应的根节点
        int leafNodeNum;                //服务器&客户端：文件分块数（叶子节点数）
        int MHTNum;                     //服务器：文件的MHT数量
        ServerComHelper serverComHelper;
        DataBaseManager dataBaseManager;
        string username;



        //客户端
        public FileCrypto( ServerComHelper serverComHelper, DataBaseManager dataBaseManager, string username)
        {
            rootNode = new List<string>();
            saltsVal = new List<string>();
            this.serverComHelper = serverComHelper;
            this.dataBaseManager = dataBaseManager;
            this.username = username;
        }
        public FileCrypto(ServerComHelper serverComHelper, DataBaseManager dataBaseManager, string username,string path)
        {
            rootNode = new List<string>();
            saltsVal = new List<string>();
            this.serverComHelper = serverComHelper;
            this.dataBaseManager = dataBaseManager;
            this.username = username;
            fileDir = path;
        }


        //客户端和服务器端都有
        public void FileUpload()
        {
            BigInteger alpha_Prime;
            NetPacket np = serverComHelper.RecvMsg();//!!!!!!!!!!!!!!!!1


            alpha_Prime = BlindSign.BlindSignature(np.F_prime);   //客户端和服务器端都有，请点开BlindSignature这个函数，里面有更具体的内容
            NetPacket npR=new NetPacket();
            np.F_prime = alpha_Prime;
            serverComHelper.MakeResponsePacket(npR); 
            serverComHelper.SendMsg();


            np = serverComHelper.RecvMsg();//!!!!!!!!!!!!!!!!1
            fileTag = np.fileTag;
            fileDir = "./ServerFiles/" + fileTag;
            fileName = np.fileName;
            
            long fileID = UserClassify(np);    //服务器：判断用户类型
            //************************后端：查询fileTag是否存在*****************************
            //fileTag fileName重复
            //fileName重复就删了
            if (fileID == 0)
            {
                Console.WriteLine("该用户为初始上传者");
                fileEncryptKey = np.enKey;
                //**********************通信：服务器发消息给客户端告诉你是初始上传者******************************
                serverComHelper.MakeResponsePacket(NetPublic.DefindedCode.AGREEUP);
                serverComHelper.SendMsg();

                //**********************通信：客户端将密文fileCiphertext上传给服务器******************************

                InitialUpload();   //执行初始上传者的操作。客户端和服务器都各自有一部分，需要您点开再分呢~
        }
            else
            {
                serverComHelper.MakeResponsePacket(NetPublic.DefindedCode.FILEEXISTED);
                serverComHelper.SendMsg();
                Console.WriteLine("该用户为后续上传者");
                MHTNum=dataBaseManager.GetMHTNum(fileTag);  
                SubsequentUpload(fileID);                         //执行后续上传者的操作

        }
    }

        //客户端
        byte[] CalculateSHA1()
        {
            using (FileStream fileStream = new FileStream(fileDir, FileMode.Open))
            {
                SHA1 sha1 = SHA1.Create();
                byte[] hashValue = sha1.ComputeHash(fileStream);

                return hashValue;
            } 
        }

        //客户端：计算SHA256
        byte[] CalculateSHA256(string str)
        {
            SHA256 sha256 = SHA256.Create();
            byte[] hashVal = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));

            return hashVal;
        }

        //客户端
        public void FileDownload()
        {
            serverComHelper.SendFile(fileDir);
        }

        //客户端
        public byte[] ReadFileContent()
        {
            FileInfo fileInfo = new FileInfo(fileDir);
            fileSize = fileInfo.Length;

            byte[] fileContent = new byte[fileInfo.Length + 100];

            try
            {
                using (FileStream fileStream = new FileStream(fileDir, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(fileStream))
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

        //客户端：对文件进行加密


        //客户端：计算文件标签
        public string GetFileTag()
        {
            fileTag = BitConverter.ToString(CalculateSHA256(BitConverter.ToString(fileCiphertext)));
            return fileTag;
        }

        //服务器
        public long UserClassify(NetPacket np)
        {
            //需要服务器端在数据库的文件表中查询，fileTag是否存在
            //如果存在，返回fileID（fileID是从1开始编号的）
            //不存在返回0
            return dataBaseManager.GetFileCountByTag(np.fileTag);
        }

        //客户端和服务器端都有，请参考具体注释♪(･ω･)ﾉ
        public void InitialUpload()
        {
            serverComHelper.RecvFile(fileTag);
            fileCiphertext = ReadFileContent();

            List<string> list= MerkleHashTree.FileSplit(fileCiphertext);  //服务器：对文件分块
            leafNodeNum = list.Count;
            Console.WriteLine("文件分块数量" + list.Count);

            MHTNum = MerkleHashTree.CalculateMHTNum(list.Count);   //服务器：计算MHT的数量
            Console.WriteLine("MHT的数量" + MHTNum);           

            saltsVal=MerkleHashTree.GenerateSalVal(MHTNum,8);  //服务器：生成y个随机盐值序列，8指定了每个序列的长度

            //这段是服务器：生成MHTNum个MHT
            for(int i=0;i<MHTNum;++i)
            {
                MerkleHashTree mht = new MerkleHashTree(list, saltsVal[i]);  //将list中的string加盐哈希作为叶子结点，生成MHT
                Console.WriteLine("这棵MHT对应的盐值" + saltsVal[i] + " 得到的根节点值为" + mht.GetRootHash());
                rootNode.Add(mht.GetRootHash());    
            }
            
           try
            {
                // 使用FileStream创建文件流
                using (FileStream fs = new FileStream(fileDir, FileMode.Create))
                {
                    // 使用Write方法将byte数组写入文件流
                    fs.Write(fileCiphertext, 0, fileCiphertext.Length);
                }

                Console.WriteLine("文件写入成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine("写入文件时出错：" + ex.Message);
            }

            
            long fileID = 0;
            dataBaseManager.InsertFileTable(ref fileID,fileName,fileTag,MHTNum,fileSize,fileDir,fileEncryptKey); //****************服务器：插入FileTable中********************

            for(int i=0;i<MHTNum ;++i)
            {
                dataBaseManager.InsertMHTTable(fileID, i, saltsVal[i], rootNode[i]);   //****************服务器：插入MHT表****************
            }

            string uploadDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            dataBaseManager.InsertUpFileTable(fileID,fileName,uploadDateTime,username);    //****************服务器：插入文件所有权表****************
        }

        //客户端和服务器端都有，请参考具体注释
        public void SubsequentUpload(long fileID)
        {
            //服务器：发起挑战
            int challengeLeafNode=0;

            int k = PoW.GenerateChallenge(MHTNum,leafNodeNum, ref challengeLeafNode);  
            Console.WriteLine("\n选中第" + k + "棵树的第" + challengeLeafNode + "个叶子节点作为挑战 (从0开始！)");
            for (int i = 0; i < MHTNum; ++i)
            {
                   saltsVal.Add(dataBaseManager.GetSalt(fileID, i));  //服务器：从数据库中获取盐值

            }

            //***********************通信：服务器将challengeLeafNode和k(MHT的编号)发给客户端**************************
            NetPacket np = new NetPacket();
            np.challengeLeafNode = challengeLeafNode;
            np.MHTID = k;
            np.enKey = saltsVal[k];
            serverComHelper.MakeResponsePacket(np);
            serverComHelper.SendMsg();

            

            //*************************通信：客户端将ResponseNodeSet发送给服务器*************************************
            np = serverComHelper.RecvMsg();

            //服务器：验证响应
            bool isPassPow = PoW.VerifyRepsonse(saltsVal[k], np.ResponseNodeSet, rootNode[k]); //CSP验证Response正确性

            //*************************通信：服务器将验证结果isPassPow发送给客户端**************************************
            if(isPassPow)
            {
                serverComHelper.MakeResponsePacket(NetPublic.DefindedCode.AGREEUP);
            }
            else
            {
                serverComHelper.MakeResponsePacket(NetPublic.DefindedCode.DENIED);
            }

            if (isPassPow)
            {
                Console.WriteLine("用户通过了PoW验证!");

                string uploadDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                dataBaseManager.InsertUpFileTable(fileID,fileName,uploadDateTime,username); //服务器：更新UploadFileTable
            }
            else
            {
                Console.WriteLine("用户未通过验证!");
            }
        }

 

    }
}
