using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Cloud
{
    class BlindSign
    {
        public static BigInteger BlindSignature(BigInteger F_prime)
        {
            BigInteger p = new BigInteger(100001159);
            //服务器：选取 r∈Zp*
            BigInteger r = 15;

            //服务器: alpha_prime = r * F_prime (mod p)
            BigInteger alpha_prime = (r * F_prime) % p;

            return alpha_prime;
        }
    }
}
