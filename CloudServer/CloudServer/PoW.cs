using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Cloud
{
    class PoW
    {
        //生成挑战（MHT编号+leafNode编号-暂时固定位0号）
        public static int GenerateChallenge(int MHTNum, int leafNodeNum, ref int challengeLeafNode)
        {
            int chooseMHT;

            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())  //随机选择一棵MHT
            {
                byte[] data = new byte[5];
                rng.GetBytes(data);
                int value = BitConverter.ToInt32(data, 0);
                chooseMHT = Math.Abs(value % MHTNum);
            }

            challengeLeafNode = 0;
            return chooseMHT;
        }

        //判断用户的Response是否正确
        public static bool VerifyRepsonse(string salt, List<string> ResponseNodeSet, string rightRootNodeVal)
        {
            string rootNodeVal = MerkleHashTree.GenerateResponseRootNode(ResponseNodeSet, salt);

            return rootNodeVal == rightRootNodeVal;
        }
    }
}
