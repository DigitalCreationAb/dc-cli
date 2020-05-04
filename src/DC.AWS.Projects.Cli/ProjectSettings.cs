using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace DC.AWS.Projects.Cli
{
    public class ProjectSettings
    {
        private ProjectSettings()
        {
            
        }
        
        public string DefaultLanguage { private get; set; }
        public IDictionary<string, ApiConfiguration> Apis { get; set; } = new Dictionary<string, ApiConfiguration>();

        public IDictionary<string, ClientConfiguration> Clients { get; set; } =
            new Dictionary<string, ClientConfiguration>();

        [JsonIgnore]
        public string ProjectRoot { get; set; }

        public static ProjectSettings New(ILanguageRuntime defaultLanguage, string path)
        {
            return new ProjectSettings
            {
                DefaultLanguage = defaultLanguage?.ToString(),
                ProjectRoot = path
            };
        }

        public void AddApi(
            string name,
            string baseUrl,
            string relativePath,
            ILanguageRuntime defaultLanguage,
            int externalPort,
            int? port = null)
        {
            Apis[name] = new ApiConfiguration
            {
                BaseUrl = baseUrl,
                DefaultLanguage = defaultLanguage?.ToString(),
                Port = port ?? GetRandomUnusedPort(),
                ExternalPort = externalPort,
                RelativePath = relativePath
            };
        }

        public void AddClient(
            string name,
            string baseUrl,
            string relativePath,
            ClientType clientType,
            int externalPort,
            int? port = null)
        {
            Clients[name] = new ClientConfiguration
            {
                BaseUrl = baseUrl,
                ClientType = clientType,
                Port = port ?? GetRandomUnusedPort(),
                RelativePath = relativePath,
                Api = null,
                ExternalPort = externalPort
            };
        }
        
        public void AddClient(
            string name,
            string baseUrl,
            string relativePath,
            ClientType clientType,
            string api,
            int? port = null)
        {
            Clients[name] = new ClientConfiguration
            {
                BaseUrl = baseUrl,
                ClientType = clientType,
                Port = port ?? GetRandomUnusedPort(),
                RelativePath = relativePath,
                Api = api
            };
        }

        public ILanguageRuntime GetLanguage(string api = null)
        {
            return FunctionLanguage.Parse(Apis.ContainsKey(api ?? "")
                ? Apis[api ?? ""].DefaultLanguage ?? DefaultLanguage
                : DefaultLanguage);
        }

        public static ProjectSettings Read()
        {
            var currentPath = Environment.CurrentDirectory;
            
            while (true)
            {
                if (File.Exists(Path.Combine(currentPath, ".project.settings")))
                {
                    var settings = Json.DeSerialize<ProjectSettings>(
                        File.ReadAllText(Path.Combine(currentPath, ".project.settings")));

                    settings.ProjectRoot = currentPath;

                    return settings;
                }

                currentPath = Directory.GetParent(currentPath).FullName;
            }
        }

        public void Save()
        {
            File.WriteAllText(Path.Combine(ProjectRoot, ".project.settings"), Json.Serialize(this));
        }
        
        public (string path, string name) FindApiRoot(string path)
        {
            return FindRoot(path, "api");
        }
        
        public (string path, string name) FindFunctionRoot(string path)
        {
            return FindRoot(path, "function");
        }

        public string FindApiPath(string name)
        {
            var path = FindPath("api", name, ProjectRoot);
            
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException($"There is no api named: {name}");

            return path;
        }
        
        public string GetApiFunctionUrl(string api, string functionUrl)
        {
            if (!Apis.ContainsKey(api))
                throw new InvalidOperationException($"There is no api named: {api}");

            var apiBaseUrl = Apis[api].BaseUrl;

            if (apiBaseUrl.StartsWith("/"))
                apiBaseUrl = apiBaseUrl.Substring(1);
            
            if (apiBaseUrl.EndsWith("/"))
                apiBaseUrl = apiBaseUrl.Substring(0, apiBaseUrl.Length - 1);

            if (functionUrl.StartsWith("/"))
                functionUrl = functionUrl.Substring(1);

            return string.IsNullOrEmpty(apiBaseUrl) ? $"/{functionUrl}" : $"/{apiBaseUrl}/{functionUrl}";
        }

        public string GetRootedPath(string path)
        {
            path = path.Replace("[[PROJECT_ROOT]]", ProjectRoot);
            
            if (Path.IsPathRooted(path))
                return path;

            if (path.StartsWith("./"))
                path = path.Substring(2);

            return Path.Combine(Environment.CurrentDirectory, path);
        }
        
        private (string path, string name) FindRoot(string path, string type)
        {
            var currentPath = GetRootedPath(path);

            while (true)
            {
                if (!currentPath.StartsWith(ProjectRoot))
                    throw new InvalidOperationException("Can't search paths outside of project directory");
                
                if (File.Exists(Path.Combine(currentPath, $"{type}.infra.yml")))
                    return (currentPath, new DirectoryInfo(currentPath).Name);
                
                currentPath = Directory.GetParent(currentPath).FullName;
            }
        }

        private static string FindPath(string type, string name, string currentPath)
        {
            var directoriesToSkip = new List<string>
            {
                "node_modules",
                "infrastructure"
            };
            
            var dir = new DirectoryInfo(currentPath);

            if (dir.Name == name && File.Exists(Path.Combine(dir.FullName, $"{type}.infra.yml")))
                return dir.FullName;
            
            var dirs = dir.GetDirectories();

            return (from child in dirs
                    where !directoriesToSkip.Contains(child.Name)
                    select FindPath(type, name, child.FullName))
                .FirstOrDefault(foundPath => !string.IsNullOrEmpty(foundPath));
        }
        
        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
        
        public class ApiConfiguration
        {
            public string BaseUrl { get; set; }
            public string DefaultLanguage { get; set; }
            public int Port { get; set; }
            public int ExternalPort { get; set; }
            public string RelativePath { get; set; }
        }
        
        public class ClientConfiguration
        {
            public string BaseUrl { get; set; }
            public string RelativePath { get; set; }
            public ClientType ClientType { get; set; }
            public int Port { get; set; }
            public string Api { get; set; }
            public int? ExternalPort { get; set; }
        }
    }
}