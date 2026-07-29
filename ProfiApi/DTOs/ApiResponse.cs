namespace ProfiApi.DTOs;

public record ApiResponce<T>(
    bool success,
    T? Data,
    string Message,
    string? ErrorCode = null
);

public static class Api
{
    public static IResult Ok<T>(T data, string message = "OK") =>
        Results.Ok(new ApiResponce<T>(true, data, message));

    public static IResult Created<T>(T data, string uri) =>
        Results.Created(uri, new ApiResponce<T>(true, data, "Создано"));

    public static IResult Fail(int status, string message, string errorCode) =>
        Results.Json(new ApiResponce<object>(false, null, message, errorCode), statusCode: status);

    public static IResult BadRequest(string message)
        => Fail(400, message, "BAD_REQUEST");
    public static IResult Unauthorized()
        => Fail(401, "Не авторизован", "UNAUTHORIZED");
    public static IResult Forbidden()
        => Fail(403, "Нет доступа", "FORBIDDEN");
    public static IResult NotFound(string message)
        => Fail(404, message, "NOT_FOUND");
    public static IResult Conflict(string message)
        => Fail(409, message, "CONFLICT");
}