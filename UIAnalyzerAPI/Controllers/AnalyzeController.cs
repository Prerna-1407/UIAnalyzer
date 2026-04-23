using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UIAnalyzerAPI.Models;
using UIAnalyzerAPI.Services;

namespace UIAnalyzerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyzeController : ControllerBase
    {
        private string path = "C:\\Prerna\\Logs\\";
        private readonly SolutionExtractor _extractor;
        private readonly ProjectParser _projectParser;
        private readonly RoslynAnalyzer _roslyn;
        private readonly RazorParser _razor;

        public AnalyzeController(SolutionExtractor extractor, ProjectParser projectParser, RoslynAnalyzer roslyn, RazorParser razor)
        {
            _extractor = extractor;
            _projectParser = projectParser;
            _roslyn = roslyn;
            _razor = razor;
        }

        [HttpPost]
        [Route("analyze")]
        public async Task<IActionResult> Analyze(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Zip file required.");

            //var tempDir = Path.Combine(Path.GetTempPath(), "ui-analyzer", Guid.NewGuid().ToString("N"));
            var tempDir = path;
            Directory.CreateDirectory(tempDir);

            try
            {
                var zipPath = Path.Combine(tempDir, file.FileName);
                await using (var fs = System.IO.File.Create(zipPath))
                {
                    await file.CopyToAsync(fs);
                }

                _extractor.ExtractZip(zipPath, tempDir);

                var projects = _projectParser.FindProjects(tempDir);
                var nugets = new Dictionary<string, List<(string Id, string Version)>>();

                foreach (var p in projects)
                {
                    var pkgs = _projectParser.ParseNuGetPackages(p.Path);
                    nugets[p.Name] = pkgs;
                }

                // Parse UI pages
                var pages = _razor.ParsePages(tempDir);

                // Roslyn analysis for external API calls
                var externalApis = await _roslyn.FindExternalApiCallsAsync(tempDir);

                var result = new AnalysisResult{
                    Projects= projects,
                    NugetPackages= nugets,
                    Pages= pages,
                    ExternalApis= externalApis};
                return Ok(result);
            }
            finally
            {
                // optional: cleanup after some time; for now keep for debugging
                // Directory.Delete(tempDir, true);
            }
        }
    }
}
