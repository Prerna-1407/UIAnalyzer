namespace UIAnalyzerAPI.Models
{
    public class ExternalApiInfo
    {
        public string File {  get; set; }
        public string CallSite { get; set; }
        public string? UrlPattern { get; set; }
    }
}
