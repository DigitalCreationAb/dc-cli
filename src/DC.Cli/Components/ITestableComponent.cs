using System.Threading.Tasks;

namespace DC.Cli.Components
{
    public interface ITestableComponent : IComponent
    {
        Task<ComponentActionResult> Test();
    }
}