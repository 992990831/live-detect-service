namespace LiveDetect.Service.Model
{
    public class LiveDetectModel
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

        public string FilePath { get; set; }

        public Boolean Result { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
