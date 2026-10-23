using Promoos.TaskManager.Api.Enums;

using System.ComponentModel.DataAnnotations;

namespace Promoos.TaskManager.Api.Dto;

public sealed record CreateTaskRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [EnumDataType(typeof(Priority))]
    public Priority Priority { get; init; }
}