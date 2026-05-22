using E_learningProject.Core.Entities;
using E_learningProject.Data.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace E_learningProject.Web.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestsDb");
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var module = new Module
            {
                Title = "Integration Module",
                Description = "Module for integration tests",
                Lessons =
                [
                    new Lesson { Title = "Lesson 1", TextContent = "Sample", Order = 1 }
                ]
            };

            db.Modules.Add(module);

            var quiz = new Quiz
            {
                Title = "Integration Quiz",
                PassingScore = 70,
                Questions =
                [
                    new Question
                    {
                        Statement = "2+2?",
                        Type = Core.Enums.QuestionType.MultipleChoice,
                        Options =
                        [
                            new Option { Text = "4", IsCorrect = true },
                            new Option { Text = "5", IsCorrect = false }
                        ]
                    }
                ]
            };

            db.Quizzes.Add(quiz);
            db.SaveChanges();

            module.QuizId = quiz.Id;

            db.Certificates.Add(new Certificate
            {
                StudentId = "student.demo",
                ModuleId = module.Id,
                UniqueCode = "CERT-INT-001",
                IssueDate = DateTime.UtcNow
            });

            db.SaveChanges();
        });
    }
}
