namespace LiveDetect.Service.Model
{
    public class LiveDetectConfig
    {
        public int id { get; set; }
        public string merchantId { get; set; }
        public string merchantName { get; set; }

        /// <summary>
        /// 隐私条款
        /// </summary>
        public string terms { get; set; }

        /// <summary>
        /// 回调地址
        /// </summary>
        public string callback { get; set; }

        public DateTime updatedAt { get; set; }
    }
}
