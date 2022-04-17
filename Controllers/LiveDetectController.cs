using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LiveDetect.Service.Service;
using LiveDetect.Service.Model;
using System.Security.Cryptography;
using LiveDetect.Service.Common;

namespace LiveDetect.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LiveDetectController : ControllerBase
    {
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
    }

    public struct VerifyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public string Account { get; set; }
    }
}
