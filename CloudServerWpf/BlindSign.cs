using System;
using System.Linq.Expressions;
using System.Numerics;
using System.Security.Cryptography;

namespace Cloud
{
    internal class BlindSign
    {
        public static BigInteger BlindSignature(BigInteger F_prime)
        {
                  
            //服务器：选取 r∈Zp*
            BigInteger r = 15;

            //服务器: alpha_prime = r * F_prime (mod p)
            BigInteger alpha_prime = (r * F_prime) % p;

            return alpha_prime;
        }
    }
}
