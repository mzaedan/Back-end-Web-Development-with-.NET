using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path} {DateTime.UtcNow} Started");
    await next(context);
    Console.WriteLine($"Response: {context.Request.Method} {context.Request.Path} {context.Response.StatusCode} {DateTime.UtcNow} Finished");
});
app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

var todos = new List<Todo>();

app.MapGet("/todos", () => todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
    var TargetTodo = todos.SingleOrDefault(t => t.Id == id);
    return TargetTodo is null 
    ? TypedResults.NotFound() 
    : TypedResults.Ok(TargetTodo);
});

app.MapPost("/todos", (Todo task) =>
{
    todos.Add(task);
    return TypedResults.Created($"/todos/{task.Id}", task);
}).AddEndpointFilter(async (context, next) =>
{
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();

    if (taskArgument.DueDate < DateTime.UtcNow)
    {
        errors.Add("DueDate", new[] { "Due date cannot be in the past." });
    }

    if (taskArgument.IsCompleted)
    {
        errors.Add(nameof(taskArgument.IsCompleted), ["Cannot add completed todo."]);
    }

    if(errors.Count > 0)
    {
        return TypedResults.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapDelete("/todos/{id}", (int id) =>
{
    todos.RemoveAll(t => t.Id == id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);

// public interface ITaskService
// {
//     Task<Todo?> GetTodoByIdAsync(int id);
//     Task<IEnumerable<Todo>> GetAllTodosAsync();
//     Task AddTodoAsync(Todo todo);
//     Task DeleteTodoAsync(int id);
// }
