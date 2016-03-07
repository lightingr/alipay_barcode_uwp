using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace Jeffreye.Alipay.BarcodePayment.Utils
{
    /// <summary>
    /// RFC4226 - HOTP: An HMAC-Based One-Time Password Algorithm
    /// http://tools.ietf.org/html/rfc4226
    /// </summary>
    public static class Hotp
    {
        /// <summary>
        /// Takes a counter and produces an HOTP value
        /// </summary>
        /// <param name="key">The secret key to use in HOTP calculations</param>
        /// <param name="counter">the counter to be incremented each time this method is called</param>
        /// <param name="digitals">all of the HOTP values are six digits long</param>
        /// <returns></returns>
        public static string Compute(byte[] key,long counter,int digitals = 6)
        {
            var hashData = GetBigEndianBytes(counter);
            var rawValue = HmacSha1(key,hashData);
            return Digits(rawValue, digitals);
        }
        
        /// <summary>
        /// converts a long into a big endian byte array.
        /// </summary>
        /// <remarks>
        /// RFC 4226 specifies big endian as the method for converting the counter to data to hash.
        /// </remarks>
        static byte[] GetBigEndianBytes(long input)
        {
            var data = BitConverter.GetBytes(input);

            // Since .net uses little endian numbers, we need to reverse the byte order to get big endian.
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);


            return data;
        }

        /// <summary>
        /// Calculates OTPs
        /// </summary>
        static long HmacSha1(byte[] key, byte[] value)
        {
            MacAlgorithmProvider provider = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);
            IBuffer keyMaterial = CryptographicBuffer.CreateFromByteArray(key);
            var cKey = provider.CreateKey(keyMaterial);

            byte[] hmacComputedHash;

            IBuffer data = CryptographicBuffer.CreateFromByteArray(value);
            IBuffer buffer = CryptographicEngine.Sign(cKey, data);

            CryptographicBuffer.CopyToByteArray(buffer, out hmacComputedHash);
            

            // The RFC has a hard coded index 19 in this value.
            // This is the same thing but also accomodates SHA256 and SHA512
            // hmacComputedHash[19] => hmacComputedHash[hmacComputedHash.Length - 1]

            int offset = hmacComputedHash[hmacComputedHash.Length - 1] & 0xF;
                        
            return 
                ((hmacComputedHash[offset] & 0x7f) << 24
                | (hmacComputedHash[offset + 1] ) << 16
                | (hmacComputedHash[offset + 2] ) << 8
                | (hmacComputedHash[offset + 3] )) ;
        }


        /// <summary>
        /// truncates a number down to the specified number of digits
        /// </summary>
        static string Digits(long input, int digitCount)
        {
            var truncatedValue = ((int)input % (int)Math.Pow(10, digitCount));
            return truncatedValue.ToString().PadLeft(digitCount, '0');
        }
    }
}
