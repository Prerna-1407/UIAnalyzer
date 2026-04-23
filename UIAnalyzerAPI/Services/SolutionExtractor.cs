using System.IO.Compression;

namespace UIAnalyzerAPI.Services
{
    public class SolutionExtractor
    {
        public void ExtractZip(string zipFilePath, string extractToDirectory)
        {
            // Overwrite existing files if any
            ZipFile.ExtractToDirectory(zipFilePath, extractToDirectory, true);
        }
    }
}
