using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jeffreye.Alipay.BarcodePayment.Utils
{
    /// <summary>
    /// port of com.alipay.mobile.security.otp.service.OtpManagerImpl
    /// </summary>
    public class OtpManager
    {
        string tid;
        string userIndex;
        string userId;
        Windows.UI.Xaml.Application applicationContex;

        public OtpManager(string tid,string index,string userId)
        {
            this.tid = tid;
            this.userIndex = index;
            this.userId = userId;
            applicationContex = Windows.UI.Xaml.Application.Current;
        }
        
        public String getDynamicOtp(String payChannelIndex, long? systemCurrentTimeMillis = null)
        {
            String dynamicOtp;

            //获取OTP Num
            String otpNum = getOtpNum(payChannelIndex, systemCurrentTimeMillis);
            if (string.IsNullOrEmpty(otpNum))
            {
                Debug.WriteLine("otpNum is null");
                return null;
            }
            else {
                String index = getIndex(getUserId());
                if (string.IsNullOrEmpty(index))
                {
                    Debug.WriteLine("user index is null");
                    //失败
                    return null;
                }
                else {

                    index = String.Format("{0:D10}", index);
                    dynamicOtp = encrypteForBarcode("28" + index + String.Format("{0:D6}", otpNum));
                }
            }

            Debug.WriteLine("Dynamic Nuber:{0}", dynamicOtp, "");
            return dynamicOtp;
        }

        protected String encrypteForBarcode(String str)
        {
            int i = 0;
            var obj = new int[str.Length];
            for (int i2 = 0; i2 < str.Length; i2++)
            {
                obj[i2] = int.Parse(str.Substring(i2, 1));
            }
            var obj2 = new int[6];
            var obj3 = new int[10];
            int[] iArr = new int[10];
            Array.Copy(obj, 2, obj3, 0, 10);
            Array.Copy(obj, 12, obj2, 0, 6);
            while (i <= 9)
            {
                iArr[i] = ((obj3[i] * 107) + obj2[i % 6]) % 10;
                i++;
            }
            return "28" + intArrayToString(iArr) + intArrayToString(obj2);
        }

        private string intArrayToString(int[] iArr)
        {
            var sb = new StringBuilder();
            foreach (var item in iArr)
            {
                sb.Append(item);
            }
            return sb.ToString();
        }

        public String getOtpNum(String extraInfo,long? systemCurrentTimeMillis = null)
        {
            log("getOtpNum extraInfo" + extraInfo);
            String tid4Location = getTid4Location();
            if (string.IsNullOrEmpty(tid4Location))
            {
                log("getOtpNum tid is null");
                //logGetOtp(null, null, str);
                return null;
            }
            string encryptedSeed;
            String settingInfo;
            byte[] OtpNumseed;
            //String key = OtpShareStore.SETTING_INFOS_NEW;
            String lastSeed = OtpShareStore.getString(this.applicationContex, tid4Location, OtpShareStore.SETTING_INFOS_NEW);
            log("获取本地加密的种子1 encryptedSeed =" + lastSeed);
            if (string.IsNullOrEmpty(lastSeed))
            {
                //key = OtpShareStore.SETTING_INFOS;
                encryptedSeed = OtpShareStore.getString(this.applicationContex, tid4Location, OtpShareStore.SETTING_INFOS);
                settingInfo = OtpShareStore.SETTING_INFOS;
            }
            else {
                encryptedSeed = lastSeed;
                settingInfo = OtpShareStore.SETTING_INFOS_NEW;
            }

            log("获取本地加密的种子2 encryptedSeed =" + encryptedSeed + " settingInfo=" + settingInfo);
            if (encryptedSeed == null || "" == encryptedSeed)
            {
                log("getOtpNum encryptedSeed is null");
                OtpNumseed = null;
            }
            else {
                lastSeed = tid4Location.GetJavaHashCode().ToString();
                if (settingInfo == (OtpShareStore.SETTING_INFOS))
                {
                    lastSeed = Guid.NewGuid().ToString();
                    //lastSeed = DeviceInfo.getInstance().getImei();
                }
                byte[] decryptOtpSeed = OtpSeedCryptor.decryptOtpSeed(encryptedSeed, lastSeed);
                if (decryptOtpSeed == null)
                {
                    log("机密种子seed is null");
                    deleteSeed();
                    OtpNumseed = decryptOtpSeed;
                }
                else {
                    OtpNumseed = decryptOtpSeed;
                }
            }
            if (OtpNumseed == null)
            {
                log("getOtpNum seed is null");
                //logGetOtp(null, null, str);
                return null;
            }
            byte[] bArr;
            if (String.IsNullOrEmpty(extraInfo))
            {
                bArr = OtpNumseed;
            }
            else
            {
                log("init seed: " + bytesToHexString(OtpNumseed));
                log("extraInfo : " + extraInfo);
                byte[] a = sha1(extraInfo);
                bArr = new byte[(a.Length + OtpNumseed.Length)];
                Array.Copy(OtpNumseed, 0, bArr, 0, OtpNumseed.Length);
                Array.Copy(a, 0, bArr, OtpNumseed.Length, a.Length);
                log("extraBytes : " + bytesToHexString(a));
                log("final seed :" + bytesToHexString(bArr));
            }

            try
            {

                var timeDiff = getServerTimeDiff();
                string intervalStr = OtpShareStore.getString(this.applicationContex, "interval", settingInfo);
                long interval = !string.IsNullOrEmpty(intervalStr) ? long.Parse(intervalStr) : 30;

                var currentTimeMillis = 
                    systemCurrentTimeMillis.HasValue?
                    systemCurrentTimeMillis.Value:
                    (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;

                long unixTimestamp = !string.IsNullOrEmpty(timeDiff) ? (currentTimeMillis / 1000) + long.Parse(timeDiff) : currentTimeMillis / 1000;

                if (settingInfo == (OtpShareStore.SETTING_INFOS))
                {
                    try
                    {
                        lastSeed = OtpShareStore.getString(this.applicationContex, getUserId() + "+" + tid4Location, settingInfo);

                        OtpShareStore.putString(this.applicationContex, "interval", interval.ToString(), OtpShareStore.SETTING_INFOS_NEW);
                        OtpShareStore.putString(this.applicationContex, getUserId() + "+" + tid4Location, lastSeed, OtpShareStore.SETTING_INFOS_NEW);
                        OtpShareStore.putString(this.applicationContex, "timedeff", timeDiff, OtpShareStore.SETTING_INFOS_NEW);

                        lastSeed = bytesToHexString(bArr);
                        log(" strOtpSeedHex=" + lastSeed);

                        lastSeed = OtpSeedCryptor.encryptOtpSeed(lastSeed, tid4Location.GetJavaHashCode().ToString());
                        log(" decodeSeed=" + lastSeed);

                        OtpShareStore.putString(this.applicationContex, tid4Location, lastSeed, OtpShareStore.SETTING_INFOS_NEW);

                    }
                    catch (Exception e)
                    {

                    }
                }

                return Hotp.Compute(bArr, unixTimestamp / interval, 6);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private string bytesToHexString(byte[] otpNumseed)
        {
            var sb = new StringBuilder();
            foreach (var item in otpNumseed)
            {
                var hexString = item.ToString("H");
                if (hexString.Length < 2)
                {
                    sb.Append(0);
                }
                sb.Append(hexString.ToUpper());
            }
            return sb.ToString();
        }

        private byte[] sha1(string str)
        {
            return OtpSeedCryptor.Sha1(str);
        }

        private void deleteSeed()
        {

        }

        private string getServerTimeDiff()
        {
            return null;
        }



        private string getTid4Location()
        {
            return tid;
        }

        private string getIndex(string p)
        {
            if (p == null)
            {
                return null;
            }
            return userIndex;
        }

        private string getUserId()
        {
            return userId;
        }

        private void log(string v)
        {
            Debug.WriteLine(v);
        }
    }
}
