using Dapper;
using MySql.Data.MySqlClient;
using LiveDetect.Service.Model;

namespace LiveDetect.Service.Service
{
    public interface IRepository
    {
        List<Account> GetAccounts();

        //根据client id找到对应的user key关系
        UserKeyRelation? GetUserKeys(string clientID);

        void AddLiveDetect(LiveDetectModel model);

        List<LiveDetectModel> GetLiveDetectsList(int pageIndex, int pageSize, string filter);

        List<User> GetUserList();

        int GetLiveDetectCount(string filter);

        List<LiveDetectConfig> GetLiveDetecConfig(int pageIndex, int pageSize, string filter);

        int GetConfigCount(string filter);

        void SaveConfigTerms(string merchantId, string terms);

        string GetConfigTerm(string merchantId);

        string GetConfigCallbackUrl(string merchantId);

        void AddLiveDetectCode(LiveDetectCode model);

        List<LiveDetectCode> GetLiveDetectCodeList(int pageIndex, int pageSize, string filter);

        int GetCodesCount(string filter);

        void UpdateConfigCallback(string merchantId, string callback);

        void AddConsumptionHistory(ConsumptionHistory ch);
    }

    public class MySQLRepository: IRepository
    {
        private readonly MySqlConnection conn = new MySqlConnection();

        public MySQLRepository(IConfiguration config)
        {
            Console.WriteLine(config["Setting:MySQL"]);
            conn.ConnectionString = config["Setting:MySQL"];
        }

        public List<Account> GetAccounts()
        {
           List<Account> accounts =  conn.Query<Account>("SELECT * FROM minsh.account limit 100 ").ToList();

            return accounts;
        }

        public UserKeyRelation? GetUserKeys(string clientID)
        {
            var userKeyList = conn.Query<UserKeyRelation>("select * from minsh.userkeyrelation where merchantID=@clientID limit 1", new { clientID });

            if(userKeyList == null)
            {
                return null;
            }

            return userKeyList.FirstOrDefault();
        }

        public void AddLiveDetect(LiveDetectModel model)
        {
            conn.Execute("insert into minsh.livedetect(ClientId, Account, FilePath, TransId, Result, CreatedAt) values(@ClientId, @Account, @FilePath, @TransId, @Result, @CreatedAt)"
                , new { ClientId= model.ClientId, Account=model.Account, FilePath= model.FilePath, TransId=model.TransId, Result= model.Result, CreatedAt=DateTime.Now });

        }

        public List<User> GetUserList()
        {
            var userKeyList = conn.Query<User>("SELECT merchantId, `Name` FROM minsh.user as u left join minsh.userkeyrelation as uk on u.identifier=uk.userID where merchantId is not null;").ToList();

            return userKeyList;
        }

        public List<LiveDetectModel> GetLiveDetectsList(int pageIndex, int pageSize, string filter)
        {
            var liveDetectList = conn.Query<LiveDetectModel>(String.Format("select clientid, u.`name` as clientName, account, filepath, transId, result, createdat "
            + " from minsh.livedetect as ld left join minsh.`userkeyrelation` as ur on ld.clientid = ur.merchantid "
            + " left join minsh.`user` as u on ur.userid = u.identifier" 
            + filter
            +" order by ld.createdat desc limit {0}, {1}", (pageIndex-1) * pageSize, pageSize )).ToList();

            return liveDetectList;
        }

        public int GetLiveDetectCount(string filter)
        {
            string sql = "select count(1) "
            + " from minsh.livedetect as ld left join minsh.`userkeyrelation` as ur on ld.clientid = ur.merchantid "
            + " left join minsh.`user` as u on ur.userid = u.identifier "
            + filter;
            var count = conn.QueryFirst<int>(sql);

            return count;
        }

        public List<LiveDetectConfig> GetLiveDetecConfig(int pageIndex, int pageSize, string filter)
        {
            var configs = conn.Query<LiveDetectConfig>(string.Format("select id, merchantid, merchantname, terms, callback, updatedat "
                + " from minsh.`livedetect-config` "
                + filter
                + " order by createdat desc limit {0}, {1}", (pageIndex - 1) * pageSize, pageSize)).ToList();

            return configs;
        }

