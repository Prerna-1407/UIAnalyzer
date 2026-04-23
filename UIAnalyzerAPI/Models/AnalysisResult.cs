namespace UIAnalyzerAPI.Models
{
    public class AnalysisResult
    {
         public List<ProjectInfo> Projects { get; set; }
         public Dictionary<string, List<(string Id, string Version)>> NugetPackages { get; set; }
        public List<PageInfo> Pages { get; set; }
        public List<ExternalApiInfo> ExternalApis { get; set; }
    }
}
