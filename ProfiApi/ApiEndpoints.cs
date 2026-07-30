using System;
using Microsoft.EntityFrameworkCore;
using ProfiApi.Data;
using ProfiApi.DTOs;
using ProfiApi.Models;
using ProfiApi.Services;
using BC = BCrypt.Net.BCrypt;

namespace ProfiApi;

public static class ApiEndpoints
{
    public static void MapAll(WebApplication app)
    {
        MapAuth(app);
    }

    static void MapAuth(WebApplication app)
    {
        var g = app.MapGroup("/auth").WithTags("Auth").RequireAuthorization();

        //===ендпоинт для входа===
        g.MapPost("/login", async (LoginRequest req, AppDbContext db, JwtService jwt) =>
        {
            var err = Validator.Check(Validator.Validate(req));
            if (err is not null) return err;

            var user = await db.Users
                .Include(u => u.IdRoleNavigation)
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user is null || !BC.Verify(req.Password, user.PasswordHash)){
                return Api.Fail(401, "Неверный Email или пароль", "INVALID_CREDENTIALS");
            }

            var oldTokens = await db.RefreshTokens
                .Where(t => t.UserId == user.Id && t.IsRevoked != true)
                .ToListAsync();
            oldTokens.ForEach(t => t.IsRevoked = true);

            var refresh = await CreateRefreshToken(user.Id, db);
            return Api.Ok(new AuthResponce(
                jwt.Generate(user), refresh.Token, user.IdRoleNavigation.Title
            ));

        }).AllowAnonymous();

        // ===Метод для создания рефреш токена===
        static async Task<RefreshToken> CreateRefreshToken(int userId, AppDbContext db)
        {
            var token = new RefreshToken
            {
                UserId = userId,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
                        Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            };
            db.RefreshTokens.Add(token);
            await db.SaveChangesAsync();
            return token;
        }
    }
}