        public string GetConfigTerm(string merchantId)
        {
            var config = conn.Query<LiveDetectConfig>("select id, merchantid, merchantname, terms, updatedat from minsh.`livedetect-config` where merchantid=@merchantId limit 1", new { merchantId }).FirstOrDefault();

            if (config == null)
            {
                return string.Empty;
            }

            return config.terms;
        }

        public void SaveConfigTerms(string merchantId, string terms)
        {
            string sql = "update minsh.`livedetect-config` set terms = '" + terms + "' where merchantId='" + merchantId + "' ";
            conn.Execute(sql);
        }

        public int GetConfigCount(string filter)
        {
            string sql = "select count(1) "
                + " from minsh.`livedetect-config` "
                + filter;

            var count = conn.QueryFirst<int>(sql);

            return count;
        }

        #region 验证码
        public void AddLiveDetectCode(LiveDetectCode model)
        {
            conn.Execute("insert into minsh.`livedetect-code`(ClientId, Account, TransId, CreatedAt) values(@ClientId, @Account, @TransId, @CreatedAt)"
                , new { ClientId = model.ClientId, Account = model.Account, TransId = model.TransId, CreatedAt = DateTime.Now });
        }

        public List<LiveDetectCode> GetLiveDetectCodeList(int pageIndex, int pageSize, string filter)
        {
            var liveDetectList = conn.Query<LiveDetectCode>(String.Format("select clientid, u.`name` as clientName, account, transId, createdat "
            + " from minsh.`livedetect-code` as ld left join minsh.`userkeyrelation` as ur on ld.clientid = ur.merchantid "
            + " left join minsh.`user` as u on ur.userid = u.identifier"
            + filter
            + " order by ld.createdat desc limit {0}, {1}", (pageIndex - 1) * pageSize, pageSize)).ToList();

            return liveDetectList;
        }

        public int GetCodesCount(string filter)
        {
            string sql = "select count(1) "
            + " from minsh.`livedetect-code` as ld left join minsh.`userkeyrelation` as ur on ld.clientid = ur.merchantid "
            + " left join minsh.`user` as u on ur.userid = u.identifier "
            + filter;
            var count = conn.QueryFirst<int>(sql);

            return count;
        }

        #endregion

        #region 回调地址
        public void UpdateConfigCallback(string merchantId, string callback)
        {
            string sql = "update minsh.`livedetect-config` set callback = '" + callback + "' where merchantId='" + merchantId + "' ";
            conn.Execute(sql);
        }

        public string GetConfigCallbackUrl(string merchantId)
        {
            var config = conn.Query<LiveDetectConfig>("select id, merchantid, merchantname, terms, callback, updatedat from minsh.`livedetect-config` where merchantid=@merchantId limit 1", new { merchantId }).FirstOrDefault();

            if (config == null)
            {
                return string.Empty;
            }

            return config.callback;
        }

        public void AddConsumptionHistory(ConsumptionHistory ch)
        {
            conn.Execute("insert into minsh.consumptionhistory(Account, ServiceID, DatasourceID, DatasourceName, Amount, Count, SuccessFlag, `Description`, `Date`, IsSecond, SecondSearchTime, LocalFilePath, KeyInfo1, KeyInfo2, KeyInfo3, Discount, RequestIP, RequestUrl)" + 
            " values(@account, @service_id, @datasource_id, @datasource_name, @requestedAmount, @count, @v_successFlag, @v_description, now(), @v_second, @v_secondtime, @v_filePath,@v_keyinfo1,@v_keyinfo2,@v_keyinfo3,@v_discount, @v_ip, @v_url)"
                , new { account = ch.Account, service_id = ch.ServiceType, datasource_id = ch.DatasourceId, 
                datasource_name = ch.DatasourceName, requestedAmount = ch.Amount, count=1, v_successFlag =ch.ResponseCode,
                v_description = ch.Description, v_second = ch.IsSecond? 1:0, v_secondtime = ch.SecondTime, v_filePath = ch.FilePath, 
                v_keyinfo1=ch.KeyInfo1, v_keyinfo2=ch.KeyInfo2, v_keyinfo3=ch.KeyInfo3, v_discount= ch.Discount, v_ip=ch.IP, v_url=ch.RequestUrl});
        }
        #endregion
    }
}
