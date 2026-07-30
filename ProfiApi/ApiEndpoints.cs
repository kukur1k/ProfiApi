using System;
using System.Text;
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

        g.MapPost("/register", async (RegisterRequest req, AppDbContext db, JwtService jwt) =>
        {
            var err = Validator.Check(Validator.Validate(req));
            if (err is not null) return err;

            var user = db.Users
                .Include(u => u.IdRoleNavigation)
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user is not null)
                return Api.Fail(401, "Данный Email уже заркгестрирован", "INVALID_CREDENTIALS");

            var newUser = new User()
            {
                Email = req.Email,
                PasswordHash = BC.HashPassword(req.Password),
                FirstName = req.FirstName,
                LastName = req.LastName,
                MiddleName = req.MiddleName,
                IdRole = 1
            };

            await db.Users.AddAsync(newUser);
            await db.SaveChangesAsync();
            await db.Entry(newUser).Reference(u => u.IdRoleNavigation).LoadAsync();
            var refresh = await CreateRefreshToken(newUser.Id, db);
            return Api.Ok(new AuthResponce(
                jwt.Generate(newUser), refresh.Token, newUser.IdRoleNavigation.Title
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
