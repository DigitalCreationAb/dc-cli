using System.Collections.Generic;
using System.Threading.Tasks;

namespace DC.Cli.Components
{
    public interface INeedConfiguration : IComponent
    {
        Task<IEnumerable<(string key, string question, ConfigurationType configurationType)>> GetRequiredConfigurations(
            Components.ComponentTree components);
        
        public enum ConfigurationType
        {
            Project,
            User
        }
    }
}