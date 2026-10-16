
namespace ToDo.Webapp.Services;

public interface INavMenuService
{
    event Func<Task> OnUpdate;

    public Task UpdateNavMenuAsync();
}
