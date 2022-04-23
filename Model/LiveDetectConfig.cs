namespace LiveDetect.Service.Model
{
    public class LiveDetectConfig
    {
        public int id { get; set; }
        public string merchantId { get; set; }
        public string merchantName { get; set; }

        public string terms { get; set; }

        public DateTime updatedAt { get; set; }
    }
}
