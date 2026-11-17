using Microsoft.EntityFrameworkCore;

using NReco.Logging.File;

using Promoos.TaskManager.Api.Configuration;
using Promoos.TaskManager.Api.Database;
using Promoos.TaskManager.Api.Endpoints;
using Promoos.TaskManager.Api.Channels;
using Promoos.TaskManager.Api.Publishers;
using Promoos.TaskManager.Api.Services;
using Promoos.TaskManager.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFile("logs/app-{0:yyyy-MM-dd}.log", fileLoggerOpts =>
{
    fileLoggerOpts.FormatLogFileName = fileName => string.Format(fileName, DateTime.UtcNow);
    fileLoggerOpts.Append = true;
});

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();

builder.Services
    .AddOptions<RabbitMqOptions>()
    .BindConfiguration("RabbitMq");

// On shutdown, give in-flight requests and queued task-completed events up to 5 seconds to complete.
builder.Services.Configure<HostOptions>(hostOptions => hostOptions.ShutdownTimeout = TimeSpan.FromSeconds(5));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Dependency injection configuration
builder.Services.AddScoped<TaskService>();
builder.Services.AddSingleton<TaskCompletedEventChannel>();
builder.Services.AddSingleton<ITaskEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<TaskCompletedEventPublishingWorker>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapEndpoints();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var context = services.GetRequiredService<AppDbContext>();

// Since integration tests work with in-memory databases, only a relational database needs to be migrated
if (context.Database.IsRelational())
{
    await context.Database.MigrateAsync();
}

app.Run();