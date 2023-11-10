using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Cloud
{
    internal class BlindSign
    {
        public static BigInteger BlindSignature(BigInteger F_prime)
        {
            BigInteger p = new(100001159);
            BigInteger r = 15;

            BigInteger alpha_prime = r * F_prime % p;

            return alpha_prime;
        }
    }
}
