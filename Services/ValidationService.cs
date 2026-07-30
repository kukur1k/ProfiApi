using ProfiApi;
using ProfiApi.DTOs;
using Microsoft.AspNetCore.Http;

namespace ProfiApi.Services;

public static class Validator
{
    public static List<String> Validate(RegisterRequest req)
    {
        var errors = new List<String>();
        if (string.IsNullOrWhiteSpace(req.Email)) errors.Add("Email обязателен");
        if (!req.Email.Contains('@'))                errors.Add("Некорректный Email");
        if (string.IsNullOrWhiteSpace(req.Password)) errors.Add("Пароль обязателен");
        if (req.Password.Length < 6)                 errors.Add("Пароль минимум 6 символов");
        if (string.IsNullOrWhiteSpace(req.LastName)) errors.Add("Фамилия обязательна");
        if (string.IsNullOrWhiteSpace(req.FirstName))errors.Add("Имя обязательно");
        return errors;
    }

    public static List<String> Validate(LoginRequest req)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(req.Email))    errors.Add("Email обязателен");
        if (string.IsNullOrWhiteSpace(req.Password)) errors.Add("Пароль обязателен");
        return errors;
    }

    public static IResult? Check(List<string> errors)
    {
        return errors.Count > 0
            ? Api.BadRequest(string.Join("; ", errors))
            : null;
    }
}
