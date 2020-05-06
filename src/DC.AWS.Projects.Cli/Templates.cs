using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DC.AWS.Projects.Cli
{
    public static class Templates
    {
        public static Task Extract(
            string resourceName,
            string destination,
            TemplateType templateType,
            params (string name, string value)[] variables)
        {
            return Extract(resourceName, destination, templateType, true, variables);
        }
        
        public static async Task Extract(
            string resourceName,
            string destination,
            TemplateType templateType,
            bool overwrite,
            params (string name, string value)[] variables)
        {
            if (!overwrite && File.Exists(destination))
                return;
            
            var directory = Path.GetDirectoryName(destination);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            await File.WriteAllTextAsync(
                destination, 
                await GetContent(resourceName, templateType, variables));
        }

        private static async Task<string> GetContent(
            string resourceName,
            TemplateType templateType,
            params (string name, string value)[] variables)
        {
            var infrastructureTemplateData = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream($"DC.AWS.Projects.Cli.Templates.{templateType.ToString()}.{resourceName}");

            await using (infrastructureTemplateData)
            using(var reader = new StreamReader(infrastructureTemplateData!))
            {
                var templateData = await reader.ReadToEndAsync();

                templateData = variables
                    .Aggregate(
                        templateData,
                        (current, variable) => current
                            .Replace($"[[{variable.name}]]", variable.value.Trim()));

                return templateData;
            }
        }
        
        public enum TemplateType
        {
            Infrastructure,
            Config,
            Services
        }
    }
}