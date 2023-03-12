namespace LiveDetect.Service.Model
{
    class IdCardModel
    {
        public IdCardModel_Result? words_result { get; set; }
    }

    class IdCardModel_Result
    {
        public IdCardModel_ResultType? 姓名 { get; set; }
        public IdCardModel_ResultType? 民族 { get; set; }
        public IdCardModel_ResultType? 住址 { get; set; }
        public IdCardModel_ResultType? 公民身份号码 { get; set; }
        public IdCardModel_ResultType? 出生 { get; set; }
        public IdCardModel_ResultType? 性别 { get; set; }
    }

    class IdCardModel_ResultType
    {
        public string? words { get; set; }
    }

    public class OCRResponseModel
    {
        public string? address { get; set; }

        public string? birthday { get; set; }

        public string? idNumber { get; set; }

        public string? name { get; set; }

        public string? people { get; set; }

        public string? sex { get; set; }

        public string? type { get; set; }

        public string? issueAuthority { get; set; }

        public string? validity { get; set; }
    }
}
