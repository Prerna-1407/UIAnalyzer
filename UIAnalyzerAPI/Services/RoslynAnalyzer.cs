using UIAnalyzerAPI.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace UIAnalyzerAPI.Services
{
    public class RoslynAnalyzer
    {
        public async Task<List<ExternalApiInfo>> FindExternalApiCallsAsync(string rootDir)
        {
            var results = new List<ExternalApiInfo>();
            var csFiles = Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories);

            var syntaxTrees = new List<SyntaxTree>();
            var sourceTexts = new List<string>();
            foreach (var f in csFiles)
            {
                try
                {
                    var text = await File.ReadAllTextAsync(f);
                    sourceTexts.Add(text);
                    syntaxTrees.Add(CSharpSyntaxTree.ParseText(text, path: f));
                }
                catch { }
            }

            var compilation = CSharpCompilation.Create("AnalysisCompilation",
                syntaxTrees,
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            foreach (var tree in syntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                // Find invocation expressions
                var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
                foreach (var inv in invocations)
                {
                    var expr = inv.Expression.ToString();

                    // Heuristics: method names
                    var methodName = inv.Expression is MemberAccessExpressionSyntax ma ? ma.Name.Identifier.Text : inv.Expression.ToString();

                    if (methodName.Contains("GetAsync") || methodName.Contains("PostAsync") || methodName.Contains("SendAsync") || methodName.Contains("Execute"))
                    {
                        // Try to find URL argument
                        string? urlPattern = null;
                        foreach (var arg in inv.ArgumentList.Arguments)
                        {
                            var constVal = semanticModel.GetConstantValue(arg.Expression);
                            if (constVal.HasValue && constVal.Value is string s && LooksLikeUrl(s))
                            {
                                urlPattern = s;
                                break;
                            }
                            // also check string literal syntax
                            if (arg.Expression is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
                            {
                                var s2 = lit.Token.ValueText;
                                if (LooksLikeUrl(s2)) { urlPattern = s2; break; }
                            }
                        }

                        // Determine containing file path
                        var filePath = tree.FilePath ?? "unknown";
                        results.Add(new ExternalApiInfo{ 
                            File=filePath, 
                            CallSite=inv.ToString(), 
                            UrlPattern=urlPattern
                        });
                    }
                }

                // Also look for object creations like new RestClient("https://api...")
                var creations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
                foreach (var oc in creations)
                {
                    var typeName = oc.Type.ToString();
                    if (typeName.Contains("RestClient") || typeName.Contains("HttpClient"))
                    {
                        string? urlPattern = null;
                        if (oc.ArgumentList != null)
                        {
                            foreach (var arg in oc.ArgumentList.Arguments)
                            {
                                var constVal = semanticModel.GetConstantValue(arg.Expression);
                                if (constVal.HasValue && constVal.Value is string s && LooksLikeUrl(s))
                                {
                                    urlPattern = s;
                                    break;
                                }
                            }
                        }
                        results.Add(new ExternalApiInfo{
                            File=tree.FilePath ?? "unknown",
                            CallSite=oc.ToString(),
                            UrlPattern=urlPattern});
                    }
                }
            }

            return results;
        }

        private bool LooksLikeUrl(string s)
        {
            return s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }
    }
}
