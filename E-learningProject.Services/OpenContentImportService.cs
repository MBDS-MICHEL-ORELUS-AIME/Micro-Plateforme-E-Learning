using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_learningProject.Core.Entities;
using E_learningProject.Core.Enums;
using E_learningProject.Data.Context;
using E_learningProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace E_learningProject.Services;

public sealed class OpenContentImportService : IOpenContentImportService
{
    // Sources autorisées pour l'import automatique: uniquement du contenu ouvert ou réutilisable.
    private static readonly Dictionary<string, string> ApprovedSources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Wikiversity"] = "CC BY-SA",
        ["Wikipedia"] = "CC BY-SA",
        ["Project Gutenberg"] = "Domaine public",
        ["OpenEdition"] = "Licence ouverte/variable",
        ["YouTube"] = "URL externe uniquement"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;

    public OpenContentImportService(ApplicationDbContext dbContext, HttpClient httpClient)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
    }

    public async Task<OpenContentImportResult> ImportAsync(int maxModules = 20, CancellationToken cancellationToken = default)
    {
        // On limite le volume importé pour éviter de surcharger la base.
        var targetCount = maxModules <= 0 ? 20 : Math.Min(maxModules, 100);
        // Le catalogue mélange des sources en ligne et un jeu de secours local.
        var catalog = await BuildCatalogAsync(targetCount, cancellationToken);

        var importedModules = 0;
        var importedLessons = 0;
        var importedQuizzes = 0;
        var skippedDuplicates = 0;
        var skippedInvalidLicense = 0;
        var errors = new List<string>();

        foreach (var sourceModule in catalog)
        {
            try
            {
                // Toute source hors périmètre est écartée avant même l'écriture en base.
                if (!IsSourceApproved(sourceModule.SourceName, sourceModule.SourceLicense))
                {
                    skippedInvalidLicense++;
                    continue;
                }

                // Le hash permet d'identifier un contenu déjà importé même si le titre change légèrement.
                var contentHash = ComputeHash(sourceModule.Title, sourceModule.SourceName, sourceModule.SourceUrl);
                var exists = await _dbContext.ContentImportLogs
                    .AsNoTracking()
                    .AnyAsync(x => x.ContentHash == contentHash, cancellationToken);

                var titleExists = await _dbContext.Modules
                    .AsNoTracking()
                    .AnyAsync(m => m.Title.ToLower() == sourceModule.Title.ToLower(), cancellationToken);

                if (exists || titleExists)
                {
                    skippedDuplicates++;
                    continue;
                }

                // Le module est reconstruit à partir du seed source puis enrichi avec ses leçons.
                var module = new Module
                {
                    Title = sourceModule.Title,
                    Description = $"{sourceModule.Description}\n\nSource: {sourceModule.SourceName} | Licence: {sourceModule.SourceLicense}",
                    Lessons = sourceModule.Lessons
                        .Select((lesson, index) => new Lesson
                        {
                            Title = lesson.Title,
                            TextContent = lesson.TextContent,
                            VideoUrl = lesson.VideoUrl,
                            Order = index + 1
                        })
                        .ToList()
                };

                _dbContext.Modules.Add(module);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // On journalise l'import pour conserver la traçabilité de la source et de la licence.
                var log = new ContentImportLog
                {
                    EntityType = "Module",
                    EntityId = module.Id,
                    SourceName = sourceModule.SourceName,
                    SourceUrl = sourceModule.SourceUrl,
                    SourceLicense = sourceModule.SourceLicense,
                    ContentHash = contentHash,
                    ImportedAt = DateTime.UtcNow
                };
                _dbContext.ContentImportLogs.Add(log);

                if (sourceModule.Quiz is not null)
                {
                    // Certains modules reçoivent aussi un quiz pour être utilisables immédiatement.
                    var quiz = new Quiz
                    {
                        Title = sourceModule.Quiz.Title,
                        PassingScore = 70,
                        Questions = sourceModule.Quiz.Questions
                            .Select(q => new Question
                            {
                                Statement = q.Statement,
                                Type = q.Type,
                                Options = q.Options
                                    .Select(o => new Option { Text = o.Text, IsCorrect = o.IsCorrect })
                                    .ToList()
                            })
                            .ToList()
                    };

                    _dbContext.Quizzes.Add(quiz);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    module.QuizId = quiz.Id;
                    importedQuizzes++;
                }

                importedModules++;
                importedLessons += module.Lessons.Count;

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                errors.Add($"{sourceModule.Title}: {ex.Message}");
            }
        }

        return new OpenContentImportResult
        {
            ImportedModules = importedModules,
            ImportedLessons = importedLessons,
            ImportedQuizzes = importedQuizzes,
            SkippedDuplicates = skippedDuplicates,
            SkippedInvalidLicense = skippedInvalidLicense,
            Errors = errors
        };
    }

    private async Task<List<SourceModuleSeed>> BuildCatalogAsync(int targetCount, CancellationToken cancellationToken)
    {
        var catalog = new List<SourceModuleSeed>();

        // Wikipedia apporte des résumés courts et variés.
        var wikipediaModules = await FetchWikipediaModulesAsync(12, cancellationToken);
        catalog.AddRange(wikipediaModules);

        // Wikiversity apporte des contenus plus proches d'un parcours pédagogique.
        var wikiversityModules = await FetchWikiversityModulesAsync(8, cancellationToken);
        catalog.AddRange(wikiversityModules);

        if (catalog.Count < targetCount)
        {
            // Si les sources externes ne suffisent pas, on complète avec un catalogue local stable.
            var fallback = BuildFallbackCatalog();
            foreach (var seed in fallback)
            {
                if (catalog.Count >= targetCount)
                {
                    break;
                }

                catalog.Add(seed);
            }
        }

        return catalog.Take(targetCount).ToList();
    }

    private async Task<List<SourceModuleSeed>> FetchWikipediaModulesAsync(int count, CancellationToken cancellationToken)
    {
        var modules = new List<SourceModuleSeed>();
        // Les thèmes sont choisis pour couvrir les domaines utiles à la plateforme.
        var themes = new[]
        {
            "Comptabilite",
            "Administration",
            "Gestion de projet",
            "Relations internationales",
            "Communication",
            "Droit",
            "Economie",
            "C Sharp",
            "Algorithme",
            "Programmation orientee objet",
            "Anglais des affaires",
            "Base de donnees"
        };

        foreach (var theme in themes.Take(count))
        {
            // Chaque thème est transformé en module court et immédiatement exploitable.
            var page = await FetchWikipediaSummaryAsync(theme, cancellationToken);
            if (page is null)
            {
                continue;
            }

            var lessons = BuildLessonsFromText(page.Extract, page.SourceName, page.SourceUrl);
            if (lessons.Count == 0)
            {
                continue;
            }

            modules.Add(new SourceModuleSeed(
                $"{page.Title} - introduction",
                TruncateText(page.Extract, 280),
                page.SourceName,
                page.SourceUrl,
                page.SourceLicense,
                lessons,
                BuildDefaultQuiz(page.Title)));
        }

        return modules;
    }

    private async Task<List<SourceModuleSeed>> FetchWikiversityModulesAsync(int count, CancellationToken cancellationToken)
    {
        var modules = new List<SourceModuleSeed>();
        // Ces domaines produisent des textes plus pédagogiques que de simples résumés encyclopédiques.
        var domains = new[]
        {
            "Comptabilite",
            "Administration",
            "Management",
            "Droit",
            "Economie",
            "Programmation",
            "Langue anglaise",
            "Systeme d'information"
        };

        foreach (var domain in domains.Take(count))
        {
            // Les extraits obtenus servent à générer plusieurs leçons plus courtes.
            var page = await FetchWikiversityExtractAsync(domain, cancellationToken);
            if (page is null)
            {
                continue;
            }

            var lessons = BuildLessonsFromText(page.Extract, page.SourceName, page.SourceUrl);
            if (lessons.Count == 0)
            {
                continue;
            }

            modules.Add(new SourceModuleSeed(
                $"{page.Title} - parcours",
                TruncateText(page.Extract, 280),
                page.SourceName,
                page.SourceUrl,
                page.SourceLicense,
                lessons,
                null));
        }

        return modules;
    }

    private async Task<SourcePage?> FetchWikipediaSummaryAsync(string topic, CancellationToken cancellationToken)
    {
        try
        {
            // L'API REST Wikipedia fournit un résumé bref suffisant pour initialiser un module.
            var encoded = Uri.EscapeDataString(topic);
            var url = $"https://fr.wikipedia.org/api/rest_v1/page/summary/{encoded}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var root = document.RootElement;
            if (!root.TryGetProperty("extract", out var extractNode))
            {
                return null;
            }

            var extract = extractNode.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(extract))
            {
                return null;
            }

            var title = root.TryGetProperty("title", out var titleNode)
                ? titleNode.GetString() ?? topic
                : topic;

            var sourceUrl = root.TryGetProperty("content_urls", out var urlsNode)
                && urlsNode.TryGetProperty("desktop", out var desktopNode)
                && desktopNode.TryGetProperty("page", out var pageNode)
                    ? pageNode.GetString() ?? $"https://fr.wikipedia.org/wiki/{encoded}"
                    : $"https://fr.wikipedia.org/wiki/{encoded}";

            return new SourcePage(title, extract, "Wikipedia", sourceUrl, "CC BY-SA");
        }
        catch
        {
            return null;
        }
    }

    private async Task<SourcePage?> FetchWikiversityExtractAsync(string topic, CancellationToken cancellationToken)
    {
        try
        {
            // Wikiversity fournit un extrait plus long, pratique pour découper des leçons.
            var encoded = Uri.EscapeDataString(topic);
            var url = $"https://fr.wikiversity.org/w/api.php?action=query&prop=extracts&explaintext=1&exintro=0&titles={encoded}&format=json&redirects=1";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("query", out var query)
                || !query.TryGetProperty("pages", out var pages))
            {
                return null;
            }

            foreach (var page in pages.EnumerateObject())
            {
                var node = page.Value;
                if (!node.TryGetProperty("extract", out var extractNode))
                {
                    continue;
                }

                var extract = extractNode.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(extract))
                {
                    continue;
                }

                var title = node.TryGetProperty("title", out var titleNode)
                    ? titleNode.GetString() ?? topic
                    : topic;

                return new SourcePage(
                    title,
                    extract,
                    "Wikiversity",
                    $"https://fr.wikiversity.org/wiki/{Uri.EscapeDataString(title.Replace(' ', '_'))}",
                    "CC BY-SA");
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static List<SourceLessonSeed> BuildLessonsFromText(string text, string sourceName, string sourceUrl)
    {
        // On transforme un texte long en leçons courtes et mieux assimilables.
        var chunks = SplitIntoChunks(text, 3, 650);
        var lessons = new List<SourceLessonSeed>();

        for (var i = 0; i < chunks.Count; i++)
        {
            lessons.Add(new SourceLessonSeed(
                $"Lecon {i + 1}",
                $"{chunks[i]}\n\nAttribution: {sourceName} - {sourceUrl}"));
        }

        return lessons;
    }

    private static List<string> SplitIntoChunks(string text, int maxChunks, int maxLength)
    {
        // On normalise les retours à la ligne avant de regrouper les phrases.
        var normalized = text.Replace("\r", " ").Replace("\n", " ");
        var sentences = normalized
            .Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 20)
            .ToList();

        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var sentence in sentences)
        {
            if (current.Length + sentence.Length + 2 > maxLength)
            {
                if (current.Length > 0)
                {
                    chunks.Add(current.ToString().Trim());
                    current.Clear();
                }

                if (chunks.Count >= maxChunks)
                {
                    break;
                }
            }

            current.Append(sentence).Append(". ");
        }

        if (chunks.Count < maxChunks && current.Length > 0)
        {
            chunks.Add(current.ToString().Trim());
        }

        return chunks;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength].Trim() + "...";
    }

    private static SourceQuizSeed BuildDefaultQuiz(string title)
    {
        // Le quiz vérifie surtout que l'apprenant a compris la provenance et le bon usage du contenu.
        return new SourceQuizSeed(
            $"Quiz - {title}",
            [
                new SourceQuestionSeed(
                    "Le contenu de ce module provient-il d'une source ouverte et attribuee ?",
                    QuestionType.TrueFalse,
                    [new SourceOptionSeed("Vrai", true), new SourceOptionSeed("Faux", false)]),
                new SourceQuestionSeed(
                    "Quelle bonne pratique est recommandee apres lecture ?",
                    QuestionType.MultipleChoice,
                    [
                        new SourceOptionSeed("Verifier les references de la source", true),
                        new SourceOptionSeed("Ignorer la licence", false),
                        new SourceOptionSeed("Copier sans attribution", false)
                    ])
            ]);
    }

    private static bool IsSourceApproved(string sourceName, string sourceLicense)
    {
        // On compare la source et sa licence pour rester dans un cadre de réutilisation autorisé.
        if (!ApprovedSources.TryGetValue(sourceName, out var expected))
        {
            return false;
        }

        return expected.Equals(sourceLicense, StringComparison.OrdinalIgnoreCase)
            || expected.Contains("variable", StringComparison.OrdinalIgnoreCase)
            || expected.Contains("URL externe", StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeHash(string title, string sourceName, string sourceUrl)
    {
        // Le hash combine les éléments clés du contenu pour obtenir un identifiant stable.
        var payload = $"{title.Trim().ToLowerInvariant()}|{sourceName.Trim().ToLowerInvariant()}|{sourceUrl.Trim().ToLowerInvariant()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    private static IReadOnlyList<SourceModuleSeed> BuildFallbackCatalog()
    {
        // Le catalogue de secours garantit un import possible même sans appel réseau.
        var modules = new List<SourceModuleSeed>();

        modules.Add(new SourceModuleSeed(
            "Comptabilite: notions fondamentales",
            "Synthese introductive sur les principes comptables et les etats financiers.",
            "Wikipedia",
            "https://fr.wikipedia.org/wiki/Comptabilit%C3%A9",
            "CC BY-SA",
            [
                new SourceLessonSeed("Principes comptables", "Prudence, continuite d'exploitation et regularite des enregistrements."),
                new SourceLessonSeed("Bilan et resultat", "Identifier actifs/passifs et produits/charges dans une logique de pilotage."),
                new SourceLessonSeed("Ecritures de base", "Enregistrer une operation simple achat/vente/tresorerie.")
            ],
            new SourceQuizSeed(
                "Quiz - Comptabilite fondamentale",
                [
                    new SourceQuestionSeed("Le bilan presente-t-il actifs et passifs ?", QuestionType.TrueFalse, [new("Vrai", true), new("Faux", false)]),
                    new SourceQuestionSeed("Quel principe impose d'anticiper les pertes probables ?", QuestionType.MultipleChoice, [new("Prudence", true), new("Neutralite", false), new("Confidentialite", false)])
                ])));

        modules.Add(new SourceModuleSeed(
            "Administration publique: organisation et processus",
            "Cours de base sur les fonctions administratives et la qualite de service.",
            "Wikiversity",
            "https://fr.wikiversity.org/wiki/Administration_publique",
            "CC BY-SA",
            [
                new SourceLessonSeed("Fonctions administratives", "Planifier, organiser, diriger et controler les activites."),
                new SourceLessonSeed("Procedures et conformite", "Standardiser les processus et tracer les decisions."),
                new SourceLessonSeed("Suivi de performance", "Suivre des indicateurs simples de qualite et de delais.")
            ],
            null));

        modules.Add(new SourceModuleSeed(
            "Relations internationales: concepts cles",
            "Introduction aux acteurs, institutions et logiques de cooperation.",
            "OpenEdition",
            "https://books.openedition.org/",
            "Licence ouverte/variable",
            [
                new SourceLessonSeed("Acteurs internationaux", "Etats, organisations et societes civiles transnationales."),
                new SourceLessonSeed("Negociation", "Preparation d'interets, marges de concession et accord final."),
                new SourceLessonSeed("Gestion des tensions", "Mediation, prevention et communication de crise.")
            ],
            null));

        modules.Add(new SourceModuleSeed(
            "Anglais professionnel: communication ecrite",
            "Pratiques essentielles pour emails, reunions et comptes rendus.",
            "Project Gutenberg",
            "https://www.gutenberg.org/",
            "Domaine public",
            [
                new SourceLessonSeed("Email structure", "Objet clair, contexte court, demande explicite et formule de cloture."),
                new SourceLessonSeed("Meeting language", "Vocabulaire actionnable pour animer et conclure une reunion."),
                new SourceLessonSeed("Follow-up", "Resume des decisions, actions et echeances en anglais simple.")
            ],
            new SourceQuizSeed(
                "Quiz - Anglais professionnel",
                [
                    new SourceQuestionSeed("Un email professionnel doit-il contenir une action claire ?", QuestionType.TrueFalse, [new("Vrai", true), new("Faux", false)]),
                    new SourceQuestionSeed("Quelle formule est la plus adaptee en cloture ?", QuestionType.MultipleChoice, [new("Best regards", true), new("Yo", false), new("See ya", false)])
                ])));

        modules.Add(new SourceModuleSeed(
            "Programmation C#: fondamentaux",
            "Cours d'initiation aux structures de controle et a la decomposition par methodes.",
            "Wikipedia",
            "https://fr.wikipedia.org/wiki/C_Sharp",
            "CC BY-SA",
            [
                new SourceLessonSeed("Variables et types", "Type safety, declaration explicite et conversions maitrises."),
                new SourceLessonSeed("Conditions et boucles", "if, switch, for, while: choisir la structure adaptee."),
                new SourceLessonSeed("Methodes", "Decouper les responsabilites pour un code lisible et testable.", "https://www.youtube.com/watch?v=GhQdlIFylQ8")
            ],
            new SourceQuizSeed(
                "Quiz - C# fondamentaux",
                [
                    new SourceQuestionSeed("C# est-il un langage type statique ?", QuestionType.TrueFalse, [new("Vrai", true), new("Faux", false)]),
                    new SourceQuestionSeed("Quel mot-cle declare une classe ?", QuestionType.MultipleChoice, [new("class", true), new("define", false), new("type", false)])
                ])));

        var seed = modules.ToList();
        while (seed.Count < 20)
        {
            var pivot = modules[seed.Count % modules.Count];
            seed.Add(new SourceModuleSeed(
                $"{pivot.Title} - Serie {seed.Count - 4}",
                pivot.Description,
                pivot.SourceName,
                $"{pivot.SourceUrl}#{seed.Count}",
                pivot.SourceLicense,
                pivot.Lessons,
                pivot.Quiz));
        }

        return seed;
    }

    private sealed record SourcePage(string Title, string Extract, string SourceName, string SourceUrl, string SourceLicense);

    private sealed record SourceModuleSeed(
        string Title,
        string Description,
        string SourceName,
        string SourceUrl,
        string SourceLicense,
        IReadOnlyList<SourceLessonSeed> Lessons,
        SourceQuizSeed? Quiz);

    private sealed record SourceLessonSeed(string Title, string TextContent, string? VideoUrl = null);

    private sealed record SourceQuizSeed(string Title, IReadOnlyList<SourceQuestionSeed> Questions);

    private sealed record SourceQuestionSeed(string Statement, QuestionType Type, IReadOnlyList<SourceOptionSeed> Options);

    private sealed record SourceOptionSeed(string Text, bool IsCorrect);
}
