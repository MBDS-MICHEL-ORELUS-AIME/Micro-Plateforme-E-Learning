using E_learningProject.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace E_learningProject.Services;

public class CertificateService : ICertificateService
{
    public string GenerateCertificateNumber(string studentId, int moduleId)
    {
        // Timestamped code keeps certificate references unique and easy to audit.
        var safeStudent = string.IsNullOrWhiteSpace(studentId) ? "UNKNOWN" : studentId.Trim().ToUpperInvariant();
        return $"CERT-{safeStudent}-{moduleId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    public byte[] GenerateCertificatePdf(string studentId, string moduleTitle, string certificateCode, DateTime issueDate)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var safeStudent = string.IsNullOrWhiteSpace(studentId) ? "Apprenant" : studentId.Trim();
        var safeModule = string.IsNullOrWhiteSpace(moduleTitle) ? "Module de formation" : moduleTitle.Trim();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Single-page layout keeps generated certificates lightweight and printable.
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);

                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    column.Item().AlignCenter().Text("Certificat de réussite")
                        .FontSize(34)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken2);

                    column.Item().AlignCenter().Text("Le présent certificat est décerné à")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken2);

                    column.Item().AlignCenter().Text(safeStudent)
                        .FontSize(28)
                        .Bold()
                        .FontColor(Colors.Black);

                    column.Item().AlignCenter().Text("pour avoir terminé avec succès")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken2);

                    column.Item().AlignCenter().Text(safeModule)
                        .FontSize(20)
                        .SemiBold()
                        .FontColor(Colors.Green.Darken2);

                    column.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Text($"Date de délivrance : {issueDate:yyyy-MM-dd}").FontSize(12);
                        row.RelativeItem().AlignRight().Text($"Code du certificat : {certificateCode}").FontSize(12);
                    });

                    column.Item().PaddingTop(25).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    column.Item().AlignCenter().Text("Projet d'apprentissage en ligne")
                        .FontSize(12)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return document.GeneratePdf();
    }
}