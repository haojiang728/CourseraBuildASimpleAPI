using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware to log requests and responses
app.Use(async (context, next) =>
{
    var logger = app.Logger;

    // Log request
    logger.LogInformation("Incoming request: {method} {path}",
        context.Request.Method,
        context.Request.Path);

    await next();

    // Log response
    logger.LogInformation("Outgoing response: {statusCode}",
        context.Response.StatusCode);
});

// Authentication Middleware
app.Use(async (context, next) =>
{
    // Allow swagger without authentication
    if (context.Request.Path.StartsWithSegments("/swagger"))
    {
        await next();
        return;
    }

    // for demonstration purposes, just checks for a hardcoded API key in the header
    const string ApiKeyHeader = "X-Api-Key";
    const string ExpectedKey = "thesecretkey";

    if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Missing API Key");
        return;
    }

    if (providedKey != ExpectedKey)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Invalid API Key");
        return;
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// In-memory user store
var users = new Dictionary<int, User>();
var nextId = 1;

// GET all users
app.MapGet("/users", () =>
{
    return Results.Ok(users.Values);
});

// GET user by ID
app.MapGet("/users/{id}", (int id) =>
{
    return users.TryGetValue(id, out var user)
        ? Results.Ok(user)
        : Results.NotFound();
});

// POST create user
app.MapPost("/users", (User user) =>
{
    // Check for valid data - UserAge must be 0 or greater
     if (user.UserAge < 0)
        return Results.BadRequest("UserAge must be 0 or greater.");

    user.Id = nextId++;
    users[user.Id] = user;
    return Results.Created($"/users/{user.Id}", user);
});

// PUT update user
app.MapPut("/users/{id}", (int id, User updated) =>
{
    if (!users.ContainsKey(id))
        return Results.NotFound();

    // Check for valid data - UserAge must be 0 or greater
     if (updated.UserAge < 0)
        return Results.BadRequest("UserAge must be 0 or greater.");

    updated.Id = id;
    users[id] = updated;
    return Results.Ok(updated);
});

// DELETE user
app.MapDelete("/users/{id}", (int id) =>
{
    return users.Remove(id)
        ? Results.NoContent()
        : Results.NotFound();
});

app.Run();

// Simple user model
record User
{
    public int Id { get; set; }
    required public string UserName { get; set; }
    public int UserAge { get; set; }
}
