using System;
using System.Threading.Tasks;
using CommandLine;

namespace DC.AWS.Projects.Cli.Commands
{
    public static class Logs
    {
        public static async Task Execute(Options options)
        {
            var settings = await ProjectSettings.Read();

            var components = Components.Components.BuildTree(settings, options.Path);

            var restoreResult = await components.Stop();
            
            Console.Write(restoreResult.Output);
        }
        
        [Verb("logs", HelpText = "Get logs for the application.")]
        public class Options
        {
            [Option('p', "path", HelpText = "Path to get logs from")]
            public string Path { get; set; } = Environment.CurrentDirectory;
        }
    }
}