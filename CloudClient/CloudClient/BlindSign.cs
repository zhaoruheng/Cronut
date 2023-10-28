using NetPublic;
using System;
using System.Linq.Expressions;
using System.Numerics;
using System.Security.Cryptography;

namespace Cloud
{
    class BlindSign
    {
        public static string BlindSignature(byte[] hashValue, ClientComHelper clientComHelper, string username)
        {
            //客户端：选择一个指定的大素数p
            BigInteger p = new BigInteger(100001159);

            //客户端：随机生成 t∈Zp*
            BigInteger t = GenerateRandomBigInteger(p);
            t = BigInteger.Abs(t);

            //客户端: 计算 F' = t * h_F(mod p)
            BigInteger hashValueInteger = new BigInteger(hashValue);
            hashValueInteger = BigInteger.Abs(hashValueInteger);        //将哈希值转换成正整数
            BigInteger F_prime = (t * hashValueInteger) % p;

            //********************通信：客户端将F_prime发送给服务器*****************************
            NetPacket np = new NetPacket();
            np.F_prime = F_prime;
            np.userName = username;
            clientComHelper.MakeRequestPacket(np);
            clientComHelper.SendMsg();

            //******************通信：服务器将alpha_prime发给客户端*****************************
            np = clientComHelper.RecvMsg();
            BigInteger alpha_prime = np.F_prime;
            //客户端: 计算 t^{-1} * alpha_prime (mod p)
            BigInteger tInverse = CalculateModularInverse(t, p);
            BigInteger alpha = (tInverse * alpha_prime) % p;

            Console.WriteLine("签名Alpha: " + alpha);

            return alpha.ToString(); //客户端
        }

        //客户端：生成一个随机数t∈Zp*
        private static BigInteger GenerateRandomBigInteger(BigInteger p)
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[p.ToByteArray().Length];
            rng.GetBytes(randomBytes);

            BigInteger t = new BigInteger(randomBytes);
            t = BigInteger.Remainder(t, p) + 1;

            return t;
        }

        //客户端：计算逆元 t^{-1} (mod p)
        private static BigInteger CalculateModularInverse(BigInteger t, BigInteger p)
        {
            BigInteger tInverse = BigInteger.ModPow(t, p - 2, p);   //使用费马小定理计算mod p的逆元
            return tInverse;
        }
    }
}
