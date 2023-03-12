namespace LiveDetect.Service.Model
{
    public class ConsumptionHistory
    {
        public string Account;
        public int ServiceType;
        public Decimal Amount;
        public int ResponseCode;
        public string Description;
        public string DatasourceId = "0";
        public string DatasourceName = "";
        public bool IsSecond = false;
        public string SecondTime = "";
        public string FilePath = "";
        public string KeyInfo1 = "";
        public string KeyInfo2 = "";
        public string KeyInfo3 = "";
        public Decimal Discount = 1M;
        public string IP = "";
        public string RequestUrl;
    }
}
