using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LiveDetect.Service.Service;
using LiveDetect.Service.Model;
using System.Security.Cryptography;
using LiveDetect.Service.Common;
using System.IO;

namespace LiveDetect.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LiveDetectController : ControllerBase
    {
        private const string folder = "live-video";

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

            return new VerifyResponse { Success = true, Account = account };
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

            return new { list = repo.GetLiveDetectsList(pageIndex, pageSize, filter), count = repo.GetUserCount(filter) };
        }

        [HttpPost]
        [Route("record")]
        public void RecordLiveDetect(RecordLiveVideoData recordData)
        {
            
            if(!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string fileName = DateTime.Now.ToString("yyyy-MM-dd-mm-ss") + ".txt";

            System.IO.File.WriteAllText(folder + "/" + fileName, System.Text.Json.JsonSerializer.Serialize(recordData));

            repo.AddLiveDetect(new LiveDetectModel() { Account=recordData.account, ClientId = recordData.clientId, Result = recordData.result, FilePath=fileName  });
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
    }

    public struct VerifyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public string Account { get; set; }
    }

    public struct RecordLiveVideoData
    {
        public string clientId { get; set; }

        public string account { get; set; }

        public string videoBase64 { get; set; }

        public string bestImg { get; set; }

        public float score { get; set; }

        public Boolean result { get; set; }
    }
}
