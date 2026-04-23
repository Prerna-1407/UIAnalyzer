namespace UIAnalyzerAPI.Models
{
    public class PageInfo
    {
        public string Path {get;set;}
        public string PageType { get; set; }
        public List<ControlInfo> Controls { get; set; }
    }
}
