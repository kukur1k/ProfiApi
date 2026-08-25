using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
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
        MapDashboard(app);
        MapSkills(app);
        MapSearch(app);
        MapShortlists(app);
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

        g.MapGet("/{id:int}", async (int id, HttpContext ctx, AppDbContext db, JwtService jwt, string? mode) =>
        {
            var user = await db.Users
                .Include(u => u.IdRoleNavigation)
                .Include(u => u.Ratings)
                .Include(u => u.Skills).ThenInclude(s => s.Technology)
                .Include(u => u.Skills).ThenInclude(r => r.Confirmations)
                .Include(u => u.Experiences).ThenInclude(e => e.EmpType)
                .Include(u => u.Experiences).ThenInclude(e => e.Position)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user is null) return Api.NotFound("Пользователь не найден");

            var rating = user.Ratings.FirstOrDefault();

            if (mode == "employer")
            {
                return Api.Ok(new
                {
                    PublicId = $"ITP-{user.Id:D5}",
                    DisplayName = 
                        $"{user.LastName} {user.FirstName?[0]}.{(user.MiddleName?.Length > 0 ? user.MiddleName[0] + "." : "")} (анонимный профиль)",
                    IsActive = true,
                    CompetencyIndex = rating?.CompetencyIndex ?? 0,
                    TrustLevel = rating?.TrustLevel ?? 0,
                    ConfirmsCount = rating?.ConfirmsCount ?? 0,
                    SkillsCount = user.Skills.Count,
                    Skills = user.Skills
                        .OrderByDescending(s => s.Skilllevel)
                        .Select(s => new
                        {
                            Technology = s.Technology?.Name ?? "",
                            Level = s.Skilllevel,
                            ConfirmsCount = s.Confirmations.Count(c => c.Status == "accepted"),
                            HasConfirms = s.Confirmations.Any(c => c.Status == "accepted")
                        }),
                    Experience = user.Experiences
                        .OrderByDescending(e => e.DateEnd == null)
                        .ThenByDescending(e => e.DateStart)
                        .Select(e => new
                        {
                            DateStart = e.DateStart?.ToString("yyyy"),
                            DateEnd = e.DateEnd?.ToString("yyyy") ?? "н.в",
                            IsCurrent = e.DateEnd == null,
                            EmpType = e.EmpType?.Title ?? "",
                            Position = e.Position?.Title ?? ""
                        })
                });
            }

            return Api.Ok(new
            {
                user.Id,
                user.LastName,
                user.FirstName,
                user.MiddleName,
                user.Email,
                user.Phone,
                Role = user.IdRoleNavigation?.Title,
                user.RegisteredAt,
                CompetencyIndex = rating?.CompetencyIndex ?? 0,
                TrustLevel = rating?.TrustLevel ?? 0,
                ConfirmsCount = rating?.ConfirmsCount ?? 0,
                Skills = user.Skills
                    .OrderByDescending(s => s.Skilllevel)
                    .Select(s => new
                    {
                        Technology = s.Technology?.Name ?? "",
                        Level = s.Skilllevel,
                        ConfirmsCount = s.Confirmations.Count(c => c.Status == "accepted")
                    }),
                Experience = user.Experiences
                    .Select(e => new
                    {
                        DateStart = e.DateStart?.ToString("yyyy"),
                        DateEnd = e.DateEnd?.ToString("yyyy") ?? "н.в.",
                        IsCurrent = e.DateEnd == null,
                        EmpType = e.EmpType?.Title ?? "",
                        Position = e.Position?.Title ?? ""
                    })
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
                return Api.NotFound($"Технология с id={req.TechnologyId} не найдена");

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
        
        sk.MapDelete("/{id:int}", async (int id, HttpContext ctx, AppDbContext db, JwtService jwt) =>
        {
            var uId = jwt.GetUserId(ctx.User);

            var skill = await db.Skills.FirstOrDefaultAsync(s => s.UserId == uId && s.Id == id);
            if (skill is null) return Api.NotFound("Данный навык не найден");
            db.Remove(skill);
            await db.SaveChangesAsync();
            return Api.Ok<object?>(null, "Удалено");
        });

    }

    static void MapDashboard(WebApplication app)
    {
        var g = app.MapGroup("/dashboard").WithTags("Dashboard").RequireAuthorization();

        g.MapGet("/summary",async (AppDbContext db) =>
        {
            // всего пользователей в сети
            var totaActyvity = await db.Users.CountAsync(u => u.IdRole != null);

            var yesterday = DateTime.UtcNow.AddDays(-1);
            var weekAgo = DateTime.UtcNow.AddDays(-7);

            // суточный прирост пользователей
            var deltaUsers = await db.Users.CountAsync(u => u.RegisteredAt >= yesterday);
            // недельный прирост пользователей
            var weeklyGrowthUsers = await db.Users.CountAsync(u => u.RegisteredAt >= weekAgo);

            // средний рейтинг
            var avgRating = await db.Ratings.AverageAsync(r => (double?)r.CompetencyIndex) ?? 0;
            // средний рейтинг на той неделе
            var avgRatingLastWeek = await db.Ratings
                .Where(r => r.CalculateAt <= weekAgo)
                .AverageAsync(r => (double?)r.CompetencyIndex) ?? 0; 
            // средний рейтинг вчера
            var avgRatingYesterday = await db.Ratings
                .Where(r => r.CalculateAt >= yesterday)
                .AverageAsync(r => (double?)r.CompetencyIndex) ?? 0; 
            // Недельный прирос рейтинга
            var ratingDeltaWeek = Math.Round((avgRating - avgRatingLastWeek) /10, 2);
            var ratingDelta = Math.Round((avgRating - avgRatingYesterday) /10, 2);


            // средний уровень соответствия
            var avgTrust = await db.Ratings.AverageAsync(r => (double?)r.TrustLevel) ?? 0;
            // средний уровень соответствия на той неделе
            var avgTrustLastWeek = await db.Ratings
                .Where(r => r.CalculateAt <= weekAgo)
                .AverageAsync(r => (double?)r.TrustLevel) ?? 0; 
            // средний уровень соответствия вчера
            var avgTrustYesterday = await db.Ratings
                .Where(r => r.CalculateAt >= yesterday)
                .AverageAsync(r => (double?)r.TrustLevel) ?? 0; 
            // недельный прирос уровня соответствия
            var vacancyDeltaWeek = Math.Round(avgTrust - avgTrustLastWeek, 2);
            // дневной прирос уровня соответствия
            var vacancyDelta = Math.Round(avgTrust - avgTrustYesterday, 2);
            

            return Api.Ok(new
            {
                ActiveProfiles = totaActyvity,
                ProfilesDelta = deltaUsers,
                ProfilesDeltaWeek = weeklyGrowthUsers,
                AvgRating = Math.Round(avgRating / 10, 2),
                AvgRatingDelta = ratingDelta,
                AvgRatingDeltaWeek = ratingDeltaWeek,
                VacancyMatch = Math.Round(avgTrust, 2),
                VacancyMatchDelta = vacancyDelta,
                VacancyMatchDeltaWeek = vacancyDeltaWeek
            });

        });
    }

    static async Task MapSkills(WebApplication app)
    {
        var g = app.MapGroup("/skills").WithTags("Skills").RequireAuthorization();

        g.MapGet("/top", async (AppDbContext db) =>
        {
            var totalCount = await db.Skills.CountAsync();
            if (totalCount == 0) return Api.Ok(new { Items = Array.Empty<object>() });

            var skills = await db.Skills
                .Include(s => s.Technology)
                .GroupBy(s => new {s.TechnologyId, s.Technology!.Name})
                .Select(g => new {g.Key.Name,
                    Count = g.Count(),
                    Percent = Math.Round((double)g.Count() / totalCount * 100, 2)})
                .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();
            


            return Api.Ok(new {Items = skills});
        });

        g.MapGet("/suggest", async (string? q, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length <= 1)
                return Api.Ok(Array.Empty<string>());

            var result = await db.Technologies
                .Where(t => t.Name.ToLower().StartsWith(q.ToLower()))
                .Select(t => t.Name)
                .Take(8)
                .ToListAsync();
            
            return Api.Ok(result);
        });
    }

    static void MapSearch(WebApplication app)
    {
        var g = app.MapGroup("/users").WithTags("Search").RequireAuthorization();

        g.MapGet("/search", async (
            AppDbContext db,
            [FromQuery] string? technology = null,
            int minLevel = 0,
            int maxLevel = 10,
            double minRating = 0,
            int minExp = 0) =>
        {
            // общая часть запроса
            var query = db.Users
                .Include(u => u.Skills).ThenInclude(s => s.Technology)
                .Include(u => u.Ratings)
                .Include(u => u.Experiences)
                .Where(u => u.IdRole != null)
                .AsQueryable();

            // поиск по технологии
            if (!string.IsNullOrWhiteSpace(technology))
            {   
                var techs = technology.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(u => 
                    u.Skills.Any(s => 
                    techs.Any(t =>
                        s.Technology!.Name.ToLower().Contains(t.ToLower())) 
                    && s.Skilllevel >= minLevel
                    && s.Skilllevel <= maxLevel));
            }

            // поиск по технологии
            if (minRating > 0)
            {
                query = query.Where(u => 
                    u.Ratings.Any(r => (double)r.CompetencyIndex >= minRating));
            }

            // ищем суммарный стаж

            // if (minExp > 0)
            // {
            //     query = query.Where(u => 
            //         u.Experiences.Sum(e => 
            //             (e.DateEnd ?? DateOnly.FromDateTime(DateTime.Now))
            //             .ToDateTime(TimeOnly.MinValue) - e.DateStart.DayNumber
            //         ) >= minExp);
            // }

            var total = await query.CountAsync();

            // взяли 2 рейтинга первых, и на их основе далее просчитаем рост это, или падение (тренд)
            var Items = await query
                .Select(u => new
                {
                    Id = u.Id,
                    DisplayName = u.LastName + " " + u.FirstName!.Substring(0, 1) + ".",
                    Skills = u.Skills
                        .OrderByDescending(s => s.Skilllevel)
                        .Select(s => $"{s.Technology!.Name}:{s.Skilllevel}").ToArray(),
                    CompetencyIndex = u.Ratings.Max(r => (double?)r.CompetencyIndex) ?? 0,
                    TrustLevel = u.Ratings.Max(r => (double?)r.TrustLevel) ?? 0,
                    CurrentRating = u.Ratings.OrderByDescending(r => r.CalculateAt).FirstOrDefault(),
                    PreviousRating = u.Ratings.OrderByDescending(r => r.CalculateAt).Skip(1).FirstOrDefault()
                }).ToListAsync();
            
            var result = Items.Select(i => new
            {
                Id = i.Id,
                DisplayName = i.DisplayName,
                Skills = i.Skills,
                CompetencyIndex = i.CompetencyIndex,
                TrustLevel = i.TrustLevel,
                Trend = GetTrend(i.CurrentRating?.CompetencyIndex, i.PreviousRating?.CompetencyIndex)
            });

            return Api.Ok(new {Total = total, Items = result});

        });
    }


    private static string GetTrend(decimal? current, decimal? previous)
    {
        if (!current.HasValue || !previous.HasValue) return "➡️"; // stable;

        var diff = current.Value - previous.Value;

        if (diff > 0.5m) return "📈"; // up
        if (diff < 0.5m) return "📉"; // down
        return "➡️"; // stable;
    }


    static void MapShortlists(WebApplication app)
    {
        var g = app.MapGroup("/shortlists").WithTags("Shortlists").RequireAuthorization();

        // GET /shortlists
        g.MapGet("/", async (HttpContext ctx, AppDbContext db, JwtService jwt) =>
        {
            var uId = jwt.GetUserId(ctx.User);
            var list = await db.Shortlists
                .Where(s => s.OwnerId == uId)
                .OrderByDescending(s => s.CreatedAt)
                
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    s.CreatedAt,
                    CandidatesCount = s.ShortlistCandidates.Count,
                    Candidates = s.ShortlistCandidates.Select(sl => new
                    {
                        PublicId = $"ITP-{sl.UserId:D5}",
                      
                        Rating = sl.User.Ratings
                            .Select(r => (double?)r.CompetencyIndex)
                            .Max() ?? 0
                    })
                })
                .ToListAsync();

            return Api.Ok(list);
        });


        g.MapPost("/", async (HttpContent ctx, AppDbContext db, JwtService jwt, ShortlistRequest req) =>
        {
            var sl = new Shortlist
            {
                Name = req.Name,
                Description = req.Description
            };

            await db.Shortlists.AddAsync(sl);
            return Api.Created($"/shortlists/{sl.Id}", new {sl.Id, sl.Name});
        });


        g.MapDelete("/{id:int}", async (int id, HttpContext ctx, AppDbContext db, JwtService jwt) =>
        {
            var uid = jwt.GetUserId(ctx.User);
            var s = await db.Shortlists
                .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == uid);

            if (s is null) return Api.NotFound("Подборка не найдена");

            db.Shortlists.Remove(s);
            await db.SaveChangesAsync();

            return Api.Ok<object?>(null, "Подборка удалена");
        });

        // добавление кандидата в подборку
        g.MapPost("/{id:int}/candidates", async (int id,
            ShortlistAddCandidateRequest req,
            HttpContext ctx, AppDbContext db, JwtService jwt) =>
        {
            var uid = jwt.GetUserId(ctx.User);

            if (!await db.Shortlists.AnyAsync(s => s.Id == id && s.OwnerId == uid))
                return Api.NotFound("Подборка не найдена");

            if (!await db.Users.AnyAsync(u => u.Id == req.UserId))
                return Api.NotFound("Пользователь не найден");

            if (await db.ShortlistCandidates
                    .AnyAsync(sc => sc.ShortlistId == id && sc.UserId == req.UserId))
                return Api.Conflict("Кандидат уже в подборке");

            var sc = new ShortlistCandidate
            {
                ShortlistId = id,
                UserId = req.UserId,
                Note = req.Note,
                AddedAt = DateTime.UtcNow
            };

            db.ShortlistCandidates.Add(sc);
            await db.SaveChangesAsync();

            return Api.Ok(new { sc.ShortlistId, sc.UserId, sc.AddedAt });
        });

        // удаление кандидата из подборки
        g.MapDelete("/{id:int}/candidates/{userId:int}", async (int id, int userId,
            HttpContext ctx, AppDbContext db, JwtService jwt) =>
        {
            var uid = jwt.GetUserId(ctx.User);

            if (!await db.Shortlists.AnyAsync(s => s.Id == id && s.OwnerId == uid))
                return Api.NotFound("Подборка не найдена");

            var sc = await db.ShortlistCandidates
                .FirstOrDefaultAsync(x => x.ShortlistId == id && x.UserId == userId);

            if (sc is null) return Api.NotFound("Кандидат не найден в подборке");

            db.ShortlistCandidates.Remove(sc);
            await db.SaveChangesAsync();

            return Api.Ok<object?>(null, "Кандидат удалён из подборки");
        });

        g.MapDelete("/{id:int}", async (HttpContext ctx, JwtService jwt, AppDbContext db, int id) =>
        {
           var uid = jwt.GetUserId(ctx.User);

            var sl = await db.Shortlists.FirstOrDefaultAsync(x => x.Id == id);

            db.Shortlists.Remove(sl);
            await db.SaveChangesAsync();

        });
    }

}
