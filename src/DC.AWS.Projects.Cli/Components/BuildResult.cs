using System.Collections.Immutable;

namespace DC.AWS.Projects.Cli.Components
{
    public class BuildResult : IComponentResult
    {
        public BuildResult(bool success, string output)
        {
            Success = success;
            Output = output;
        }

        public bool Success { get; }
        public string Output { get; }
    }
}