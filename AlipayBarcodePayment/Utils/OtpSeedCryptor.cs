using System;
using System.Diagnostics;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace Jeffreye.Alipay.BarcodePayment.Utils
{
    /// <summary>
    /// port of com.alipay.mobile.security.otp.service.otpOtpSeedCryptor
    /// </summary>
    public static class OtpSeedCryptor
    {
        public static int GetJavaHashCode(this string str)
        {
            var count = str.Length;
            var offset = 0;
            if (count > 0)
            {
                int h = 0;
                int off = offset;
                int len = count;


                for (int i = 0; i < len; i++)
                {
                    h = 31 * h + str[off++];
                }
                return h;
            }
            return 0;
        }

        public static byte[] Sha1(string value)
        {
            HashAlgorithmProvider provider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);
            var data = CryptographicBuffer.ConvertStringToBinary(value, BinaryStringEncoding.Utf8);
            var hash = provider.HashData(data);

            byte[] result;
            CryptographicBuffer.CopyToByteArray(hash, out result);
            return result;
        }

        static String f48a = "OtpSeedCryptor";
        private static byte[] f49b;
        private static String lastSeed = "";

        static OtpSeedCryptor()
        {
            unchecked
            {

                byte[] bArr = new byte[] { (byte)-61, (byte)-40, (byte)-52, (byte)-62, (byte)-39, (byte)-52, (byte)-42, (byte)-39, (byte)-58, (byte)-46, (byte)-59, (byte)-42, (byte)-59, (byte)-46, (byte)-127, (byte)-127, (byte)-127, (byte)-38, (byte)-51, (byte)-61, (byte)-62, (byte)-62, (byte)-51, (byte)-51 };

                f49b = bArr;
                for (int i = 0; i < 24; i++)
                {
                    bArr[i] = (byte)(bArr[i] ^ (byte)-96);
                }
            }
        }


        private static String encryptCore(String encryptedSeed)
        {
            if (encryptedSeed == null)
            {
                return null;
            }
            int length = encryptedSeed.Length;
            if (length < 8)
            {
                return null;
            }
            String toLowerCase = encryptedSeed.ToLower();
            for (int i = 0; i < length; i++)
            {
                if (getIndexOfChar(toLowerCase[i], "0123456789abcdef") == -1)
                {
                    return null;
                }
            }
            byte[] bytes = Encoding.UTF8.GetBytes(toLowerCase);
            try
            {
                //Key secretKeySpec = new SecretKeySpec(m29a(), "DESede");
                //Cipher instance = Cipher.getInstance("DESede/ECB/NOPADDING");
                //instance.init(1, secretKeySpec);
                //bytes = instance.doFinal(bytes);
                var input = CryptographicBuffer.CreateFromByteArray(bytes);
                var instance = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.TripleDesEcb);
                var secretKeySpec = instance.CreateSymmetricKey(CryptographicBuffer.CreateFromByteArray(generateKey()));
                var output = CryptographicEngine.Encrypt(secretKeySpec, input, null);
                CryptographicBuffer.CopyToByteArray(output, out bytes);

                if (bytes == null || bytes.Length < 8)
                {
                    Debug.WriteLine("encrypt error");
                    //LoggerFactory.getTraceLogger().debug(f48a, "cipherData is null ");
                    return null;
                }
                var stringBuffer = new StringBuilder(bytes.Length * 2);
                for (int i2 = 0; i2 < bytes.Length; i2++)
                {
                    stringBuffer.Append(String.Format("%02x", bytes[i2]));
                }
                return stringBuffer.ToString();
            }
            catch (Exception e)
            {

                return null;
            }
        }

        private static byte[] generateKey()
        {
            if (lastSeed == null)
            {
                lastSeed = "";
            }
            int length = lastSeed.Length;
            int length2 = f49b.Length;
            if (length >= length2)
            {
                length = length2;
            }
            var obj = new byte[length2];
            Array.Copy(f49b, 0, obj, 0, length2);
            for (length2 = 0; length2 < length; length2++)
            {
                obj[length2] = (byte)(f49b[length2] ^ lastSeed[length2]);
            }

            return obj;
        }

        private static byte[] decryptCore(String encryptedSeed)
        {
            int i = 0;
            if (encryptedSeed == null)
            {
                return null;
            }
            int length = encryptedSeed.Length / 2;
            if (length < 8)
            {
                return null;
            }
            String toLowerCase = encryptedSeed.ToLower();
            byte[] bArr = new byte[length];
            for (int i2 = 0; i2 < length; i2++)
            {
                int indexOfChar = getIndexOfChar(toLowerCase[i2 * 2], "0123456789abcdef");
                int indexOfChar2 = getIndexOfChar(toLowerCase[(i2 * 2) + 1], "0123456789abcdef");
                if (indexOfChar == -1 || indexOfChar2 == -1)
                {
                    return null;
                }
                bArr[i2] = (byte)(((indexOfChar << 4) | indexOfChar2) & 255);
            }
            try
            {
                //Key secretKeySpec = new SecretKeySpec(generateKey(), "DESede");
                //Cipher instance = Cipher.getInstance("DESede/ECB/NOPADDING");
                //instance.init(2, secretKeySpec);
                //byte[] doFinal = instance.doFinal(bArr);

                var input = CryptographicBuffer.CreateFromByteArray(bArr);
                var instance = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.TripleDesEcb);
                var secretKeySpec = instance.CreateSymmetricKey(CryptographicBuffer.CreateFromByteArray(generateKey()));
                var output = CryptographicEngine.Decrypt(secretKeySpec, input, null);
                byte[] doFinal;
                CryptographicBuffer.CopyToByteArray(output, out doFinal);

                if (doFinal == null || doFinal.Length < 8)
                {
                    return null;
                }
                String str2 = Encoding.UTF8.GetString(doFinal);
                int length2 = str2.Length / 2;
                if (length2 < 8)
                {
                    Debug.WriteLine("decrypt error");
                    return null;
                }
                doFinal = new byte[length2];
                while (i < length2)
                {
                    int indexOfChar3 = getIndexOfChar(str2[i * 2], "0123456789abcdef");
                    int indexOfChar = getIndexOfChar(str2[(i * 2) + 1], "0123456789abcdef");
                    if (indexOfChar3 == -1 || indexOfChar == -1)
                    {
                        return null;
                    }
                    doFinal[i] = (byte)(((indexOfChar3 << 4) | indexOfChar) & 255);
                    i++;
                }
                return doFinal;
            }
            catch (Exception e)
            {

                return null;
            }
        }

        public static byte[] decryptOtpSeed(String seed, String last)
        {
            try
            {
                lastSeed = last;
                return decryptCore(seed);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static String encryptOtpSeed(String seed, String last)
        {
            try
            {
                lastSeed = last;
                return encryptCore(seed);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static int getIndexOfChar(char c, String str)
        {
            return str.IndexOf(c);
        }
    }
}