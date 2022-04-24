namespace LiveDetect.Service.Model
{
    /// <summary>
    /// 验证码使用记录
    /// </summary>
    public class LiveDetectCode
    {
        public int Id { get; set; }

        /// <summary>
        /// 商户号
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// 商户名
        /// </summary>
        public string ClientName { get; set; }

        public string Account { get; set; }


        public string TransId { get; set; }


        public DateTime CreatedAt { get; set; }
    }
}
