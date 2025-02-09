using Microsoft.EntityFrameworkCore;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ToDoDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ToDoDB") 
        ?? throw new InvalidOperationException("Connection string is not configured.");

    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0))
    );
});

Console.WriteLine($"Connection String: {Environment.GetEnvironmentVariable("ConnectionStrings__ToDoDB")}");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoList API");
    c.RoutePrefix = string.Empty;
});
app.UseCors();

app.MapGet("/", () => "TodoList API works...");

app.MapGet("/items", async (ToDoDbContext db) =>
{
    try 
    {
        Console.WriteLine("Attempting to fetch items...");
        var items = await db.Items.ToListAsync();
        Console.WriteLine($"Found {items.Count} items");
        return Results.Ok(items);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching items: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/items/{id}", async (int id, ToDoDbContext db) =>
    await db.Items.FindAsync(id) is Item item ? Results.Ok(item) : Results.NotFound());

app.MapPost("/", async (Item item, ToDoDbContext db) => {
    db.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/items/{item.Id}", item);
});

app.MapPut("/items/{id}", async (int id, bool IsComplete, ToDoDbContext db) => {
    var item = await db.Items.FindAsync(id);
    if (item is null) return Results.NotFound();

    item.IsComplete = IsComplete;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/items/{id}", async (int id, ToDoDbContext db) => {
    var item = await db.Items.FindAsync(id);
    if(item is null) return Results.NotFound();

    db.Items.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();