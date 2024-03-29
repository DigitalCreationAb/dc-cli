using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DC.Cli
{
    public class GoLanguage : ILanguage
    {
        private const string LanguageName = "go";

        public string Name => LanguageName;

        public static ILanguage Instance { get; } = new GoLanguage();
        
        private static ILanguageVersion Go1 { get; } = new GoVersion("1", "go1.x");
        
        public IEnumerable<ILanguageVersion> GetVersions()
        {
            yield return Go1;
        }

        public ILanguageVersion GetDefaultVersion()
        {
            return Go1;
        }
        
        private class GoVersion : ILanguageVersion
        {
            private readonly string _runtimeName;
            private readonly Docker.Container _dockerContainer;
            
            public GoVersion(string version, string runtimeName)
            {
                Version = version;
                _runtimeName = runtimeName;

                _dockerContainer = Docker
                    .TemporaryContainerFromImage("amazonlinux:2", project: "centos")
                    .EntryPoint("go")
                    .EnvironmentVariable("GOPATH", "/usr/local/src/.go");
            }

            public string Language => LanguageName;
            public string Version { get; }
            
            public Task<bool> Restore(string path)
            {
                var goPath = new DirectoryInfo(Path.Combine(path, ".go"));
                
                if (!goPath.Exists)
                    goPath.Create();
                
                return _dockerContainer
                    .WithVolume(path, "/usr/local/src", true)
                    .Run("get -v -t -d ./...");
            }

            public Task<bool> Clean(string path)
            {
                return _dockerContainer
                    .WithVolume(path, "/usr/local/src", true)
                    .EntryPoint("rm")
                    .Run("-r -f .go .out");
            }

            public async Task<bool> Build(string path)
            {
                var restoreResult = await Restore(path);

                if (!restoreResult)
                    return false;

                if (File.Exists(Path.Combine(path, "build.sh")))
                {
                    return await _dockerContainer
                        .WithVolume(path, "/usr/local/src", true)
                        .EntryPoint("./build.sh")
                        .Run("");   
                }

                return await _dockerContainer
                    .WithVolume(path, "/usr/local/src", true)
                    .Run("build -o ./.out/main -v .");
            }

            public async Task<bool> Test(string path)
            {
                var restoreResult = await Restore(path);

                if (!restoreResult)
                    return false;
                
                return await _dockerContainer
                    .WithVolume(path, "/usr/local/src", true)
                    .Run("test -run ''");
            }

            public Task<bool> StartWatch(string path)
            {
                //TODO: Implement
                return Task.FromResult(true);
            }

            public Task<bool> StopWatch(string path)
            {
                //TODO: Implement
                return Task.FromResult(true);
            }

            public string GetHandlerName()
            {
                return "main";
            }

            public string GetFunctionOutputPath(string functionPath)
            {
                return Path.Combine(functionPath, ".out");
            }

            public string GetRuntimeName()
            {
                return _runtimeName;
            }

            public override string ToString()
            {
                return $"{Language}:{Version}";
            }
        }
    }
}