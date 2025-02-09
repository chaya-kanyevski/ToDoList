using System;
using Microsoft.EntityFrameworkCore;
using TodoApi;

Console.WriteLine("STARTUP: Application is starting...");

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("STARTUP: WebApplication.CreateBuilder completed");

try 
{
    builder.Services.AddEndpointsApiExplorer(); 
    builder.Services.AddSwaggerGen();

    Console.WriteLine("STARTUP: Trying to get connection string");
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ToDoDB");
    
    Console.WriteLine($"STARTUP: Connection String: {connectionString ?? "NULL"}");

    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("STARTUP: CONNECTION STRING IS EMPTY OR NULL!");
        throw new InvalidOperationException("Connection string is not configured.");
    }

    builder.Services.AddDbContext<ToDoDbContext>(options =>
    {
        Console.WriteLine("STARTUP: Configuring DbContext");
        options.UseMySql(
            connectionString, 
            ServerVersion.AutoDetect(connectionString),
            mySqlOptions => 
            {
                Console.WriteLine("STARTUP: Configuring MySQL options");
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }
        );
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
    });

    Console.WriteLine("STARTUP: Services configuration complete");

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoList API");
        c.RoutePrefix = string.Empty;
    });

    app.UseCors("AllowAll");

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

    Console.WriteLine("STARTUP: All routes configured");

using (var scope = app.Services.CreateScope())
{
    try 
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
        Console.WriteLine("Attempting to run database migrations...");
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrations completed successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error running database migrations: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        throw;
    }
} 

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"STARTUP: CRITICAL ERROR IN MAIN: {ex.Message}");
    Console.WriteLine($"STARTUP: Stack Trace: {ex.StackTrace}");
    throw;
}