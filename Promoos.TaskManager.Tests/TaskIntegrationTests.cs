using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Promoos.TaskManager.Api.Contracts;
using Promoos.TaskManager.Api.Database;
using Promoos.TaskManager.Api.Dto;
using Promoos.TaskManager.Api.Enums;
using Promoos.TaskManager.Api.Publishers;

using System.Net;
using System.Net.Http.Json;

using Xunit;

namespace Promoos.TaskManager.Tests;

public class TaskIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITaskEventPublisher _publisherMock = Substitute.For<ITaskEventPublisher>();

    public TaskIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 1. Remove the standard DbContextOptions registration
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                // 2. CRITICAL FOR EF CORE 9: Remove the underlying provider configuration
                // This stops Npgsql from conflicting with InMemory
                var configDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>));

                if (configDescriptor != null)
                {
                    services.Remove(configDescriptor);
                }

                var databaseName = "TestDb_" + Guid.NewGuid();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                });

                services.RemoveAll<ITaskEventPublisher>();
                services.AddSingleton(_publisherMock);
            });
        });
    }

    [Fact]
    public async Task CreateAndCompleteTask_ShouldPublishTaskCompletedEvent()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createRequest = new CreateTaskRequest
        {
            Name = "Buy milk",
            Priority = Priority.Low
        };
        var createResponse = await client.PostAsJsonAsync("/tasks", createRequest, cancellationToken: TestContext.Current.CancellationToken);

        createResponse.EnsureSuccessStatusCode();
        var taskId = await createResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var completeResponse = await client.PutAsync($"/tasks/{taskId}/complete", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        await _publisherMock.Received(1).PublishCompletedAsync(
            Arg.Is<TaskCompletedEvent>(x => x.TaskId == taskId && x.Title == "Buy milk"),
            Arg.Any<CancellationToken>());
    }
}