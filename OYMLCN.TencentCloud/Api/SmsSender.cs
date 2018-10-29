using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace OYMLCN.TencentCloud
{
    /// <summary>
    /// SmsSender 基于 qcloudsms 2017.08.08
    /// </summary>
    public class SmsSender
    {
        private int SDKAppID;
        private string AppKey;
        SmsSenderUtil util = new SmsSenderUtil();

        /// <summary>
        /// SmsSender
        /// </summary>
        /// <param name="sdkAppId">SDK AppID是短信应用的唯一标识，调用短信API接口时需要提供该参数。</param>
        /// <param name="appKey">App Key是用来校验短信发送请求合法性的密码，与SDK AppID对应，需要业务方高度保密，切勿把密码存储在客户端。</param>
        public SmsSender(int sdkAppId, string appKey)
        {
            this.SDKAppID = sdkAppId;
            this.AppKey = appKey;
        }

        const string smsurl = "https://yun.tim.qq.com/v5/tlssmssvr/sendsms";
        const string multismsurl = "https://yun.tim.qq.com/v5/tlssmssvr/sendmultisms2";

        /// <summary>
        /// 普通单发短信接口，明确指定内容，如果有多个签名，请在内容中以【】的方式添加到信息内容中，否则系统将使用默认签名
        /// </summary>
        /// <param name="type">短信类型，0 为普通短信，1 营销短信</param>
        /// <param name="nationCode">国家码，如 86 为中国</param>
        /// <param name="phoneNumber">不带国家码的手机号</param>
        /// <param name="msg">信息内容，必须与申请的模板格式一致，否则将返回错误</param>
        /// <param name="extend">扩展码，可填空</param>
        /// <param name="ext">服务端原样返回的参数，可填空</param>
        /// <returns></returns>
        public SmsSenderResult Send(int type, string nationCode, string phoneNumber, string msg, string extend, string ext)
        {
            if (0 != type && 1 != type)
                throw new Exception("type " + type + " error");
            if (null == extend)
                extend = "";
            if (null == ext)
                ext = "";

            long random = util.GetRandom();
            long curTime = util.GetCurTime();

            // 按照协议组织 post 请求包体
            JObject data = new JObject();

            JObject tel = new JObject();
            tel.Add("nationcode", nationCode);
            tel.Add("mobile", phoneNumber);

            data.Add("tel", tel);
            data.Add("msg", msg);
            data.Add("type", type);
            data.Add("sig", util.StrToHash(String.Format("appkey={0}&random={1}&time={2}&mobile={3}", AppKey, random, curTime, phoneNumber)));
            data.Add("time", curTime);
            data.Add("extend", extend);
            data.Add("ext", ext);

            string wholeUrl = smsurl + "?sdkappid=" + SDKAppID + "&random=" + random;
            HttpWebRequest request = util.GetPostHttpConn(wholeUrl);
            byte[] requestData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            request.ContentLength = requestData.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(requestData, 0, requestData.Length);
            requestStream.Close();

            // 接收返回包
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
            string responseStr = streamReader.ReadToEnd();
            streamReader.Close();
            responseStream.Close();
            SmsSenderResult result;
            if (HttpStatusCode.OK == response.StatusCode)
                result = util.ResponseStrToSenderResult(responseStr);
            else
            {
                result = new SmsSenderResult();
                result.result = -1;
                result.errmsg = "http error " + response.StatusCode + " " + responseStr;
            }
            return result;
        }
        /// <summary>
        /// 发送国内普通短信
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public SmsSenderResult SendNormalSMS(string phoneNumber, string msg) => Send(0, "86", phoneNumber, msg, "", "");

        /// <summary>
        /// 指定模板单发
        /// </summary>
        /// <param name="nationCode">国家码，如 86 为中国</param>
        /// <param name="phoneNumber">不带国家码的手机号</param>
        /// <param name="templId">模板 id</param>
        /// <param name="templParams">模板参数列表，如模板 {1}...{2}...{3}，那么需要带三个参数</param>
        /// <param name="sign">短信签名，如果使用默认签名，该字段可缺省</param>
        /// <param name="extend">扩展码，可填空</param>
        /// <param name="ext">服务端原样返回的参数，可填空</param>
        /// <returns></returns>
        public SmsSenderResult SendWithParam(string nationCode, string phoneNumber, int templId, List<string> templParams, string sign, string extend, string ext)
        {
            if (null == sign)
                sign = "";
            if (null == extend)
                extend = "";
            if (null == ext)
                ext = "";

            long random = util.GetRandom();
            long curTime = util.GetCurTime();

            // 按照协议组织 post 请求包体
            JObject data = new JObject();

            JObject tel = new JObject();
            tel.Add("nationcode", nationCode);
            tel.Add("mobile", phoneNumber);

            data.Add("tel", tel);
            data.Add("sig", util.CalculateSigForTempl(AppKey, random, curTime, phoneNumber));
            data.Add("tpl_id", templId);
            data.Add("params", util.SmsParamsToJSONArray(templParams));
            data.Add("sign", sign);
            data.Add("time", curTime);
            data.Add("extend", extend);
            data.Add("ext", ext);

            string wholeUrl = smsurl + "?sdkappid=" + SDKAppID + "&random=" + random;
            HttpWebRequest request = util.GetPostHttpConn(wholeUrl);
            byte[] requestData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            request.ContentLength = requestData.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(requestData, 0, requestData.Length);
            requestStream.Close();

            // 接收返回包
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
            string responseStr = streamReader.ReadToEnd();
            streamReader.Close();
            responseStream.Close();
            SmsSenderResult result;
            if (HttpStatusCode.OK == response.StatusCode)
                result = util.ResponseStrToSenderResult(responseStr);
            else
            {
                result = new SmsSenderResult();
                result.result = -1;
                result.errmsg = "http error " + response.StatusCode + " " + responseStr;
            }
            return result;
        }
        /// <summary>
        /// 单发国内模板短信
        /// </summary>
        /// <param name="templId">模板 id</param>
        /// <param name="phoneNumber">不带国家码的手机号</param>
        /// <param name="tempParams">模板参数列表，如模板 {1}...{2}...{3}，那么需要带三个参数</param>
        /// <returns></returns>
        public SmsSenderResult SendSMSWithParam(int templId, string phoneNumber, params string[] tempParams) =>
            SendSMSWithParam(templId, phoneNumber, tempParams.ToList());
        /// <summary>
        /// 单发国内模板短信
        /// </summary>
        /// <param name="templId">模板 id</param>
        /// <param name="phoneNumber">不带国家码的手机号</param>
        /// <param name="tempParams">模板参数列表，如模板 {1}...{2}...{3}，那么需要带三个参数</param>
        /// <returns></returns>
        public SmsSenderResult SendSMSWithParam(int templId, string phoneNumber, List<string> tempParams) =>
            SendSMSWithSignAndParam(templId, phoneNumber, "", tempParams);
        /// <summary>
        /// 单发自定义签名的国内模板短信
        /// </summary>
        /// <param name="templId">模板 id</param>
        /// <param name="phoneNumber">不带国家码的手机号</param>
        /// <param name="sign">短信签名，如果使用默认签名，该字段可缺省</param>
        /// <param name="tempParams">模板参数列表，如模板 {1}...{2}...{3}，那么需要带三个参数</param>
        /// <returns></returns>
        public SmsSenderResult SendSMSWithSignAndParam(int templId, string phoneNumber, string sign, List<string> tempParams) =>
            SendWithParam("86", phoneNumber, templId, tempParams, sign, "", "");

        /// <summary>
        /// 普通群发短信接口，明确指定内容，如果有多个签名，请在内容中以【】的方式添加到信息内容中，否则系统将使用默认签名
        /// 【注意】海外短信无群发功能
        /// </summary>
        /// <param name="type">短信类型，0 为普通短信，1 营销短信</param>
        /// <param name="nationCode">国家码，如 86 为中国</param>
        /// <param name="phoneNumbers">不带国家码的手机号列表</param>
        /// <param name="msg">信息内容，必须与申请的模板格式一致，否则将返回错误</param>
        /// <param name="extend">扩展码，可填空</param>
        /// <param name="ext">服务端原样返回的参数，可填空</param>
        /// <returns></returns>
        public SmsSenderResult Send(int type, string nationCode, List<string> phoneNumbers, string msg, string extend, string ext)
        {
            if (0 != type && 1 != type)
                throw new Exception("type " + type + " error");
            if (null == extend)
                extend = "";
            if (null == ext)
                ext = "";

            long random = util.GetRandom();
            long curTime = util.GetCurTime();

            // 按照协议组织 post 请求包体
            JObject data = new JObject();
            data.Add("tel", util.PhoneNumbersToJSONArray(nationCode, phoneNumbers));
            data.Add("type", type);
            data.Add("msg", msg);
            data.Add("sig", util.CalculateSig(AppKey, random, curTime, phoneNumbers));
            data.Add("time", curTime);
            data.Add("extend", extend);
            data.Add("ext", ext);

            string wholeUrl = multismsurl + "?sdkappid=" + SDKAppID + "&random=" + random;
            HttpWebRequest request = util.GetPostHttpConn(wholeUrl);
            byte[] requestData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            request.ContentLength = requestData.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(requestData, 0, requestData.Length);
            requestStream.Close();

            // 接收返回包
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
            string responseStr = streamReader.ReadToEnd();
            streamReader.Close();
            responseStream.Close();
            SmsSenderResult result;
            if (HttpStatusCode.OK == response.StatusCode)
                result = util.ResponseStrToSenderResult(responseStr);
            else
            {
                result = new SmsSenderResult();
                result.result = -1;
                result.errmsg = "http error " + response.StatusCode + " " + responseStr;
            }
            return result;
        }
        /// <summary>
        /// 指定模板群发
        /// 【注意】海外短信无群发功能
        /// </summary>
        /// <param name="nationCode">国家码，如 86 为中国</param>
        /// <param name="phoneNumbers">不带国家码的手机号列表</param>
        /// <param name="templId">模板 id</param>
        /// <param name="templParams">模板参数列表</param>
        /// <param name="sign">签名，如果填空，系统会使用默认签名</param>
        /// <param name="extend">扩展码，可以填空</param>
        /// <param name="ext">服务端原样返回的参数，可以填空</param>
        /// <returns></returns>
        public SmsSenderResult SendWithParam(String nationCode, List<string> phoneNumbers, int templId, List<string> templParams, string sign, string extend, string ext)
        {
            if (null == sign)
                sign = "";
            if (null == extend)
                extend = "";
            if (null == ext)
                ext = "";

            long random = util.GetRandom();
            long curTime = util.GetCurTime();

            // 按照协议组织 post 请求包体
            JObject data = new JObject();
            data.Add("tel", util.PhoneNumbersToJSONArray(nationCode, phoneNumbers));
            data.Add("sig", util.CalculateSigForTempl(AppKey, random, curTime, phoneNumbers));
            data.Add("tpl_id", templId);
            data.Add("params", util.SmsParamsToJSONArray(templParams));
            data.Add("sign", sign);
            data.Add("time", curTime);
            data.Add("extend", extend);
            data.Add("ext", ext);

            string wholeUrl = multismsurl + "?sdkappid=" + SDKAppID + "&random=" + random;
            HttpWebRequest request = util.GetPostHttpConn(wholeUrl);
            byte[] requestData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            request.ContentLength = requestData.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(requestData, 0, requestData.Length);
            requestStream.Close();

            // 接收返回包
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
            string responseStr = streamReader.ReadToEnd();
            streamReader.Close();
            responseStream.Close();
            SmsSenderResult result;
            if (HttpStatusCode.OK == response.StatusCode)
                result = util.ResponseStrToSenderResult(responseStr);
            else
            {
                result = new SmsSenderResult();
                result.result = -1;
                result.errmsg = "http error " + response.StatusCode + " " + responseStr;
            }
            return result;
        }

        /// <summary>
        /// 短信发送响应结果
        /// </summary>
        public class SmsSenderResult
        {
            /// <summary>
            /// 错误码，0 表示成功(计费依据)，非 0 表示失败
            /// 参考 https://cloud.tencent.com/document/product/382/3771
            /// </summary>
            public int result { set; get; }
            /// <summary>
            /// 错误消息，result 非 0 时的具体错误信息
            /// </summary>
            public string errmsg { set; get; }
            /// <summary>
            /// 用户的 session 内容，腾讯 server 回包中会原样返回
            /// </summary>
            public string ext { set; get; }
            /// <summary>
            /// [单发]本次发送标识 id，标识一次短信下发记录
            /// </summary>
            public string sid { set; get; }
            /// <summary>
            /// [单发]短信计费的条数
            /// </summary>
            public int fee { set; get; }
            /// <summary>
            /// [群发]手机号码
            /// </summary>
            public string mobile { get; set; }
            /// <summary>
            /// [群发]国家码
            /// </summary>
            public string nationcode { get; set; }
            /// <summary>
            /// [群发]结果详细
            /// </summary>
            public SmsSenderResult[] detail { get; set; }
        }

        class SmsSenderUtil
        {
            Random random = new Random();

            public HttpWebRequest GetPostHttpConn(string url)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                return request;
            }

            public long GetRandom() => random.Next(999999) % 900000 + 100000;

            public long GetCurTime() => (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            // 将二进制的数值转换为 16 进制字符串，如 "abc" => "616263"
            private static string ByteArrayToHex(byte[] byteArray)
            {
                string returnStr = "";
                if (byteArray != null)
                    for (int i = 0; i < byteArray.Length; i++)
                        returnStr += byteArray[i].ToString("x2");
                return returnStr;
            }

            public string StrToHash(string str) =>
                ByteArrayToHex(SHA256Managed.Create().ComputeHash(Encoding.UTF8.GetBytes(str)));

            public SmsSenderResult ResponseStrToSenderResult(string str) =>
                JsonConvert.DeserializeObject<SmsSenderResult>(str);

            public JArray SmsParamsToJSONArray(List<string> templParams)
            {
                JArray smsParams = new JArray();
                foreach (string templParamsElement in templParams)
                    smsParams.Add(templParamsElement);
                return smsParams;
            }

            public JArray PhoneNumbersToJSONArray(string nationCode, List<string> phoneNumbers)
            {
                JArray tel = new JArray();
                int i = 0;
                do
                {
                    JObject telElement = new JObject();
                    telElement.Add("nationcode", nationCode);
                    telElement.Add("mobile", phoneNumbers.ElementAt(i));
                    tel.Add(telElement);
                }
                while (++i < phoneNumbers.Count);

                return tel;
            }

            public string CalculateSigForTempl(string appkey, long random, long curTime, List<string> phoneNumbers)
            {
                string phoneNumbersString = phoneNumbers.ElementAt(0);
                for (int i = 1; i < phoneNumbers.Count; i++)
                    phoneNumbersString += "," + phoneNumbers.ElementAt(i);
                return StrToHash(String.Format("appkey={0}&random={1}&time={2}&mobile={3}", appkey, random, curTime, phoneNumbersString));
            }

            public string CalculateSigForTempl(string appkey, long random, long curTime, string phoneNumber)
            {
                List<string> phoneNumbers = new List<string>();
                phoneNumbers.Add(phoneNumber);
                return CalculateSigForTempl(appkey, random, curTime, phoneNumbers);
            }

            public string CalculateSig(string appkey, long random, long curTime, List<string> phoneNumbers)
            {
                string phoneNumbersString = phoneNumbers.ElementAt(0);
                for (int i = 1; i < phoneNumbers.Count; i++)
                    phoneNumbersString += "," + phoneNumbers.ElementAt(i);
                return StrToHash(String.Format("appkey={0}&random={1}&time={2}&mobile={3}", appkey, random, curTime, phoneNumbersString));
            }
        }
    }
}
