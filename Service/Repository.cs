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

        int GetUserCount(string filter);

        List<LiveDetectConfig> GetLiveDetecConfig(int pageIndex, int pageSize, string filter);

        int GetConfigCount(string filter);

        void SaveConfigTerms(string merchantId, string terms);

        string GetConfigTerm(string merchantId);
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
            conn.Execute("insert into minsh.livedetect(ClientId, Account, FilePath, Result, CreatedAt) values(@ClientId, @Account, @FilePath, @Result, @CreatedAt)"
                , new { ClientId= model.ClientId, Account=model.Account, FilePath= model.FilePath, Result= model.Result, CreatedAt=DateTime.Now });

        }

        public List<User> GetUserList()
        {
            var userKeyList = conn.Query<User>("SELECT merchantId, `Name` FROM minsh.user as u left join minsh.userkeyrelation as uk on u.identifier=uk.userID where merchantId is not null;").ToList();

            return userKeyList;
        }

        public List<LiveDetectModel> GetLiveDetectsList(int pageIndex, int pageSize, string filter)
        {
            var liveDetectList = conn.Query<LiveDetectModel>(String.Format("select clientid, u.`name` as clientName, account, filepath, result, createdat "
            + " from minsh.livedetect as ld left join minsh.`userkeyrelation` as ur on ld.clientid = ur.merchantid "
            + " left join minsh.`user` as u on ur.userid = u.identifier" 
            + filter
            +" order by ld.createdat desc limit {0}, {1}", (pageIndex-1) * pageSize, pageSize )).ToList();

            return liveDetectList;
        }

        public int GetUserCount(string filter)
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
            var configs = conn.Query<LiveDetectConfig>(string.Format("select id, merchantid, merchantname, terms, updatedat "
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
    }
}
