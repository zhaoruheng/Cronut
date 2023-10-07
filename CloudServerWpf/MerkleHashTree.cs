using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Cloud
{
    public class MerkleHashTree
    {
        Queue<string> hashQueue;    //客户端&服务器：用于建树的队列
        List<string> hashList;      //客户端&服务器：存放每个节点的哈希值
        string rootNodeVal;         //客户端&服务器：根节点哈希值
        string salt;                //客户端&服务器：这棵树对应的盐值
        int leafNodeNum;            //客户端&服务器：叶子节点数目

        //客户端的构造函数
        public MerkleHashTree(List<string>data, string salt,int code)
        {
            this.salt = salt;
            hashQueue = new Queue<string>();
            hashList = new List<string>();
            leafNodeNum = data.Count;
        }
       
        //服务器端的构造函数
        public MerkleHashTree(List<string> data,string salt)
        {
            this.salt = salt;                               //为这棵MHT对应的盐值赋值
            hashQueue = new Queue<string>();                        
            hashList = new List<string>();               
            leafNodeNum = data.Count;                           //总叶节点数

            //先把叶子节点全部入队
            for (int i = 0; i < leafNodeNum; ++i)
            {
                string content = ByteArrayToHexString(CalculateHash(Encoding.UTF8.GetBytes(data[i] + salt)));
                hashQueue.Enqueue(content);    //入队
                hashList.Add(content);          //加入哈希值列表中
            }
            
            BuildTree();        //创建MHT

            rootNodeVal = hashQueue.First();   //给根节点赋值
        }

        //服务器：建MHT
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
                    string content = ByteArrayToHexString(CalculateHash(HexStringToByteArray(combined)));  //进行哈希得到该节点的内容

                    newQueue.Enqueue(content);           //将新节点入队
                    hashList.Add(content);               //加入哈希表中
                }

                hashQueue = newQueue;      //_hashQueue就是下次要处理的队列了
            }
        }

        //客户端&服务器：对文件进行分块，不够用0填充
        public static List<string> FileSplit(byte[] data)
        {
            //确定文件块的大小
            int x = data.Length;
            int blockSize = Math.Max((int)(0.009*x),1);
            

            Console.WriteLine("密文的大小" + data.Length+" 文件块大小"+blockSize);

            //计算划分的块数 
            int blockCount = (data.Length + blockSize - 1) / blockSize;
            
            List<string> blocks = new List<string>();
            for(int i = 0; i < blockCount; ++i)
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

        //服务器：计算MHT数量
        public static int CalculateMHTNum(int x)
        {
            double y = Math.Max( 8.2215 / (1 + Math.Exp(2.4485 - (488.6871 / x))),1);
            return (int)Math.Round(y);
        }

        //服务器：生成x个随机盐值序列
        public static List<string> GenerateSalVal(int x,int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var salts = new List<string>();
            var salt = new char[length];

            using(var rng=new RNGCryptoServiceProvider())
            {
                for(int j=0;j<x ;++j)
                {
                    byte[] randomBytes = new byte[length];
                    rng.GetBytes(randomBytes);
                    for(int i=0;i<length ;++i)
                    {
                        salt[i] = chars[randomBytes[i] % chars.Length];
                    }
                    salts.Add(new string(salt));
                }
            }
            return salts;
        }

        //客户端&服务器端
        public string GetRootHash()
        {
            return rootNodeVal;
        }

        //客户端&服务器端
        public static byte[] CalculateHash(byte[] data)
        {
            SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(data);
            return hash;
        }

        //客户端&服务器端
        public static byte[] HexStringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length >> 1)
                                .Select(x => Convert.ToByte(hex.Substring(x << 1, 2), 16))
                                .ToArray();
        }

        //客户端&服务器端
        public static string ByteArrayToHexString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        //客户端：生成响应
        public List<string> GenerateResponseNodeSet(List<string> data, string salt, int challengeLeafNode)
        {
            List<string> result = new List<string>();

            //将叶子节点入队
            for (int i = 0; i < leafNodeNum; ++i)
            {
                string content = ByteArrayToHexString(CalculateHash(Encoding.UTF8.GetBytes(data[i] + salt)));
                hashQueue.Enqueue(content);
                hashList.Add(content);
            }

            Console.WriteLine("Response中的节点：");
            Console.WriteLine(challengeLeafNode + " hashList大小 "+hashList.Count+" 内容" + hashList[challengeLeafNode]);

            result.Add(hashList[challengeLeafNode]);
            if (challengeLeafNode % 2 == 0)
            {
                Console.WriteLine((challengeLeafNode + 1) + " hashList大小 " + hashList.Count + " 内容" + hashList[challengeLeafNode+1]);
                result.Add(hashList[challengeLeafNode + 1]);
            }
            else 
            {
                Console.WriteLine((challengeLeafNode - 1) + " hashList大小 " + hashList.Count + " 内容" + hashList[challengeLeafNode-1]);
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
                    string content = ByteArrayToHexString(CalculateHash(HexStringToByteArray(combined)));  //进行哈希得到该节点的内容

                    newQueue.Enqueue(content);           //将新节点入队
                    hashList.Add(content);               //加入哈希表中
                }

                //将这一层的兄弟节点加入Response集合
                if (challengeLeafNode % 2 == 0)
                {
                    Console.WriteLine((challengeLeafNode + 1) + " hashList大小 "+hashList.Count + " 内容" + hashList[challengeLeafNode+1]);
                    result.Add(hashList[challengeLeafNode + 1]);
                }
                else
                {
                    Console.WriteLine((challengeLeafNode - 1) + " hashList大小 " + hashList.Count + " 内容" + hashList[challengeLeafNode-1]);
                    result.Add(hashList[challengeLeafNode - 1]);
                }
                challengeLeafNode /= 2;

                hashQueue = newQueue;      //hashQueue就是下次要处理的队列了
                //hashList.Clear();           //将hashList清空
            }

            Console.WriteLine("\n返回的Response集合的元素个数：" + result.Count);
            return result;
        }

        //服务器端：根据客户端的Response生成对应的根节点
        public static string GenerateResponseRootNode(List<string> ResponseNodeSet, string salt)
        {
            string combined = ResponseNodeSet[0] + ResponseNodeSet[1] + ByteArrayToHexString(Encoding.UTF8.GetBytes(salt)); 
            string content = ByteArrayToHexString(CalculateHash(HexStringToByteArray(combined)));

            for(int i=2;i<ResponseNodeSet.Count ;++i)
            {
                combined = content + ResponseNodeSet[i] + ByteArrayToHexString(Encoding.UTF8.GetBytes(salt));
                content = ByteArrayToHexString(CalculateHash(HexStringToByteArray(combined)));
            }

            Console.WriteLine("计算的Response集合得到的根节点: " + content);
            return content;
        }
    }
}

