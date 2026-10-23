using Promoos.TaskManager.Api.Enums;
using Promoos.TaskManager.Api.Results;

using HttpResult = Microsoft.AspNetCore.Http.Results;

namespace Promoos.TaskManager.Api.Mappers;

public static class ResultMapper
{
    public static IResult ToHttpResult(this Result result)
    {
        return result.IsSuccess
            ? HttpResult.NoContent()
            : MapError(result.Error!);
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : MapError(result.Error!);
    }

    private static IResult MapError(Error error) => error.Type switch
    {
        ErrorType.NotFound => HttpResult.Problem(detail: error.Message, statusCode: StatusCodes.Status404NotFound),
        ErrorType.Conflict => HttpResult.Problem(detail: error.Message, statusCode: StatusCodes.Status409Conflict),
        ErrorType.Validation => HttpResult.Problem(detail: error.Message, statusCode: StatusCodes.Status400BadRequest),
        _ => HttpResult.Problem(detail: error.Message, statusCode: StatusCodes.Status500InternalServerError)
    };
}