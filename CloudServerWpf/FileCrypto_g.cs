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
        string fileTag;          //客户端：文件标签
        string fileEncryptKey;   //客户端：加密文件的密钥
        const string keyPrivate = "123456789";  //客户端：用户的K_private。别删谢谢！
        byte[] fileCiphertext;   //客户端：文件密文
        const string aesIV= "446C47278157A448F925084476F2A5C2";       //客户端：AES加密的初始化向量
        List<string> saltsVal;          //服务器：每棵树的盐值
        List<string> rootNode;          //服务器：每个盐值对应的根节点
        int leafNodeNum;                //服务器&客户端：文件分块数（叶子节点数）
        int MHTNum;                     //服务器：文件的MHT数量
        ServerComHelper serverComHelper;



        //客户端
        public FileCrypto(string path, ServerComHelper serverComHelper)
        {
            fileDir = path;
            rootNode = new List<string>();
            saltsVal = new List<string>();
            this.serverComHelper = serverComHelper;
        }
        //public FileCrypto()
        //{
        //    rootNode = new List<string>();
        //    saltsVal = new List<string>();
        //}


        //客户端和服务器端都有
        public void FileUpload()
        {
            BigInteger alpha_Prime,F_Prime;
            F_Prime = 0;
            NetPacket np = serverComHelper.RecvMsg();//!!!!!!!!!!!!!!!!1


            alpha_Prime = BlindSign.BlindSignature(F_Prime);   //客户端和服务器端都有，请点开BlindSignature这个函数，里面有更具体的内容
            serverComHelper.MakeResponsePacket(F_Prime); 
            serverComHelper.SendMsg();


            NetPacket np = serverComHelper.RecvMsg();//!!!!!!!!!!!!!!!!1

            long fileID = UserClassify();    //服务器：判断用户类型
            //************************后端：查询fileTag是否存在*****************************
            //fileTag fileName重复
            //fileName重复就删了
            if (fileID == 0)
            {
                Console.WriteLine("该用户为初始上传者");

                //**********************通信：服务器发消息给客户端告诉你是初始上传者******************************

                //**********************通信：客户端将密文fileCiphertext上传给服务器******************************

                InitialUpload();   //执行初始上传者的操作。客户端和服务器都各自有一部分，需要您点开再分呢~
        }
            else
            {
                Console.WriteLine("该用户为后续上传者");
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
            byte[] plaintext = FileDecrypt();

            Console.WriteLine("\n--------------对文件进行解密----------------");
            Console.WriteLine("解密文件路径:" + fileDir);
            //Console.WriteLine("解密后的明文:");
            //Console.WriteLine(Encoding.UTF8.GetString(plaintext));
        }

        //客户端
        public byte[] ReadFileContent()
        {
            FileInfo fileInfo = new FileInfo(fileDir);
            fileSize = fileInfo.Length;
            fileName = fileInfo.Name;

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
        public byte[] FileEncrypt()
        {
            int keySize = 256;  //设置密钥长度，因为密钥长度不一定符合要求，需要resize
            int ivSize = 128;   //设置iv长度，因为iv长度不一定符合要求，也需要resize
            byte[] encryptedContent;

            //读出fileDir路径下的文件内容
            byte[] fileContent = ReadFileContent();

            using(Aes aes = Aes.Create())
            {
                //获取正确的Key
                byte[] keyBytes = Encoding.UTF8.GetBytes(fileEncryptKey);
                Array.Resize(ref keyBytes, keySize/8);
                aes.Key = keyBytes;

                //获取正确的IV
                byte[] ivBytes = Encoding.UTF8.GetBytes(aesIV);
                Array.Resize(ref ivBytes, ivSize / 8);
                aes.IV = ivBytes;

                //设置CBC模式
                aes.Mode = CipherMode.CBC;

                using (MemoryStream memoryStream=new MemoryStream())
                {
                    using(CryptoStream encryptor=new CryptoStream(memoryStream, aes.CreateEncryptor(aes.Key, aes.IV), CryptoStreamMode.Write))
                    {
                        encryptor.Write(fileContent, 0, fileContent.Length);
                    }

                    encryptedContent = memoryStream.ToArray();

                    //Console.WriteLine("\n成功加密文件" + fileDir + ".");
                    //Console.WriteLine("加密密钥" + fileEncryptKey);
                    //Console.WriteLine("密文" + BitConverter.ToString(encryptedContent));
                    //Console.WriteLine("密文长度" + encryptedContent.Length);
                }
            }
            return encryptedContent;
        }

        //客户端
        public byte[] FileDecrypt()
        {
            byte[] cipherBytes = fileCiphertext;
            int keySize = 256;
            int ivSize = 128;

            using(Aes aes = Aes.Create())
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(fileEncryptKey);
                Array.Resize(ref keyBytes, keySize / 8);
                aes.Key = keyBytes; //设置解密密钥

                byte[] ivBytes = Encoding.UTF8.GetBytes(aesIV);
                Array.Resize(ref ivBytes, ivSize / 8);
                aes.IV = ivBytes;   //设置初始化向量

                aes.Mode = CipherMode.CBC;  //设置解密模式为CBC模式

                using(MemoryStream memoryStream=new MemoryStream())
                {
                    CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);
                    cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
                    cryptoStream.FlushFinalBlock();

                    byte[] fileBytes = memoryStream.ToArray();

                    return fileBytes;
                }
            }
            
        }

        //客户端：计算文件标签
        public string GetFileTag()
        {
            fileTag = BitConverter.ToString(CalculateSHA256(BitConverter.ToString(fileCiphertext)));
            return fileTag;
        }

        //服务器
        public static long UserClassify()
        {
            //需要服务器端在数据库的文件表中查询，fileTag是否存在
            //如果存在，返回fileID（fileID是从1开始编号的）
            //不存在返回0
            long jiadefanhuizhi=123456789;

            return jiadefanhuizhi;
        }

        //客户端和服务器端都有，请参考具体注释♪(･ω･)ﾉ
        public void InitialUpload()
        {

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
            string serAdd = "./ServerFiles/" + fileTag;     //服务器的物理地址
            StoreCiphertext(fileCiphertext,serAdd);   //****************服务器:存储密文****************

            
            long fileID = 0;
            InsertFileTable(ref fileID,fileName,fileTag,MHTNum,fileSize,serAdd); //****************服务器：插入FileTable中********************

            for(int i=0;i<MHTNum ;++i)
            {
                InsertMHTTable(fileID, i, saltsVal[i], rootNode[i]);   //****************服务器：插入MHT表****************
            }

            string uploadDateTime = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
            InsertUpFileTable(fileID,fileName,uploadDateTime);    //****************服务器：插入文件所有权表****************
        }

        //客户端和服务器端都有，请参考具体注释
        public void SubsequentUpload(long fileID)
        {
            //服务器：发起挑战
            int challengeLeafNode=0;
            int k = PoW.GenerateChallenge(MHTNum,leafNodeNum, ref challengeLeafNode);  
            Console.WriteLine("\n选中第" + k + "棵树的第" + challengeLeafNode + "个叶子节点作为挑战 (从0开始！)");

            //***********************通信：服务器将challengeLeafNode和k(MHT的编号)发给客户端**************************

            //客户端：生成响应
            List<string> ResponseNodeSet = PoW.GenerateResponse(saltsVal[k],challengeLeafNode,fileCiphertext); 

            //*************************通信：客户端将ResponseNodeSet发送给服务器*************************************

            //服务器：验证响应
            bool isPassPow = PoW.VerifyRepsonse(saltsVal[k], ResponseNodeSet, rootNode[k]); //CSP验证Response正确性

            //*************************通信：服务器将验证结果isPassPow发送给客户端**************************************

            if (isPassPow)
            {
                Console.WriteLine("用户通过了PoW验证!");

                string uploadDateTime = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
                InsertUpFileTable(fileID,fileName,uploadDateTime); //服务器：更新UploadFileTable
            }
            else
            {
                Console.WriteLine("用户未通过验证!");
            }
        }

        //服务器：将密文存到本地
        public void StoreCiphertext(byte[] fileCiphertext,string serAdd)
        {
            //为什么fileCiphertext明明是这个类的成员变量还要传参呢？
            //考虑到这个函数你可能会分到其他文件中（如果没分的话就把这个参数删掉吧）
            //下面这几个函数也是同理
        }

        //服务器：插入FileTable表，包含FileID,FileName,FileTag,MHTNum(MHT数量),FileSize,serAdd字段 【FileID是主键】
        //只有初始上传者需要插入
        public void InsertFileTable(ref long fileID,string fileName,string fileTag,int MHTNum,long fileSize,string serAdd)
        {
            
        }

        //服务器：插入MHTTable，包含FileID,MHTID,salt,rootNode【FileID + MHTID是主键】
        //只有初始上传者需要插入
        public void InsertMHTTable(long fileID,int MHTID,string salt,string rootNode)
        {

        }

        //服务器：插入UpFileTable(文件所有权表)，包含UserID,FileID,UserName,FileName,UploadTime 【UserID + FileID是主键】
        //获得文件所有权的初始上传者&后续上传者都需要插入
        //我把enMD5这个字段删掉了，我觉得可以用fileTag来取代
        public void InsertUpFileTable(long fileID,string fileName,string uploadDateTime)
        {
            //不知道UserID UserName怎么获取？所以没写这个参数。请后端大哥帮帮我加上~
        }
    }
}
