
namespace ToDo.Webapp.Services;

public class NavMenuService : INavMenuService
{
    public event Func<Task>? OnUpdate;

    public Task UpdateNavMenuAsync()
    {
        Console.WriteLine("Called UpdateNavMenuAsync from Service");
        return OnUpdate?.Invoke() ?? Task.CompletedTask;
    }
}