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
        MapProfile(app);
        MapProfileData(app);
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

            var user = await db.Users
                .Include(u => u.IdRoleNavigation)
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user is not null)
                return Api.Fail(401, "Данный Email уже зарегестрирован", "INVALID_CREDENTIALS");

            var newUser = new User()
            {
                Email = req.Email,
                PasswordHash = BC.HashPassword(req.Password),
                FirstName = req.FirstName,
                LastName = req.LastName,
                MiddleName = req.MiddleName,
                Phone = req.Phone,
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

        g.MapPost("/logout", async (RefreshRequest req, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return Api.BadRequest("RefreshToken обязателен");

            var token = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == req.RefreshToken && t.IsRevoked != true);

            if (token is not null)
            {
                token.IsRevoked = true;
                await db.SaveChangesAsync();
            }
            return Api.Ok<object?>(null, "Выход выполнен");
        });

        g.MapPost("/refresh", async (RefreshRequest req, AppDbContext db, JwtService jwt) =>
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return Api.BadRequest("RefreshToken обязателен");

            var token = await db.RefreshTokens
                .Include(t => t.User).ThenInclude(u => u.IdRoleNavigation)
                .FirstOrDefaultAsync(t => t.Token == req.RefreshToken);

            if (token is null || token.IsRevoked == true || token.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return Api.Fail(401, "Токен недействителен или истёк", "INVALID_REFRESH_TOKEN");
            }

            token.IsRevoked = true;
            var newRefresh = await CreateRefreshToken(token.UserId!.Value, db);

            return Api.Ok(new AuthResponce(
                jwt.Generate(token.User),
                newRefresh.Token,
                token.User.IdRoleNavigation.Title
            ));
        });
    }

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

    static void MapProfile(WebApplication app)
    {
        var g = app.MapGroup("/users").WithTags("Profile").RequireAuthorization();

        g.MapGet("/me", async (HttpContext ctx, AppDbContext db, JwtService jwt) =>
        {
            var id = jwt.GetUserId(ctx.User);
            var user = await db.Users
                .Include(u => u.IdRoleNavigation)
                .Include(u => u.Ratings)
                .Include(u => u.Skills).ThenInclude(s => s.Technology)
                .Include(u => u.Skills).ThenInclude(s => s.Confirmations)
                .Include(u => u.Educations).ThenInclude(e => e.EduInstitution)
                .Include(u => u.Educations).ThenInclude(e => e.EduType)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user is null) return Api.NotFound("Пользователдь не найден");
            var rating = user.Ratings.FirstOrDefault();
            return Api.Ok(new
            {
                user.Id,
                user.LastName,
                user.FirstName,
                user.MiddleName,
                user.Email,
                user.Phone,
                role = user.IdRoleNavigation?.Title,
                user.RegisteredAt,
                rating = rating is null
                    ? null
                    : new
                    {
                        rating.CompetencyIndex,
                        rating.TrustLevel,
                        rating.ConfirmsCount,
                        rating.CalculateAt
                    },
                skills = user.Skills
                    .OrderByDescending(s => s.Skilllevel)
                    .Select(s => new
                    {
                        technology = s.Technology?.Name ?? "",
                        level = s.Skilllevel,
                        confirmsCount = s.Confirmations.Count(c => c.Status == "accepted"),
                        hasConfirms = s.Confirmations.Any(c => c.Status == "accepted")
                    }),
                confirmations = user.ConfirmationTargets.Select(c => new
                {
                    name = db.Users.FirstOrDefault(u => u.Id == c.TargetId).LastName,
                    technology = c.Skill.Technology.Name,
                    dateConfirm = c.RespondedAt ?? DateTimeOffset.UtcNow
                }),
                educations = user.Educations.Select(ed => new
                {
                    type = ed.EduType.Title,
                    eduInstitution = ed.EduInstitution.Title,
                    dates = ed.DateStart.Value.Year.ToString() + "-" + ed.DateEnd.Value.Year.ToString() ?? "н.в"
                })

            });
        });
        
        g.MapPut("/me", async(UpdateProfileRequest req, HttpContext ctx, AppDbContext db, JwtService jwt) =>
        {
            var id = jwt.GetUserId(ctx.User);
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return Api.NotFound("Пользователь не найден");

            
            if (req.Phone is not null && await db.Users.AnyAsync(u => u.Phone == req.Phone && u.Id != id))
                return Api.Conflict("Данный телефон уже заркгкстрирован");

            if (req.FirstName is not null)
                user.FirstName = req.FirstName;
            if (req.LastName is not null)
                user.LastName = req.LastName;
            if (req.MiddleName is not null)
                user.MiddleName = req.MiddleName;
            if (req.Phone is not null)
                user.Phone = req.Phone;

            await db.SaveChangesAsync();

            return Api.Ok(new
            {
                user.Id,
                user.Email,
                user.LastName,
                user.FirstName,
                user.MiddleName,
                user.Phone
            });
        });
    }

    static void MapProfileData(WebApplication app)
    {
        var edu = app.MapGroup("/users/me/education").WithTags("Education").RequireAuthorization();

        edu.MapPost("/", async (EducationRequest req, HttpContext ctx, AppDbContext db, JwtService jwt) =>
        {
            var id = jwt.GetUserId(ctx.User);

            var e = new Education
            {
                UserId = id,
                EduTypeId = req.EduTypeId,
                EduInstitutionId = req.EduInstitutionId,
                DateStart = req.DateStart,
                DateEnd = req.DateEnd,
            };

            db.Add(e);
            await db.SaveChangesAsync();

            return Api.Created($"/users/me/education/{e.Id}", 
                new {e.Id, e.EduTypeId, e.EduInstitutionId, e.DateStart, e.DateEnd});
        });

        edu.MapPut("/{id:int}",
            async (int id, EducationRequest req, HttpContext ctx, AppDbContext db, JwtService jwt) =>
            {
                var uId = jwt.GetUserId(ctx.User);

                var e = await db.Educations.FirstOrDefaultAsync(e => e.Id == id && e.UserId == uId);
                if (e is null) return Api.NotFound("Запись об образовании не найдена");

                if (req.DateEnd.HasValue && req.DateEnd <= req.DateStart) return Api.BadRequest("Дата окончания должна быть позже даты начала");

                if (req.EduTypeId.HasValue && !await db.EducaitonTypes.AnyAsync(t => t.Id == req.EduTypeId))
                    return Api.BadRequest($"Тип образования с ID={req.EduTypeId} не существует");

                if (req.EduInstitutionId != null)
                    e.EduInstitutionId = req.EduInstitutionId;
                if (req.EduTypeId != null)
                    e.EduTypeId = req.EduTypeId;
                if (req.DateStart != null)
                    e.DateStart = req.DateStart;
                if (req.DateEnd != null)
                    e.DateEnd = req.DateEnd;
                await db.SaveChangesAsync();
                return Api.Ok(new {e.Id, e.EduInstitutionId, e.EduTypeId, e.DateStart, e.DateEnd});
            });
        
        edu.MapDelete("/{id:int}", async (int id, HttpContext ctx, AppDbContext db, JwtService jwt) =>
        {
            var uId = jwt.GetUserId(ctx.User);

            var e = await db.Educations.FirstOrDefaultAsync(e => e.Id == id && e.UserId == uId);
            if (e is null) return Api.NotFound("Запись об образовании не найдена");

            db.Remove(e);
            await db.SaveChangesAsync();
            return Api.Ok<object?>(null, "Удалено");
        });



        var sk = app.MapGroup("/users/me/skills/").WithTags("Skills").RequireAuthorization();

        sk.MapPost("/", async (SkillRequest req, HttpContext ctx, AppDbContext db, JwtService jwt) =>
        {
            var uId = jwt.GetUserId(ctx.User);

            if (!await db.Technologies.AnyAsync(t => t.Id == req.TechnologyId))
                return Api.NotFound("Технология не найдена");

            if (await db.Skills.AnyAsync(s => s.TechnologyId == req.TechnologyId && s.UserId == uId))
                return Api.Conflict("Навык уже добавлен");

            var skill = new Skill
            {
                UserId = uId,
                TechnologyId = req.TechnologyId,
                Skilllevel = req.SkillLevel
            };

            db.Skills.Add(skill);
            await db.SaveChangesAsync();
            var tech = await db.Technologies.FindAsync(req.TechnologyId);
            return Api.Created($"/users/me/skills/{skill.Id}", new{ technology = tech!.Name, skill.Skilllevel});
        });

        sk.MapPut("/{id:int}",
            async (int id, SkillRequest req, HttpContext ctx, AppDbContext db, JwtService jwt) =>
            {
                var uId = jwt.GetUserId(ctx.User);

                var skill = await db.Skills.FirstOrDefaultAsync(s => s.UserId == uId && s.Id == id);
                if (skill is null) return Api.NotFound("Данный навык не найден");
                if (req.SkillLevel < 0 || req.SkillLevel > 10)
                    return Api.BadRequest("Уровень навыка должен быть от 0 до 10");

                skill.Skilllevel = req.SkillLevel;
                await db.SaveChangesAsync();
                return Api.Ok(new {skill.Id, skill.Skilllevel});
            });
        
        

    }
}
