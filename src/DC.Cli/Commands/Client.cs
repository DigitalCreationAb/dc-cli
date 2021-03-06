using System;
using System.Threading.Tasks;
using CommandLine;
using DC.Cli.Components.Client;

namespace DC.Cli.Commands
{
    public static class Client
    {
        public static async Task Execute(Options options)
        {
            var settings = await ProjectSettings.Read();

            var components = await Components.Components.BuildTree(settings, options.GetRootedClientPath(settings));

            await components.Initialize<ClientComponent, ClientComponentType.ComponentData>(
                new ClientComponentType.ComponentData(options.Name, options.Port, options.ClientType),
                settings);
        }
        
        [Verb("client", HelpText = "Create a client application.")]
        public class Options
        {
            [Option('n', "name", Required = true, HelpText = "Name of the client.")]
            public string Name { get; set; }

            [Option('p', "path", HelpText = "Path where to put the client.")]
            public string Path { get; set; } = Environment.CurrentDirectory;
    
            [Option('t', "type", Default = ClientType.VueNuxt, HelpText = "Client type.")]
            public ClientType ClientType { get; set; }
            
            [Option('o', "port", HelpText = "Port to run client on.")]
            public int? Port { get; set; }
            
            public string GetRootedClientPath(ProjectSettings settings)
            {
                return System.IO.Path.Combine(settings.GetRootedPath(Path), Name);
            }
        }
    }
}