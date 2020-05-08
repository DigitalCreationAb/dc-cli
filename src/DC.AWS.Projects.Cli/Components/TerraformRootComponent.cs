using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DC.AWS.Projects.Cli.Components
{
    public class TerraformRootComponent : IPackageApplication
    {
        private readonly DirectoryInfo _path;

        private TerraformRootComponent(string name, DirectoryInfo path)
        {
            Name = name;
            _path = path;
        }
        
        public string Name { get; }
        
        public async Task<PackageResult> Package(IImmutableList<PackageResource> resources, string version)
        {
            var packageResources = resources
                .Add(new PackageResource("main.tf",
                    await File.ReadAllBytesAsync(Path.Combine(_path.FullName, $"{Name}.main.tf"))));
            
            return new PackageResult($"{Name}.{version}.zip", packageResources);
        }

        public static IEnumerable<TerraformRootComponent> FindAtPath(DirectoryInfo path)
        {
            return from file in path.EnumerateFiles()
                where file.Name.EndsWith(".main.tf")
                select new TerraformRootComponent(file.Name.Substring(0, file.Name.Length - ".main.tf".Length), path);
        }
    }
}