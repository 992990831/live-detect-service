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
    }
}
