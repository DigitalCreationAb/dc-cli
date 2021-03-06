using System;
using System.Threading.Tasks;
using CommandLine;
using DC.Cli.Components.Aws.LambdaFunction;

namespace DC.Cli.Commands
{
    public static class Func
    {
        public static async Task Execute(Options options)
        {
            var projectSettings = await ProjectSettings.Read();

            var components = await Components.Components.BuildTree(
                projectSettings,
                options.GetFunctionPath(projectSettings));

            await components.Initialize<LambdaFunctionComponent, LambdaFunctionComponentType.ComponentData>(
                new LambdaFunctionComponentType.ComponentData(
                    options.Name, 
                    options.GetTrigger(),
                    options.Language),
                projectSettings);
        }
        
        [Verb("func", HelpText = "Create a function.")]
        public class Options
        {
            [Option('n', "name", Required = true, HelpText = "Name of the function.")]
            public string Name { get; set; }
            
            [Option('l', "lang", HelpText = "Language to use for the function.")]
            public string Language { get; set; }

            [Option('t', "trigger", HelpText = "Trigger type for the function.", Required = true)]
            public string Trigger { private get; set; }

            [Option('p', "path", HelpText = "Path where to put the function.")]
            public string Path { private get; set; } = Environment.CurrentDirectory;

            public string GetFunctionPath(ProjectSettings settings)
            {
                return settings.GetRootedPath(System.IO.Path.Combine(Path, Name));
            }

            public FunctionTrigger GetTrigger()
            {
                return Trigger switch
                {
                    "api" => FunctionTrigger.Api,
                    "kinesis-stream" => FunctionTrigger.KinesisStream,
                    "dynamodb-stream" => FunctionTrigger.DynamoDbStream,
                    "cron" => FunctionTrigger.Cron,
                    _ => throw new Exception("You must select a trigger")
                };
            }
        }
    }
}