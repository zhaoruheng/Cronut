using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using NetPublic;
using System.Runtime.Serialization.Formatters.Binary;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using System.Diagnostics;

///服务器端
namespace Cloud
{
    class FileCrypto
    {
        string fileDir = string.Empty;
        string readdir=string.Empty;
        string fileName = string.Empty;
        long fileSize;
        string fileTag = string.Empty;
        string fileEncryptKey = string.Empty;
        const string keyPrivate = "123456789";
        byte[] fileCiphertext;
        const string aesIV = "446C47278157A448F925084476F2A5C2";
        public string encryptedFileDir;
        public string decryptedFileDir;
        string key;

        ClientComHelper clientComHelper;
        string userName = string.Empty;
        public delegate void DelegateEventHander();
        public DelegateEventHander ReturnMsg;

        public FileCrypto(string path, ClientComHelper client, string userName)
        {
            fileDir = path;
            clientComHelper = client;
            this.userName = userName;
        }
        public FileCrypto(string path, string readdir, ClientComHelper client, string userName, string enkey)
        {
            fileDir = path;
            this.readdir = readdir;
            clientComHelper = client;
            this.userName = userName;
            fileEncryptKey = enkey;
        }

        public FileCrypto(string e, string d, string k)
        {
            encryptedFileDir = e;
            decryptedFileDir = d;
            MD5 md5 = MD5.Create();
            if (k.Length == 32)
                key = k;
            else
            {
                key = "12345678123456781234567812345678";
                //MessageBox.Show("由于key格式不正确，被指定为默认值");
            }
        }

        public byte FileUpload()
        {
            string blindSignature;

            byte[] hashValue = CalculateSHA1();

            blindSignature = BlindSign.BlindSignature(hashValue, clientComHelper, userName);

            fileEncryptKey = MerkleHashTree.ByteArrayToHexString(CalculateSM3Hash(System.Text.Encoding.UTF8.GetBytes(blindSignature)));

            fileCiphertext = SM4Encrypt();

            fileTag = GetFileTag();

            clientComHelper.MakeRequestPacket(NetPublic.DefindedCode.OK, userName, null, fileSize, fileTag, Path.GetFileName(fileDir), null, null, 0, fileEncryptKey);

            clientComHelper.SendMsg();

            NetPacket np = clientComHelper.RecvMsg();
            if (np.code == NetPublic.DefindedCode.AGREEUP)
            {
                InitialUpload();
            }
            else if (np.code == NetPublic.DefindedCode.FILEEXISTED)
            {
                SubsequentUpload(np.userType);
            }
            ReturnMsg?.Invoke();

            System.IO.DirectoryInfo topDir = System.IO.Directory.GetParent(fileDir);
            string pathto = topDir.FullName;

            string filePath = Path.Combine(pathto, "." + fileTag);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return np.code;
        }

        byte[] CalculateSHA1()
        {

            using (FileStream fileStream = new FileStream(fileDir, FileMode.Open, FileAccess.Read))
            {
                SHA1 sha1 = SHA1.Create();
                byte[] hashValue = sha1.ComputeHash(fileStream);

                return hashValue;
            }
        }

        public static byte[] CalculateSM3Hash(byte[] input)
        {
            SM3Digest digest = new SM3Digest();
            digest.BlockUpdate(input, 0, input.Length);
            byte[] result = new byte[digest.GetDigestSize()];
            digest.DoFinal(result, 0);
            return result;
        }

