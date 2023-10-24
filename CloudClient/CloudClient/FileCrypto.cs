using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cloud
{
    class FileCrypto_2
    {
        public string encryptedFileDir;
        public string decryptedFileDir;
        string key;
        public FileCrypto_2(string e, string d, string k)
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

        public static string GetMD5(FileStream fs)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(fs);
            string res = string.Empty;
            foreach (var i in output)
                res += i.ToString("x2");
            return res;
        }

        public static string GetSHA1(FileStream fs)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] output = sha1.ComputeHash(fs);
            string res = string.Empty;
            foreach (var i in output)
                res += i.ToString("x2");
            return res;
        }

        public static string AESEncryptString(string input, string key)
        {
            byte[] bKey = Encoding.Default.GetBytes(key);
            byte[] binput = Encoding.Default.GetBytes(input);
            byte[] encrypted;
            using (Aes aes = Aes.Create())
            {
                aes.Key = bKey;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.ECB;
                ICryptoTransform encryptor = aes.CreateEncryptor();
                encrypted = encryptor.TransformFinalBlock(binput, 0, binput.Length);
            }
            string res = Convert.ToBase64String(encrypted);
            return res;
        }
        public static string AESDecryptString(string input, string key)
        {
            byte[] binput = Convert.FromBase64String(input);
            byte[] bKey = Encoding.Default.GetBytes(key);
            byte[] decrypted;
            using (Aes aes = Aes.Create())
            {
                aes.Key = bKey;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.ECB;
                ICryptoTransform decryptor = aes.CreateDecryptor();
                decrypted = decryptor.TransformFinalBlock(binput, 0, binput.Length);
            }
            string res = Encoding.Default.GetString(decrypted);
            return res;
        }

        private byte[] AESEncrypt(byte[] plainText)
        {
            if (plainText == null || plainText.Length <= 0)
                return null;
            if (string.IsNullOrEmpty(key))
            {
                //MessageBox.Show("Key is empty");
                return null;
            }
            byte[] bKey = Encoding.Default.GetBytes(key);
            if (bKey.Length % 32 != 0)
            {
                //MessageBox.Show("Key must be divided 32");
                return null;
            }

            byte[] encrypted;
            using (Aes aes = Aes.Create())
            {
                aes.Key = bKey;
                //aes.KeySize = bKey.Length * 8;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.ECB;
                ICryptoTransform encryptor = aes.CreateEncryptor();
                encrypted = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
            }
            return encrypted;
        }

        private byte[] AESDecrypt(byte[] cipherText)
        {
            if (cipherText == null || cipherText.Length <= 0)
                return null;
            if (string.IsNullOrEmpty(key))
            {
                //MessageBox.Show("Key is empty");
                return null;
            }
            byte[] bKey = Encoding.Default.GetBytes(key);
            if (bKey.Length % 32 != 0)
            {
                //MessageBox.Show("Key must be divided 32");
                return null;
            }
            byte[] decrypted;
            using (Aes aes = Aes.Create())
            {
                aes.Key = bKey;
                //aes.KeySize = bKey.Length;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.ECB;
                ICryptoTransform decryptor = aes.CreateDecryptor();
                decrypted = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            }
            return decrypted;
        }
        public string FileEncrypt(string rawFile)
        {
            //string ciphertextPath = "D:/杂项/加密后/" + DateTime.Now.ToString();
            SM4Utils sM4Utils = new SM4Utils(key); //SM4加密模块
            string enFileFullPath = encryptedFileDir + Path.GetFileName(rawFile);
            byte[] inbuffer = new byte[1024];
            int readCount;
            using (FileStream freader = new FileStream(rawFile, FileMode.Open))
            {
                using (FileStream fwriter = new FileStream(enFileFullPath, FileMode.Create))
                {
                    while ((readCount = freader.Read(inbuffer, 0, inbuffer.Length)) > 0)
                    {
                        if (readCount != inbuffer.Length)
                        {
                            ////MessageBox.Show(readCount);
                            byte[] tmp = new byte[readCount];
                            Buffer.BlockCopy(inbuffer, 0, tmp, 0, readCount);
                            //byte[] enbyte = AESEncrypt(tmp);
                            byte[] enbyte = sM4Utils.EncryptData(tmp);
                            fwriter.Write(enbyte, 0, enbyte.Length);
                            ////MessageBox.Show(enbyte.Length);
                        }
                        else
                        {
                            ////MessageBox.Show(inbuffer.Length);
                            //byte[] enbyte = AESEncrypt(inbuffer);
                            byte[] enbyte = sM4Utils.EncryptData(inbuffer);
                            fwriter.Write(enbyte, 0, enbyte.Length);
                            ////MessageBox.Show(enbyte.Length);
                        }
                    }
                    fwriter.Close();
                }
                freader.Close();
            }
            return enFileFullPath;
        }

        public void FileDecrypt(string enFile)
        {
            SM4Utils sM4Utils = new SM4Utils(key);
            string deFileFullPath = decryptedFileDir + Path.GetFileName(enFile);
            int readCount;
            byte[] deBuffer = new byte[1040];
            using (FileStream freader = new FileStream(enFile, FileMode.Open))
            {
                using (FileStream fwriter = new FileStream(deFileFullPath, FileMode.Create))
                {
                    while ((readCount = freader.Read(deBuffer, 0, deBuffer.Length)) > 0)
                    {
                        if (readCount != deBuffer.Length)
                        {
                            byte[] tmp = new byte[readCount];
                            Buffer.BlockCopy(deBuffer, 0, tmp, 0, readCount);
                            //byte[] debyte = AESDecrypt(tmp);
                            byte[] debyte = sM4Utils.DecryptData(tmp);
                            fwriter.Write(debyte, 0, debyte.Length);
                        }
                        else
                        {
                            //byte[] debyte = AESDecrypt(deBuffer);
                            byte[] debyte = sM4Utils.DecryptData(deBuffer);
                            fwriter.Write(debyte, 0, debyte.Length);
                        }
                    }
                }
            }
        }
    }
}
