using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using DC.AWS.Projects.Cli.Components;

namespace DC.AWS.Projects.Cli
{
    public class NodeLanguage : ILanguage
    {
        public const string LanguageName = "node";

        private NodeLanguage()
        {
            
        }

        public static ILanguage Instance { get; } = new NodeLanguage();
        private static ILanguageVersion Node10 { get; } = new NodeVersion("10", "nodejs10.x", "10");
        private static ILanguageVersion Node12 { get; } = new NodeVersion("12", "nodejs12.x", "12");

        public string Name { get; } = LanguageName;

        public IEnumerable<ILanguageVersion> GetVersions()
        {
            yield return Node10;
            yield return Node12;
        }

        public ILanguageVersion GetDefaultVersion()
        {
            return Node12;
        }
        
        private class NodeVersion : ILanguageVersion
        {
            private readonly string _runtimeName;
            private readonly Docker.Container _dockerContainer;
            
            public NodeVersion(string version, string runtimeName, string dockerImageTag)
            {
                Version = version;
                _runtimeName = runtimeName;

                _dockerContainer = Docker
                    .CreateContainer($"node:{dockerImageTag}")
                    .EntryPoint("yarn");
            }

            public string Language { get; } = LanguageName;
            public string Version { get; }
            
            public async Task<BuildResult> Build(string path)
            {
                if (!File.Exists(Path.Combine(path, "package.json")))
                    return new BuildResult(true, "");

                var result = await _dockerContainer
                    .WithVolume(path, "/usr/src/app", true)
                    .Run("");

                return new BuildResult(result.exitCode == 0, result.output);
            }

            public async Task<TestResult> Test(string path)
            {
                if (!File.Exists(Path.Combine(path, "package.json")))
                    return new TestResult(true, "");

                var packageData =
                    Json.DeSerialize<PackageJsonData>(await File.ReadAllTextAsync(Path.Combine(path, "package.json")));

                if (!(packageData.Scripts ?? new Dictionary<string, string>().ToImmutableDictionary())
                    .ContainsKey("test"))
                {
                    return new TestResult(true, "");
                }
                
                var result = await _dockerContainer
                    .WithVolume(path, "/usr/src/app", true)
                    .Run("run test");

                return new TestResult(result.exitCode == 0, result.output);
            }

            public string GetHandlerName()
            {
                return "handler.handler";
            }

            public string GetFunctionOutputPath(string functionPath)
            {
                return functionPath;
            }

            public string GetRuntimeName()
            {
                return _runtimeName;
            }

            public override string ToString()
            {
                return $"{Language}:{Version}";
            }
            
            private class PackageJsonData
            {
                public IImmutableDictionary<string, string> Scripts { get; set; }
            }
        }
    }
}