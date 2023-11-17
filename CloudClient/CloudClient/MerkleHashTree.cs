using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Cloud
{
    public class MerkleHashTree
    {
        Queue<string> hashQueue;    //用于建树的队列
        List<string> hashList;      //存放每个节点的哈希值
        string rootNodeVal;         //根节点哈希值
        string salt;                //这棵树对应的盐值
        int leafNodeNum;            //叶子节点数目
        string oneleafNode;         //只有一个叶子节点特别处理

        public MerkleHashTree(List<string> data, string salt, int code)
        {
            this.salt = salt;
            hashQueue = new Queue<string>();
            hashList = new List<string>();
            leafNodeNum = data.Count;
            oneleafNode = data.First();
        }

        //客户端&服务器：对文件进行分块，不够用0填充
        public static List<string> FileSplit(byte[] data)
        {
            int x = data.Length;
            int blockSize = Math.Max((int)(0.009 * x+500), 2);

            int blockCount = (data.Length + blockSize - 1) / blockSize;

            List<string> blocks = new List<string>();
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

        public static byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length >> 1)
                                .Select(x => Convert.ToByte(hex.Substring(x << 1, 2), 16))
                                .ToArray();
        }

        public static string ByteArrayToHexString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public List<string> GenerateResponseNodeSet(List<string> data, string salt, int challengeLeafNode)
        {
            List<string> result = new List<string>();

            if (leafNodeNum == 1)
            {
                result.Add(ByteArrayToHexString(FileCrypto.CalculateSM3Hash(Encoding.UTF8.GetBytes(data[0]+salt))));
                return result;
            }

            //将叶子节点入队
            for (int i = 0; i < leafNodeNum; ++i)
            {
                string content = ByteArrayToHexString(FileCrypto.CalculateSM3Hash(Encoding.UTF8.GetBytes(data[i] + salt)));
                hashQueue.Enqueue(content);
                hashList.Add(content);
            }

            result.Add(hashList[challengeLeafNode]);
            if (challengeLeafNode % 2 == 0)
            {
                result.Add(hashList[challengeLeafNode + 1]);
            }
            else
            {
                result.Add(hashList[challengeLeafNode - 1]);
            }
            challengeLeafNode /= 2;
            hashList.Clear();

            while (hashQueue.Count > 2)
            {
                int count = hashQueue.Count;   //当前层的节点数

                if (count % 2 == 1)     //预处理：如果这一层节点个数为奇数，复制最后一个节点
                {
                    hashQueue.Enqueue(hashQueue.Last());
                    hashList.Add(hashQueue.Last());
                    count++;
                }
                hashList.Clear();

                var newQueue = new Queue<string>(count / 2);    //申请一个临时队列，用于创建要 生成层 的节点

                for (int i = 0; i < count; i += 2)
                {
                    string left = hashQueue.Dequeue();
                    string right = hashQueue.Dequeue();
                    string combined = left + right + ByteArrayToHexString(Encoding.UTF8.GetBytes(salt));    //左孩子+右孩子+salt
                    string content = ByteArrayToHexString(FileCrypto.CalculateSM3Hash(HexStringToByteArray(combined)));  //进行哈希得到该节点的内容

                    newQueue.Enqueue(content);           //将新节点入队
                    hashList.Add(content);               //加入哈希表中
                }

                //将这一层的兄弟节点加入Response集合
                if (challengeLeafNode % 2 == 0)
                {
                    result.Add(hashList[challengeLeafNode + 1]);
                }
                else
                {
                    result.Add(hashList[challengeLeafNode - 1]);
                }
                challengeLeafNode /= 2;

                hashQueue = newQueue;      //hashQueue就是下次要处理的队列了
            }

            return result;
        }

        //服务器端：根据客户端的Response生成对应的根节点
        public static string GenerateResponseRootNode(List<string> ResponseNodeSet, string salt)
        {
            string combined = ResponseNodeSet[0] + ResponseNodeSet[1] + ByteArrayToHexString(Encoding.UTF8.GetBytes(salt));
            string content = ByteArrayToHexString(FileCrypto.CalculateSM3Hash(HexStringToByteArray(combined)));

            for (int i = 2; i < ResponseNodeSet.Count; ++i)
            {
                combined = content + ResponseNodeSet[i] + ByteArrayToHexString(Encoding.UTF8.GetBytes(salt));
                content = ByteArrayToHexString(FileCrypto.CalculateSM3Hash(HexStringToByteArray(combined)));
            }

            return content;
        }
    }
}

