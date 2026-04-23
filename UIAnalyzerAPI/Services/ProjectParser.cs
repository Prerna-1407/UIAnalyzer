
using System.Xml.Linq;
using UIAnalyzerAPI.Models;

namespace UIAnalyzerAPI.Services
{
    public class ProjectParser
    {
        public List<ProjectInfo> FindProjects(string rootDir)
        {
            var list = new List<ProjectInfo>();
            var csprojFiles = Directory.GetFiles(rootDir, "*.csproj", SearchOption.AllDirectories);
            foreach (var f in csprojFiles)
            {
                var name = Path.GetFileNameWithoutExtension(f);
                list.Add(new ProjectInfo
                {
                    Name = name,
                    Path = f
                });
            }
            return list;
        }

        public List<(string Id, string Version)> ParseNuGetPackages(string csprojPath)
        {
            var result = new List<(string, string)>();
            try
            {
                var doc = XDocument.Load(csprojPath);
                XNamespace ns = doc.Root?.GetDefaultNamespace() ?? "";
                var packageRefs = doc.Descendants(ns + "PackageReference");

                foreach (var pr in packageRefs)
                {
                    var id = pr.Attribute("Include")?.Value
                             ?? pr.Element(ns + "Include")?.Value
                             ?? pr.Element(ns + "Package")?.Value
                             ?? string.Empty;

                    var version = pr.Attribute("Version")?.Value
                                  ?? pr.Element(ns + "Version")?.Value
                                  ?? string.Empty;

                    if (!string.IsNullOrEmpty(id))
                        result.Add((id, version));
                }
                //var packageRefs = doc.Descendants("PackageReference")
                //                     .Select(x => (
                //                         Id: x.Attribute("Include")?.Value ?? x.Element("Include")?.Value ?? "",
                //                         Version: x.Attribute("Version")?.Value ?? x.Element("Version")?.Value ?? ""
                //                     ));
                //foreach (var p in packageRefs)
                //{
                //    if (!string.IsNullOrEmpty(p.Id))
                //        result.Add((p.Id, p.Version));
                //}
            }
            catch
            {
                // ignore parse errors
            }
            return result;
        }
    }
}
