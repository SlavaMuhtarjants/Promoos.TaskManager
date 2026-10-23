namespace Promoos.TaskManager.Api.Configuration;

public sealed class RabbitMqOptions
{
    public required string HostName { get; init; }
    public int Port { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public required string Exchange { get; init; }
}