using E_learningProject.Core.Entities;
using E_learningProject.Core.Enums;
using E_learningProject.Data.Context;
using E_learningProject.Data.Repositories;
using E_learningProject.Services;
using E_learningProject.Services.Interfaces;
using E_learningProject.Web.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var connectionString = Environment.GetEnvironmentVariable("MICROLMS_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var applyMigrationsOnStartup = builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
    var seedDemoDataOnStartup = builder.Configuration.GetValue<bool>("Database:SeedDemoDataOnStartup");

    try
    {
        // Create the target database and schema objects when missing.
        if (!await dbContext.Database.CanConnectAsync())
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        if (applyMigrationsOnStartup)
        {
            dbContext.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database initialization failed at startup. Check PostgreSQL connection settings.");
    }

    if (seedDemoDataOnStartup)
    {
        try
        {
            if (!await dbContext.Modules.AnyAsync())
            {
                var fundamentals = new Module
                {
                    Title = "Fondamentaux C#",
                    Description = "Introduction aux variables, conditions, boucles et methodes.",
                    Lessons = new List<Lesson>
                    {
                        new() { Title = "Variables et types", TextContent = "Decouvrez les types C# de base et les declarations de variables.", Order = 1 },
                        new() { Title = "Conditions et boucles", TextContent = "Controlez le flux d'execution avec les conditions et les boucles.", Order = 2 }
                    }
                };

                var aspNet = new Module
                {
                    Title = "ASP.NET Core MVC",
                    Description = "Construisez des applications web avec des controleurs, des vues et le routage.",
                    Lessons = new List<Lesson>
                    {
                        new() { Title = "Controleurs et actions", TextContent = "Comprenez le traitement des requetes en MVC.", Order = 1 },
                        new() { Title = "Vues et Razor", TextContent = "Generez du HTML cote serveur avec les vues Razor.", Order = 2 }
                    }
                };

                dbContext.Modules.AddRange(fundamentals, aspNet);
                await dbContext.SaveChangesAsync();
            }

            if (!await dbContext.Quizzes.AnyAsync())
            {
                var module = await dbContext.Modules.OrderBy(m => m.Id).FirstAsync();

                var quiz = new Quiz
                {
                    Title = "Quiz sur les fondamentaux C# et MVC",
                    PassingScore = 70,
                    Questions = new List<Question>
                    {
                        new()
                        {
                            Statement = "Quel mot-cle declare une classe en C# ?",
                            Type = QuestionType.MultipleChoice,
                            Options = new List<Option>
                            {
                                new() { Text = "class", IsCorrect = true },
                                new() { Text = "struct", IsCorrect = false },
                                new() { Text = "define", IsCorrect = false }
                            }
                        },
                        new()
                        {
                            Statement = "ASP.NET MVC utilise des controleurs pour traiter les requetes.",
                            Type = QuestionType.TrueFalse,
                            Options = new List<Option>
                            {
                                new() { Text = "Vrai", IsCorrect = true },
                                new() { Text = "Faux", IsCorrect = false }
                            }
                        },
                        new()
                        {
                            Statement = "Ecrivez l'acronyme de ce pattern web : Model-View-Controller.",
                            Type = QuestionType.ShortAnswer,
                            Options = new List<Option>
                            {
                                new() { Text = "MVC", IsCorrect = true }
                            }
                        }
                    }
                };

                dbContext.Quizzes.Add(quiz);
                await dbContext.SaveChangesAsync();

                module.QuizId = quiz.Id;
                await dbContext.SaveChangesAsync();
            }

            if (!await dbContext.DiscussionThreads.AnyAsync())
            {
                var thread = new DiscussionThread
                {
                    Title = "Difference entre un controleur MVC et une Razor Page ?",
                    StudentId = "student.demo",
                    CreatedAt = DateTime.UtcNow,
                    IsResolved = false,
                    Replies = new List<DiscussionReply>
                    {
                        new()
                        {
                            AuthorId = "teacher.demo",
                            Message = "Les controleurs sont bases sur des actions et definissent les routes explicitement, alors que les Razor Pages sont centrees sur les pages.",
                            CreatedAt = DateTime.UtcNow
                        }
                    }
                };

                dbContext.DiscussionThreads.Add(thread);
                await dbContext.SaveChangesAsync();
            }

            var requiredRoles = new[] { "etudiant", "enseignant", "coordinateur", "superadmin" };
            var existingRoleNames = await dbContext.AppRoles
                .Select(r => r.Name.ToLower())
                .ToListAsync();

            var missingRoles = requiredRoles
                .Where(roleName => !existingRoleNames.Contains(roleName))
                .Select(roleName => new Role { Name = roleName })
                .ToList();

            if (missingRoles.Count > 0)
            {
                dbContext.AppRoles.AddRange(missingRoles);
                await dbContext.SaveChangesAsync();
            }

            var roleMap = await dbContext.AppRoles
                .ToDictionaryAsync(r => r.Name.ToLower(), r => r.Id);

            var defaultUsers = new[]
            {
                new { UserName = "superadmin", Email = "superadmin@microlms.local", RoleName = "superadmin" },
                new { UserName = "coordinateur.demo", Email = "coordinateur@microlms.local", RoleName = "coordinateur" },
                new { UserName = "teacher.demo", Email = "teacher@microlms.local", RoleName = "enseignant" },
                new { UserName = "student.demo", Email = "student@microlms.local", RoleName = "etudiant" }
            };

            foreach (var defaultUser in defaultUsers)
            {
                var userExists = await dbContext.AppUsers.AnyAsync(u => u.UserName == defaultUser.UserName || u.Email == defaultUser.Email);
                if (userExists)
                {
                    continue;
                }

                dbContext.AppUsers.Add(new User
                {
                    UserName = defaultUser.UserName,
                    Email = defaultUser.Email,
                    PasswordHash = PasswordSecurity.Hash("Admin@123"),
                    RoleId = roleMap[defaultUser.RoleName]
                });
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Demo data seeding failed at startup. The app will continue running.");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Public}/{id?}");

app.Run();

public partial class Program;