﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Cloud
{
    class PoW
    {
        //服务器：生成挑战（MHT编号+leafNode编号-暂时固定位0号）
        public static int GenerateChallenge(int MHTNum, int leafNodeNum, ref int challengeLeafNode)
        {
            //后端大哥不要把我的函数参数leafNodeNum删掉！后面有用！！！
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

        //客户端：根据Challenge生成Response
        public static List<string> GenerateResponse(string salt, int challengeLeafNode, byte[] ciphetext)
        {
            List<string> list = MerkleHashTree.FileSplit(ciphetext); //用户对文件分块

            MerkleHashTree mht = new MerkleHashTree(list, salt, 1);  //生成MHT，带3个参数的构造函数是User的，2个参数的构造函数是CSP的！请不要改谢谢！！

            List<string> ResponseNodeSet = mht.GenerateResponseNodeSet(list, salt, challengeLeafNode);  //生成响应

            return ResponseNodeSet;
        }

        //服务器：判断用户的Response是否正确
        public static bool VerifyRepsonse(string salt, List<string> ResponseNodeSet, string rightRootNodeVal)
        {
            string rootNodeVal = MerkleHashTree.GenerateResponseRootNode(ResponseNodeSet, salt);

            return rootNodeVal == rightRootNodeVal;
        }
    }
}
