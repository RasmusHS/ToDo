namespace ToDo.Domain;

public static class ToDoItemStatuses
{
    public static IReadOnlyList<string> Status = new List<string>
    {
        "Backlog",
        "In progress",
        "Awaiting review"
    };
}
