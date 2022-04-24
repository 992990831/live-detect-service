using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LiveDetect.Service.Service;
using LiveDetect.Service.Model;
using System.Security.Cryptography;
using LiveDetect.Service.Common;
using System.IO;
using System.Net;
using System.Text;

namespace LiveDetect.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [CustomExceptionFilter]
    public class LiveDetectController : ControllerBase
    {
        private const string folder = "live-video";

        // 百度云中开通对应服务应用的 API Key 建议开通应用的时候多选服务
        private static String clientId = "R28VSZLeBFw1w8b3yrz67Ait";
        // 百度云中开通对应服务应用的 Secret Key
        private static String clientSecret = "Drzmlb7qNQfGoePWv2bptetZ2yzaRUGn";

        private IRepository repo;

        public LiveDetectController(IRepository repository)
        {
            repo = repository;
        }

        [HttpGet]
        [Route("hello")]
        public async Task<string> Hello()
        {
            var result = await Task.Factory.StartNew(()=> "Hello");

            return result;
        }

        [HttpGet]
        [Route("accounts")]
        public List<Account> Accounts()
        {
            return  repo.GetAccounts();
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="clientID"></param>
       /// <param name="account"></param>
       /// <returns></returns>
        [HttpGet]
        [Route("verify/{clientID}/{encryptedAccount}")]
        public VerifyResponse Verify(string clientID, string encryptedAccount)
        {
            var userKey = repo.GetUserKeys(clientID);
            if(userKey == null)
            {
                return new VerifyResponse { Success = false, Message = "Client doesn't exists" };
            }

            var key = userKey.Key;

            var account = Util.Decrypt(encryptedAccount, key);

            if(string.IsNullOrEmpty(account))
            {
                return new VerifyResponse { Success = false, Message = "Invalid encrypted account" };
            }

            //隐私条款
            string terms = repo.GetConfigTerm(clientID);

            return new VerifyResponse { Success = true, Account = account, Terms= terms };
        }

        [HttpGet]
        [Route("encrypt/{clientID}/{account}")]
        public string Encrypt(string clientID, string account)
        {
            var userKey = repo.GetUserKeys(clientID);
            if (userKey == null)
            {
                return String.Empty;
            }

            var key = userKey.Key;

            var encryptedAccount = Util.EncryptString(account, key);

            return encryptedAccount;
        }

        [HttpGet]
        [Route("users")]
        public List<User> GetUsersList()
        {
            return repo.GetUserList();
        }

        [HttpGet]
        [Route("records")]
        public dynamic GetRecords(int pageIndex, int pageSize, string? clientId, string? result, string? start, string? end)
        {
            string filter = " where 1=1 ";
            if(!string.IsNullOrEmpty(clientId))
            {
                filter += ("and ur.merchantid='" + clientId + "'");
            }

            if (!string.IsNullOrEmpty(result))
            {
                filter += ("and ld.result='" + result + "'");
            }

            if (!string.IsNullOrEmpty(start))
            {
                filter += ("and ld.createdat>='" + start + "'");
            }

            if (!string.IsNullOrEmpty(end))
            {
                filter += ("and ld.createdat<='" + end + "'");
            }

            return new { list = repo.GetLiveDetectsList(pageIndex, pageSize, filter), count = repo.GetLiveDetectCount(filter) };
        }

        /// <summary>
        /// 如果客户端用content-type: application/json的话，这里可以在入参中使用对象(FromBody)
        /// 如果contenr-type: form/url什么的， 入参使用FromForm
        /// 但遇到了preflight的问题，由于现在的方式，服务是host在另一个.net framework IIS服务上，所以遇到了options preflight无法处理的情况
        /// 只能把content-type改为plain/text，这样就不能在入参中使用对象。 只能读取body再做json转换
        /// </summary>
        [HttpPost]
        [Route("record")]
        //public void RecordLiveDetect([FromBody] RecordLiveVideoData recordData)
        //public async void RecordLiveDetect([FromForm] RecordLiveVideoData recordData)
        public void RecordLiveDetect()
        {
            var bodyAsText = new System.IO.StreamReader(this.HttpContext.Request.Body).ReadToEndAsync().Result;
            RecordLiveVideoData recordData = System.Text.Json.JsonSerializer.Deserialize<RecordLiveVideoData>(bodyAsText);
            Console.WriteLine(recordData.account);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string fileName = DateTime.Now.ToString("yyyy-MM-dd-mm-ss") + ".txt";

            System.IO.File.WriteAllText(folder + "/" + fileName, System.Text.Json.JsonSerializer.Serialize(recordData));

            repo.AddLiveDetect(new LiveDetectModel() { Account = recordData.account, ClientId = recordData.clientId, Result = recordData.result, FilePath = fileName, TransId = recordData.transId });

            Task.Factory.StartNew(() =>
            {
                var userKey = repo.GetUserKeys(recordData.clientId);
                if (userKey == null)
                    return;

                string callbackUrl = repo.GetConfigCallbackUrl(recordData.clientId);
                Console.WriteLine("clientId:" + recordData.clientId + "callback url:" + callbackUrl);
                CallBack(callbackUrl, userKey.Key, recordData.bestImg, recordData.transId, recordData.score, recordData.result);
            });
        }

        [HttpGet]
        [Route("best-img")]
        public string GetBestImg(string filePath)
        {
            string fullPath = folder + "/" + filePath;
            if (!System.IO.File.Exists(fullPath))
            {
                return string.Empty;
            }

            string json = System.IO.File.ReadAllText(fullPath);
            var liveVideoData = System.Text.Json.JsonSerializer.Deserialize<RecordLiveVideoData>(json);

            return liveVideoData.bestImg;
        }

        [HttpGet]
        [Route("configs")]
        public dynamic GetConfigList(int pageIndex, int pageSize, string? clientId)
        {
            string filter = " where 1=1 ";
            if (!string.IsNullOrEmpty(clientId))
            {
                filter += ("and merchantid='" + clientId + "'");
            }

            return new { list = repo.GetLiveDetecConfig(pageIndex, pageSize, filter), count = repo.GetConfigCount(filter) };

        }

        [HttpPost]
        [Route("config")]
        //public void RecordLiveDetect([FromBody] RecordLiveVideoData recordData)
        //public async void RecordLiveDetect([FromForm] RecordLiveVideoData recordData)
        public void UpdateTerms()
        {
            var bodyAsText = new System.IO.StreamReader(this.HttpContext.Request.Body).ReadToEndAsync().Result;
            var config = System.Text.Json.JsonSerializer.Deserialize<LiveDetectConfig>(bodyAsText);
            Console.WriteLine(config.merchantId);
            repo.SaveConfigTerms(config.merchantId, config.terms);
        }

        [HttpGet]
        [Route("token")]
        public string GetToken()
        {
            String authHost = "https://aip.baidubce.com/oauth/2.0/token";
            HttpClient client = new HttpClient();
            List<KeyValuePair<String, String>> paraList = new List<KeyValuePair<string, string>>();
            paraList.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
            paraList.Add(new KeyValuePair<string, string>("client_id", clientId));
            paraList.Add(new KeyValuePair<string, string>("client_secret", clientSecret));

            HttpResponseMessage response = client.PostAsync(authHost, new FormUrlEncodedContent(paraList)).Result;
            String result = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(result);
            return result;
        }


        #region 验证码
        /// <summary>
        /// 如果客户端用content-type: application/json的话，这里可以在入参中使用对象(FromBody)
        /// 如果contenr-type: form/url什么的， 入参使用FromForm
        /// 但遇到了preflight的问题，由于现在的方式，服务是host在另一个.net framework IIS服务上，所以遇到了options preflight无法处理的情况
        /// 只能把content-type改为plain/text，这样就不能在入参中使用对象。 只能读取body再做json转换
        /// </summary>
        [HttpPost]
        [Route("code")]
        //public void RecordLiveDetect([FromBody] RecordLiveVideoData recordData)
        //public async void RecordLiveDetect([FromForm] RecordLiveVideoData recordData)
        public void RecordLiveDetectCode()
        {
            var bodyAsText = new System.IO.StreamReader(this.HttpContext.Request.Body).ReadToEndAsync().Result;
            RecordLiveCodeData recordData = System.Text.Json.JsonSerializer.Deserialize<RecordLiveCodeData>(bodyAsText);
            
            repo.AddLiveDetectCode(new LiveDetectCode() { Account = recordData.account, ClientId = recordData.clientId, TransId = recordData.transId });
        }

        [HttpGet]
        [Route("codes")]
        public dynamic GetCodesHistory(int pageIndex, int pageSize, string? clientId, string? start, string? end)
        {
            string filter = " where 1=1 ";
            if (!string.IsNullOrEmpty(clientId))
            {
                filter += ("and ur.merchantid='" + clientId + "'");
            }
           
            if (!string.IsNullOrEmpty(start))
            {
                filter += ("and ld.createdat>='" + start + "'");
            }

            if (!string.IsNullOrEmpty(end))
            {
                filter += ("and ld.createdat<='" + end + "'");
            }

            return new { list = repo.GetLiveDetectCodeList(pageIndex, pageSize, filter), count = repo.GetCodesCount(filter) };
        }

        #endregion

        #region 回调地址配置
        
        [HttpPost]
        [Route("config/callback")]
        //public void RecordLiveDetect([FromBody] RecordLiveVideoData recordData)
        //public async void RecordLiveDetect([FromForm] RecordLiveVideoData recordData)
        public void UpdateCallback()
        {
            var bodyAsText = new System.IO.StreamReader(this.HttpContext.Request.Body).ReadToEndAsync().Result;
            var config = System.Text.Json.JsonSerializer.Deserialize<LiveDetectConfig>(bodyAsText);
            repo.UpdateConfigCallback(config.merchantId, config.callback);
        }

        #endregion

        #region 
        /// <summary>
        /// 给客户发回调的接口
        /// </summary>
        [HttpPost]
        [Route("client/callback")]
        public void ClientCallback()
        {
            var bodyAsText = new System.IO.StreamReader(this.HttpContext.Request.Body).ReadToEndAsync().Result;
            CallbackRequest recordData = System.Text.Json.JsonSerializer.Deserialize<CallbackRequest>(bodyAsText);

            var userKey = repo.GetUserKeys(recordData.ClientId);
            if (userKey == null)
                return;

            string key = userKey.Key;
            string bestImg = recordData.BestImg;
            string transId = recordData.TransId;
            double score = recordData.Score;
            bool result = recordData.Result;

            string callbackUrl = repo.GetConfigCallbackUrl(recordData.ClientId);
            CallBack(callbackUrl, key, bestImg, transId, score, result);
        }

        private void CallBack(string url, string key, string bestImg, string transId, double score, bool result)
        {
            //string key = repo.GetUserKeys(recordData.ClientId).Key;
            //string bestImg = recordData.BestImg;
            //string transId = recordData.TransId;
            //double score = recordData.Score;
            //bool result = recordData.Result;

            Console.WriteLine(string.Format("callback: transId:{0}, score: {1}", transId, score));
            Console.WriteLine(string.Format("callback: url: {0}", url));

            var requestObj = new
            {
                transId = transId,
                bestImage = bestImg,
                score = score,
                result = result
            };

            var requestStr = System.Text.Json.JsonSerializer.Serialize(requestObj);
            byte[] binaryData = Encoding.UTF8.GetBytes(Util.EncryptString(requestStr, key));

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            try
            {
                request.ContentType = "text/plain";
                request.Method = "POST";

                request.ContentLength = binaryData.Length;
                request.Timeout = int.MaxValue;
                Stream reqstream = request.GetRequestStream();
                reqstream.Write(binaryData, 0, binaryData.Length);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream streamReceive = response.GetResponseStream();
                Encoding encoding = Encoding.UTF8;

                StreamReader streamReader = new StreamReader(streamReceive, encoding);
                string strResult = streamReader.ReadToEnd();
                //strResult = Util.Decrypt(strResult, secretKey);
                Console.WriteLine(string.Format("callback: merchantId:{0}, result: {1}", clientId, strResult));
                return;
            }
            catch (WebException webEx)
            {
                Stream ReceiveStream = webEx.Response.GetResponseStream();
                Encoding encode = Encoding.GetEncoding("utf-8");

                StreamReader readStream = new StreamReader(ReceiveStream, encode);
                string str = readStream.ReadToEnd();
                Console.WriteLine("Failed at:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                Console.Write("异常信息:" + ex.Message);
            }
            finally
            {
                request.Abort();
            }
        }

        #endregion
    }

    public struct CallbackRequest
    {
        public string ClientId { get; set; }

        public string TransId { get; set; }

        public string BestImg { get; set; }

        public double Score { get; set; }
        
        public bool Result { get; set; }
    }

    public struct VerifyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public string Account { get; set; }

        public string Terms { get; set; }
    }

    public struct RecordLiveVideoData
    {
        public string clientId { get; set; }

        public string account { get; set; }

        public string videoBase64 { get; set; }

        public string bestImg { get; set; }

        public float score { get; set; }

        public string transId { get; set; }

        public Boolean result { get; set; }
    }

    public struct RecordLiveCodeData
    {
        public string clientId { get; set; }

        public string account { get; set; }

        public string transId { get; set; }
    }
}
