using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Windows.Input;
using NetPublic;
using static Cloud.FileWatcher;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;

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
        ClientComHelper clientComHelper;
        string userName;
        public delegate void DelegateEventHander();
        public DelegateEventHander ReturnMsg;

        //客户端
        public FileCrypto(string path,ClientComHelper client, string userName)
        {
            fileDir = path;
            rootNode = new List<string>();
            saltsVal = new List<string>();
            clientComHelper = client;
            this.userName = userName;
        }
        public FileCrypto(string path, ClientComHelper client, string userName,string enkey)
        {
            fileDir = path;
            rootNode = new List<string>();
            saltsVal = new List<string>();
            clientComHelper = client;
            this.userName = userName;
            fileEncryptKey = enkey;
        }

        //客户端和服务器端都有
        public byte FileUpload()
        {
            string blindSignature;  //客户端变量

            byte[] hashValue = CalculateSHA1();    //客户端

            blindSignature = BlindSign.BlindSignature(hashValue,clientComHelper,userName);   //客户端和服务器端都有，请点开BlindSignature这个函数，里面有更具体的内容

            fileEncryptKey = Encoding.UTF8.GetString(CalculateSHA256(blindSignature)); //客户端：计算加密密钥

            fileCiphertext = FileEncrypt();   //客户端：AES加密

            fileTag = GetFileTag();   //客户端：计算文件标签

            Console.WriteLine("文件标签" + fileTag);

            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.OK, userName, null, fileSize, fileTag, Path.GetFileName(fileDir), null, null, 0,fileEncryptKey);

            clientComHelper.SendMsg();
            //**********************通信：服务器发消息给客户端告诉你是初始上传者******************************

            //**********************通信：客户端将密文fileCiphertext上传给服务器******************************

            //InitialUpload();   //执行初始上传者的操作。客户端和服务器都各自有一部分，需要您点开再分呢~
            //}
            //else
            //{
            //Console.WriteLine("该用户为后续上传者");
            //SubsequentUpload(userType);                         //执行后续上传者的操作
            //}

            NetPacket np = clientComHelper.RecvMsg();
            if (np.code == NetPublic.DefindedCode.AGREEUP)
            {
                InitialUpload();
                ////MessageBox.Show("初始上传者完成");
            }
            else if(np.code == NetPublic.DefindedCode.FILEEXISTED)
            {
                SubsequentUpload(np.userType);      
            }
            ReturnMsg?.Invoke();

            //MessageBox.Show("客户端：文件上传结束");
            return np.code;
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

        //客户端：对密文进行解密，将明文重新写入原文件
        public void FileDownload()
        {
            fileCiphertext= ReadFileContent();
            byte[] plaintext = FileDecrypt();

            try
            {
                // 使用FileStream创建文件流
                using (FileStream fs = new FileStream(fileDir, FileMode.Create))
                {
                    // 使用Write方法将byte数组写入文件流
                    fs.Write(plaintext, 0, plaintext.Length);
                }

                Console.WriteLine("文件写入成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine("写入文件时出错：" + ex.Message);
            }

            Console.WriteLine("\n--------------对文件进行解密----------------");
            Console.WriteLine("解密文件路径:" + fileDir);
            Console.WriteLine("解密后的明文:");
            Console.WriteLine(Encoding.UTF8.GetString(plaintext));
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
            System.IO.DirectoryInfo topDir = System.IO.Directory.GetParent(fileDir);
            string pathto = topDir.FullName;

            string filePath = Path.Combine(pathto, fileTag);

            try
            {
                // 使用FileStream创建文件流
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
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
            clientComHelper.SendFile(filePath);
        }

        public void SendResponseNode(byte[] ResponseNode)
        {
            System.IO.DirectoryInfo topDir = System.IO.Directory.GetParent(fileDir);
            string pathto = topDir.FullName;
            string filePath = Path.Combine(pathto, fileTag);

            try
            {
                // 使用FileStream创建文件流
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    // 使用Write方法将byte数组写入文件流
                    fs.Write(ResponseNode, 0, ResponseNode.Length);
                }

                Console.WriteLine("文件写入成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine("写入文件时出错：" + ex.Message);
            }

            clientComHelper.SendFile(filePath);
        }


        //客户端和服务器端都有，请参考具体注释
        public void SubsequentUpload(long fileID)
        {

            //***********************通信：服务器将challengeLeafNode和k(MHT的编号)发给客户端**************************
            NetPacket np = clientComHelper.RecvMsg();
            int challengeLeafNode = np.challengeLeafNode;
            int k = np.MHTID;

            //客户端：生成响应
            List<string> ResponseNodeSet = PoW.GenerateResponse(np.enKey,challengeLeafNode,fileCiphertext);

            //*************************通信：客户端将ResponseNodeSet发送给服务器*************************************
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //NetPacket npR = new NetPacket();
            //npR.ResponseNodeSet = ResponseNodeSet;
            //npR.userName = userName;
            //clientComHelper.MakeRequestPacket(npR);
            //clientComHelper.SendMsg();

            byte[] ResponseNode = ListStringToByteArray(ResponseNodeSet);
            SendResponseNode(ResponseNode);
            //MessageBox.Show("客户端成功将Response发送！");


            //*************************通信：服务器将验证结果isPassPow发送给客户端**************************************
            np = clientComHelper.RecvMsg();

            if (np.code == NetPublic.DefindedCode.AGREEUP)
            {
                //MessageBox.Show("用户通过了PoW验证!");

                string uploadDateTime = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
            }
            else
            {
                Console.WriteLine("用户未通过验证!");
            }
        }

        public static byte[] ListStringToByteArray(List<string> stringList)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, stringList);
                return memoryStream.ToArray();
            }
        }
    }
}
