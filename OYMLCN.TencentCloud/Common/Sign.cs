using System;
using System.Text;
using System.Security.Cryptography;
using OYMLCN.TencentCloud.Util;
using OYMLCN.Extensions;

namespace OYMLCN.TencentCloud.Common
{
    /// <summary>
    /// 签名类
    /// </summary>
    public class Sign
    {
        private static string Signature(int appId, string secretId, string secretKey, long expired, string fileId, string bucketName)
        {
            if (secretId == "" || secretKey == "")
            {
                return "-1";
            }
            var now = DateTime.Now.ToTimestamp();
            var rand = new Random();
            var rdm = rand.Next(Int32.MaxValue);
            var plainText = "a=" + appId + "&k=" + secretId + "&e=" + expired + "&t=" + now + "&r=" + rdm + "&f=" + fileId + "&b=" + bucketName;

            using (HMACSHA1 mac = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hash = mac.ComputeHash(Encoding.UTF8.GetBytes(plainText));
                var pText = Encoding.UTF8.GetBytes(plainText);
                var all = new byte[hash.Length + pText.Length];
                Array.Copy(hash, 0, all, 0, hash.Length);
                Array.Copy(pText, 0, all, hash.Length, pText.Length);
                return Convert.ToBase64String(all);
            }
        }

        /// <summary>
        /// 多次签名
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="secretId"></param>
        /// <param name="secretKey"></param>
        /// <param name="expired"></param>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public static string Signature(int appId, string secretId, string secretKey, long expired, string bucketName)=>
            Signature(appId, secretId, secretKey, expired, "", bucketName);

        /// <summary>
        /// 单次签名
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="secretId"></param>
        /// <param name="secretKey"></param>
        /// <param name="remotePath"></param>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public static string SignatureOnce(int appId, string secretId, string secretKey, string remotePath, string bucketName)=>
            Signature(appId, secretId, secretKey, 0, "/" + appId + "/" + bucketName + HttpUtils.EncodeRemotePath(remotePath), bucketName);

    }
}
