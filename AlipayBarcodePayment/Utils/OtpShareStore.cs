using System;
using Windows.UI.Xaml;
using System.Collections.Generic;

namespace Jeffreye.Alipay.BarcodePayment.Utils
{
    /// <summary>
    /// port of com.alipay.mobile.security.otp.service.OtpShareStore
    /// </summary>
    public class OtpShareStore
    {
        public static readonly string SETTING_INFOS = "SETTING_INFOS";
        public static readonly string SETTING_INFOS_NEW = "SETTING_INFOS_NEW";

        static Dictionary<string, string> settingInfos = new Dictionary<string, string>();
        static Dictionary<string, string> newSettingInfos = new Dictionary<string, string>();

        public static string getString(Application applicationContex, string key, string settingTag)
        {
            string result;
            if (settingTag == SETTING_INFOS)
            {
                settingInfos.TryGetValue(key, out result);
            }
            else
            {
                newSettingInfos.TryGetValue(key, out result);
            }
            return result;
        }

        public static void putString(Application applicationContex, string key, string value, string settingTag)
        {
            if (settingTag == SETTING_INFOS)
            {
                settingInfos[key] = value;
            }
            else
            {
                newSettingInfos[key] = value;
            }
        }
    }
}