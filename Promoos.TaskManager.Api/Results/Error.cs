using Promoos.TaskManager.Api.Enums;

namespace Promoos.TaskManager.Api.Results;

public sealed record Error(ErrorType Type, string Message);