        //客户端：对密文进行解密，将明文重新写入原文件
        public void FileDownload()
        {
            fileCiphertext = ReadFileContent2(readdir);
            File.Delete(readdir);
            byte[] plaintext = SM4Decrypt();

            try
            {
                using (FileStream fs = new FileStream(fileDir, FileMode.Create))
                {
                    fs.Write(plaintext, 0, plaintext.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("写入文件时出错：" + ex.Message);
            }
            
            if (File.Exists(readdir))
            {
                File.Delete(readdir);
            }
        }

        public byte[] ReadFileContent()
        {
            FileInfo fileInfo = new FileInfo(fileDir);
            fileSize = fileInfo.Length;
            fileName = fileInfo.Name;

            byte[] fileContent = new byte[fileInfo.Length + (long)100];

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

        public byte[] ReadFileContent2(string workpath)
        {
            FileInfo fileInfo = new FileInfo(workpath);
            fileSize = fileInfo.Length;
            fileName = fileInfo.Name;

            byte[] fileContent = new byte[fileInfo.Length + 100];

            try
            {
                using (FileStream fileStream = new FileStream(workpath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        int fileSize = (int)new FileInfo(workpath).Length;
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

        public byte[] SM4Encrypt()
        {
            Debug.WriteLine(fileEncryptKey);
            string keyString = fileEncryptKey;
            byte[] input = ReadFileContent();

            if (keyString.Length > 16)
            {
                keyString = keyString.Substring(0, 16); // 如果超过16字符，截断
            }
            else if (keyString.Length < 16)
            {
                keyString = keyString.PadRight(16, '0'); // 如果不足16字符，用'0'填充
            }
            byte[] keyBytes = Encoding.UTF8.GetBytes(keyString);

            // SM4参数设置，这里使用ECB模式和PKCS7Padding
            KeyParameter keyParam = new KeyParameter(keyBytes);
            SM4Engine engine = new SM4Engine();
            PaddedBufferedBlockCipher cipher = new PaddedBufferedBlockCipher(engine);
            cipher.Init(true, keyParam); // true表示加密模式

            // 加密
            byte[] outputBytes = new byte[cipher.GetOutputSize(input.Length)];
            int length1 = cipher.ProcessBytes(input, 0, input.Length, outputBytes, 0);
            cipher.DoFinal(outputBytes, length1); // 结束加密操作

            return outputBytes;
        }

        public byte[] FileEncrypt()
        {
            int keySize = 256;  //设置密钥长度，因为密钥长度不一定符合要求，需要resize
            int ivSize = 128;   //设置iv长度，因为iv长度不一定符合要求，也需要resize
            byte[] encryptedContent;

            byte[] fileContent = ReadFileContent();

            using (Aes aes = Aes.Create())
            {
                //获取正确的Key
                byte[] keyBytes = Encoding.UTF8.GetBytes(fileEncryptKey);
                Array.Resize(ref keyBytes, keySize / 8);
                aes.Key = keyBytes;

                //获取正确的IV
                byte[] ivBytes = Encoding.UTF8.GetBytes(aesIV);
                Array.Resize(ref ivBytes, ivSize / 8);
                aes.IV = ivBytes;

                //设置CBC模式
                aes.Mode = CipherMode.CBC;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream encryptor = new CryptoStream(memoryStream, aes.CreateEncryptor(aes.Key, aes.IV), CryptoStreamMode.Write))
                    {
                        encryptor.Write(fileContent, 0, fileContent.Length);
                    }

                    encryptedContent = memoryStream.ToArray();
                }
            }
            return encryptedContent;
        }

        public byte[] SM4Decrypt()
        {
            string keyString = fileEncryptKey;
            byte[] input = fileCiphertext;

            if (keyString.Length > 16)
            {
                keyString = keyString.Substring(0, 16); // 如果超过16字符，截断
            }
            else if (keyString.Length < 16)
            {
                keyString = keyString.PadRight(16, '0'); // 如果不足16字符，用'0'填充
            }
            byte[] keyBytes = Encoding.UTF8.GetBytes(keyString);

            // SM4参数设置，这里使用ECB模式和PKCS7Padding
            KeyParameter keyParam = new KeyParameter(keyBytes);
            SM4Engine engine = new SM4Engine();
            PaddedBufferedBlockCipher cipher = new PaddedBufferedBlockCipher(engine);
            cipher.Init(false, keyParam); // false表示解密模式

            // 解密
            byte[] outputBytes = new byte[cipher.GetOutputSize(input.Length)];
            int length1 = cipher.ProcessBytes(input, 0, input.Length, outputBytes, 0);
            int length2 = cipher.DoFinal(outputBytes, length1); // 结束解密操作

            // 移除可能的padding
            byte[] result = new byte[length1 + length2];
            Array.Copy(outputBytes, result, result.Length);

            return result;
        }

        public byte[] FileDecrypt()
        {
            byte[] cipherBytes = fileCiphertext;
            int keySize = 256;
            int ivSize = 128;

            using (Aes aes = Aes.Create())
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(fileEncryptKey);
                Array.Resize(ref keyBytes, keySize / 8);
                aes.Key = keyBytes;

                byte[] ivBytes = Encoding.UTF8.GetBytes(aesIV);
                Array.Resize(ref ivBytes, ivSize / 8);
                aes.IV = ivBytes;

                aes.Mode = CipherMode.CBC;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);
                    cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
                    cryptoStream.FlushFinalBlock();

                    byte[] fileBytes = memoryStream.ToArray();

                    return fileBytes;
                }
            }
        }

        public string GetFileTag()
        {
            fileTag = MerkleHashTree.ByteArrayToHexString(CalculateSM3Hash(fileCiphertext));
            return fileTag;
        }

        public void InitialUpload()
        {
            System.IO.DirectoryInfo topDir = System.IO.Directory.GetParent(fileDir);
            string pathto = topDir.FullName;

            string filePath = Path.Combine(pathto, "." + fileTag);
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    fs.Write(fileCiphertext, 0, fileCiphertext.Length);
                }
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
            string filePath = Path.Combine(pathto, "." + fileTag);

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    fs.Write(ResponseNode, 0, ResponseNode.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("写入文件时出错：" + ex.Message);
            }
            clientComHelper.SendFile(filePath);
        }

        public void SubsequentUpload(long fileID)
        {
            NetPacket np = clientComHelper.RecvMsg();
            int challengeLeafNode = np.challengeLeafNode;
            int k = np.MHTID;

            List<string> ResponseNodeSet = PoW.GenerateResponse(np.enKey, challengeLeafNode, fileCiphertext);

            byte[] ResponseNode = ListStringToByteArray(ResponseNodeSet);
            SendResponseNode(ResponseNode);

            np = clientComHelper.RecvMsg();

            if (np.code == NetPublic.DefindedCode.AGREEUP)
            {
                string uploadDateTime = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
            }
            else
            {
                Console.WriteLine("用户未通过验证!");
            }
        }

        public static string GetMD5(string input)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] tmp = Encoding.Default.GetBytes(input);
            byte[] output = md5.ComputeHash(tmp);
            string res = string.Empty;
            foreach (var i in output)
                res += i.ToString("x2");
            return res;
        }

        public static byte[] ListStringToByteArray(List<string> stringList)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
#pragma warning disable SYSLIB0011
                formatter.Serialize(memoryStream, stringList);
#pragma warning restore SYSLIB0011

                return memoryStream.ToArray();
            }
        }
    }
}
