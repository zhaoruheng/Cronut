﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;

namespace Cloud
{
    public class MerkleHashTree
    {
        private Queue<string> hashQueue;    //用于建树的队列
        private readonly List<string> hashList;      //存放每个节点的哈希值
        private readonly string rootNodeVal;         //根节点哈希值
        private readonly string salt;                //这棵树对应的盐值
        private readonly int leafNodeNum;            //叶子节点数目

        public MerkleHashTree(List<string> data, string salt, int code)
        {
            this.salt = salt;
            hashQueue = new Queue<string>();
            hashList = new List<string>();
            leafNodeNum = data.Count;
        }

        public MerkleHashTree(List<string> data, string salt)
        {
            this.salt = salt;
            hashQueue = new Queue<string>();
            hashList = new List<string>();
            leafNodeNum = data.Count;

            //先把叶子节点全部入队
            for (int i = 0; i < leafNodeNum; ++i)
            {
                string content = ByteArrayToHexString(CalculateSM3Hash(Encoding.UTF8.GetBytes(data[i] + salt)));
                hashQueue.Enqueue(content);
                hashList.Add(content);
            }

            BuildTree();        //创建MHT
            rootNodeVal = hashQueue.First();   //给根节点赋值
        }

        private void BuildTree()
        {
            while (hashQueue.Count > 1)
            {
                int count = hashQueue.Count;   //当前层的节点数

                if (count % 2 == 1)     //预处理：如果这一层节点个数为奇数，复制最后一个节点
                {
                    hashQueue.Enqueue(hashQueue.Last());
                    hashList.Add(hashQueue.Last());
                    count++;    //count一定是偶数
                }

                var newQueue = new Queue<string>(count / 2);    //申请一个临时队列，用于创建要 生成层 的节点

                for (int i = 0; i < count; i += 2)
                {
                    string left = hashQueue.Dequeue();
                    string right = hashQueue.Dequeue();
                    string combined = left + right + ByteArrayToHexString(Encoding.UTF8.GetBytes(salt));    //左孩子+右孩子+salt
                    string content = ByteArrayToHexString(CalculateSM3Hash(HexStringToByteArray(combined)));  //进行哈希得到该节点的内容

                    newQueue.Enqueue(content);           //将新节点入队
                    hashList.Add(content);               //加入哈希表中
                }

                hashQueue = newQueue;      //_hashQueue就是下次要处理的队列了
            }
        }

        //客户端&服务器：对文件进行分块，不够用0填充
        public static List<string> FileSplit(byte[] data)
        {
            int x = data.Length;
            int blockSize = Math.Max((int)(0.009 * x+500), 2);
            int blockCount = (data.Length + blockSize - 1) / blockSize;

            List<string> blocks = new();
            for (int i = 0; i < blockCount; ++i)
            {
                int startIndex = i * blockSize;
                int count = Math.Min(blockSize, data.Length - startIndex);
                byte[] block = new byte[blockSize];
                Array.Copy(data, startIndex, block, 0, count);

                string blockStr = BitConverter.ToString(block);
                blocks.Add(blockStr);
            }
            return blocks;
        }

        //计算MHT数量
        public static int CalculateMHTNum(int x)
        {
            double y = Math.Max(23.2215 / (1 + Math.Exp(2.4485 - (488.6871 / x))), 1);
            return (int)Math.Round(y);
        }

        //生成x个随机盐值序列
        public static List<string> GenerateSalVal(int x, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var salts = new List<string>();
            var salt = new char[length];

            using (var rng = new RNGCryptoServiceProvider())
            {
                for (int j = 0; j < x; ++j)
                {
                    byte[] randomBytes = new byte[length];
                    rng.GetBytes(randomBytes);
                    for (int i = 0; i < length; ++i)
                    {
                        salt[i] = chars[randomBytes[i] % chars.Length];
                    }
                    salts.Add(new string(salt));
                }
            }
            return salts;
        }

        public string GetRootHash()
        {
            return rootNodeVal;
        }

        public static byte[] CalculateSM3Hash(byte[] input)
        {
            SM3Digest digest = new SM3Digest();
            digest.BlockUpdate(input, 0, input.Length);
            byte[] result = new byte[digest.GetDigestSize()];
            digest.DoFinal(result, 0);
            return result;
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length >> 1)
                                .Select(x => Convert.ToByte(hex.Substring(x << 1, 2), 16))
                                .ToArray();
        }

        public static string ByteArrayToHexString(byte[] bytes)
        {
            StringBuilder sb = new();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        //根据客户端的Response生成对应的根节点
        public static string GenerateResponseRootNode(List<string> ResponseNodeSet, string salt)
        {
            if (ResponseNodeSet.Count == 1)
            {
                return ResponseNodeSet[0];
            }

            string combined = ResponseNodeSet[0] + ResponseNodeSet[1] + ByteArrayToHexString(Encoding.UTF8.GetBytes(salt));
            string content = ByteArrayToHexString(CalculateSM3Hash(HexStringToByteArray(combined)));

            for (int i = 2; i < ResponseNodeSet.Count; ++i)
            {
                combined = content + ResponseNodeSet[i] + ByteArrayToHexString(Encoding.UTF8.GetBytes(salt));
                content = ByteArrayToHexString(CalculateSM3Hash(HexStringToByteArray(combined)));
            }

            return content;
        }
    }
}
