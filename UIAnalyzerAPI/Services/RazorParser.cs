using HtmlAgilityPack;
using System.Xml;
using UIAnalyzerAPI.Models;

namespace UIAnalyzerAPI.Services
{
    public class RazorParser
    {
        public List<PageInfo> ParsePages(string rootDir)
        {
            var pages = new List<PageInfo>();

            // Razor/CSHTML
            var razorFiles = Directory.GetFiles(rootDir, "*.cshtml", SearchOption.AllDirectories)
                             .Concat(Directory.GetFiles(rootDir, "*.razor", SearchOption.AllDirectories));

            foreach (var file in razorFiles)
            {
                var controls = ParseHtmlControls(File.ReadAllText(file));
                pages.Add(new PageInfo { 
                    Path=file, 
                    PageType=Path.GetExtension(file).TrimStart('.'), 
                    Controls=controls });
            }

            // ASPX (basic HTML parse)
            var aspxFiles = Directory.GetFiles(rootDir, "*.aspx", SearchOption.AllDirectories);
            foreach (var file in aspxFiles)
            {
                var controls = ParseHtmlControls(File.ReadAllText(file));
                pages.Add(new PageInfo { 
                    Path=file, 
                    PageType="aspx", 
                    Controls = controls });
            }

            // XAML (WPF)
            var xamlFiles = Directory.GetFiles(rootDir, "*.xaml", SearchOption.AllDirectories);
            foreach (var file in xamlFiles)
            {
                var controls = ParseXamlControls(File.ReadAllText(file));
                pages.Add(new PageInfo{
                    Path=file, 
                    PageType = "xaml", 
                    Controls = controls 
                });
            }

            // WinForms Designer.cs (look for control instantiation)
            var designerFiles = Directory.GetFiles(rootDir, "*.Designer.cs", SearchOption.AllDirectories);
            foreach (var file in designerFiles)
            {
                var controls = ParseWinFormsDesigner(File.ReadAllText(file));
                pages.Add(new PageInfo{
                    Path=file, 
                    PageType = "winforms", 
                    Controls=controls});
            }

            return pages;
        }

        private List<ControlInfo> ParseHtmlControls(string content)
        {
            var list = new List<ControlInfo>();

            // Use HtmlAgilityPack for tolerant parsing
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            // inputs
            foreach (var node in doc.DocumentNode.SelectNodes("//input|//select|//textarea") ?? Enumerable.Empty<HtmlNode>())
            {
                var type = node.Name;
                var name = node.GetAttributeValue("name", null) ?? node.GetAttributeValue("id", null);
                var aspFor = node.GetAttributeValue("asp-for", null);
                var bind = node.GetAttributeValue("bind", null) ?? node.GetAttributeValue("@bind", null);
                var additional = $"type={node.GetAttributeValue("type", "")}";
                list.Add(new ControlInfo{
                    Type=type, 
                    Name=name, 
                    Binding=aspFor ?? bind, 
                    Additional=additional 
                });
            }

            // labels
            foreach (var node in doc.DocumentNode.SelectNodes("//label") ?? Enumerable.Empty<HtmlNode>())
            {
                var text1 = node.InnerText?.Trim();
                var @for = node.GetAttributeValue("for", null);
                list.Add(new ControlInfo{
                    Type="label", 
                    Name=@for, 
                    Binding=text1, 
                    Additional=null
                });
            }

            // Razor helpers like @Html.TextBoxFor(...) - simple regex
            var text = content;
            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"Html\.(TextBoxFor|EditorFor|LabelFor)\s*\(\s*model\s*=>\s*model\.([A-Za-z0-9_\.]+)\s*\)");
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var helper = m.Groups[1].Value;
                var binding = m.Groups[2].Value;
                list.Add(new ControlInfo
                { 
                    Type = helper, 
                    Name = null, 
                    Binding = binding, 
                    Additional = "razor-helper" 
                });
            }

            return list;
        }

        private List<ControlInfo> ParseXamlControls(string content)
        {
            var list = new List<ControlInfo>();
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(content);
                var ns = doc.Root?.Name.Namespace;
                var elements = doc.Descendants().Where(e => e.Name.LocalName == "TextBox" || e.Name.LocalName == "Label" || e.Name.LocalName == "ComboBox" || e.Name.LocalName == "Button" || e.Name.LocalName == "ComboBox");
                foreach (var el in elements)
                {
                    var name = el.Attribute("x:Name")?.Value ?? el.Attribute("Name")?.Value;
                    var binding = el.Attributes().FirstOrDefault(a => a.Value.Contains("{Binding"))?.Value;
                    list.Add(new ControlInfo { 
                        Type=el.Name.LocalName, 
                        Name = name, 
                        Binding=binding, 
                        Additional=null });
                }
            }
            catch { }
            return list;
        }

        private List<ControlInfo> ParseWinFormsDesigner(string content)
        {
            var list = new List<ControlInfo>();
            // Look for "this.textBox1 = new System.Windows.Forms.TextBox();"
            var regex = new System.Text.RegularExpressions.Regex(@"this\.(?<name>[A-Za-z0-9_]+)\s*=\s*new\s*(?<type>[\w\.]+)\s*\(");
            foreach (System.Text.RegularExpressions.Match m in regex.Matches(content))
            {
                var name = m.Groups["name"].Value;
                var type = m.Groups["type"].Value.Split('.').Last();
                list.Add(new ControlInfo{ 
                    Type = type, 
                    Name = name, 
                    Binding = null, 
                    Additional = "winforms-designer" });
            }
            return list;
        }
    }
}
