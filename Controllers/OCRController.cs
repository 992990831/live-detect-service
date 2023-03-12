using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LiveDetect.Service.Service;
using LiveDetect.Service.Model;
using System.Security.Cryptography;
using LiveDetect.Service.Common;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;
using System.Runtime.Serialization.Json;

namespace LiveDetect.Service.Controllers
{
    [Route("ocr/v2")]
    [ApiController]
    [CustomExceptionFilter]
    public class OCRController : ControllerBase
    {
        // https://console.bce.baidu.com/ai/#/ai/ocr/app/list
        // 百度云中开通对应服务应用的 API Key 建议开通应用的时候多选服务
        private static String clientId = "uTgbBKGIrFeAlNvhCarIG3Co";
        // 百度云中开通对应服务应用的 Secret Key
        private static String clientSecret = "dYQTYlHGSYwyrxdcz7RD5VC9bntooGl6";

        private IRepository repo;

        public OCRController(IRepository repository)
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

        [HttpPost]
        [Route("idRecognize")]
        public async Task<object> IdCardOCR()
        {
            var merchantId = string.Empty;
            var account = string.Empty;
            UserKeyRelation? userKey = null;//string.Empty;

            if(!this.Request.Headers.ContainsKey("merchantId") || 
            !this.Request.Headers.ContainsKey("account") ||
            !this.Request.Headers.ContainsKey("timeStamp"))
            {
                return BadRequest("缺少必要参数");
            }

            try
            {
                merchantId = this.Request.Headers["merchantId"];
                account = this.Request.Headers["account"];
                userKey = repo.GetUserKeys(merchantId);
                account = Util.Decrypt(account, userKey.Key);
            }
            catch
            {
                return BadRequest("参数错误");
            }

            //身份证图片的base64编码
            string imgBase64 = string.Empty;
            var img = Bitmap.FromStream(Request.Body);
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                ms.Seek(0, SeekOrigin.Begin);
                byte[] arr = ms.ToArray();

                imgBase64 = Convert.ToBase64String(arr);
            }

            string fileLocalPath = DateTime.Now.ToString("yyyyMMdd_hh_mm_ss_fff");
            img.Save("d:/ocr_images/"+fileLocalPath+".jpg");

            IdCardModel model = null;
            
            try{
               model = idInfoRecognize(imgBase64);
            }
             catch{

             }

            /*记录调用*/
            var ch = new ConsumptionHistory();
            ch.Account = account;
            ch.ServiceType = 13;
            ch.Amount=0;
            ch.Description= model == null? "识别失败" : "识别成功";
            ch.ResponseCode = model == null? 12 : 11;
            ch.DatasourceId="-1";
            ch.DatasourceName="敏识";
            ch.IsSecond = false;
            ch.SecondTime = "1000";
            ch.FilePath = "";
            ch.Discount=1;
            ch.IP = "";

            try
            {
                ch.KeyInfo1 = model?.words_result?.公民身份号码?.words;
                ch.KeyInfo2 = model?.words_result?.姓名?.words;
                ch.KeyInfo3 = model?.words_result?.出生?.words.Substring(0, 4) + "年" + model?.words_result?.出生?.words.Substring(4, 2) + "月" + model?.words_result?.出生?.words.Substring(6, 2) + "日";
                ch.IP = this.Request.Host.Value;
                ch.RequestUrl = "ocr/v2/idRecognize";
            }
            catch
            {

            }

            repo.AddConsumptionHistory(ch);

            OCRResponseModel responseModel = new OCRResponseModel();
            responseModel.address = model?.words_result?.住址?.words;
            try{
                responseModel.birthday = model?.words_result?.出生?.words.Substring(0,4) + "年" + model?.words_result?.出生?.words.Substring(4,2) + "月" + model?.words_result?.出生?.words.Substring(6,2) + "日";
            }
            catch{

            }
            
            responseModel.idNumber = model?.words_result?.公民身份号码?.words;
            //responseModel.issueAuthority = //model?.words_result?.住址?.words;
            responseModel.name = model?.words_result?.姓名?.words;
            //responseModel.people = //model?.words_result?.住址?.words;
            responseModel.sex = model?.words_result?.性别?.words;
            responseModel.type = "第二代身份证";
            //responseModel.validity = "";

            

            return Util.EncryptString(GetJSON<OCRResponseModel>(responseModel), userKey.Key);
        }

        private IdCardModel idInfoRecognize(string imgBase64)
        {
            string token = GetToken();
            var obj = JsonSerializer.Deserialize<Token>(token);
            string? accessToken = obj?.AccessToken;
            Console.WriteLine("token:" + accessToken);
            
            string host = "https://aip.baidubce.com/rest/2.0/ocr/v1/idcard?access_token=" + accessToken;
            Encoding encoding = Encoding.Default;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(host);
            request.Method = "post";
            request.KeepAlive = true;

            String str = "id_card_side=" + "front" + "&image=" + HttpUtility.UrlEncode(imgBase64);
            byte[] buffer = encoding.GetBytes(str);
            request.ContentLength = buffer.Length;
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
            string result = reader.ReadToEnd();          
            Console.WriteLine(result);
            var model = System.Text.Json.JsonSerializer.Deserialize<IdCardModel>(result);
            return model;
        }

        private string GetJSON<T>(T jsonObj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject((Stream)ms, jsonObj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
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
    
        public byte[] ToByte(Stream stream)
        {
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                bytes = ms.ToArray();
            }
            return bytes;
        }

    }
      
    class Token {
        [JsonPropertyName("refresh_token")]
        public string RefreshToken {get;set;}

        [JsonPropertyName("access_token")]
        public string AccessToken {get;set;}
    }
}
