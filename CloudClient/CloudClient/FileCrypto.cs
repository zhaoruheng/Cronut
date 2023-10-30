using System;
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
    }
}
