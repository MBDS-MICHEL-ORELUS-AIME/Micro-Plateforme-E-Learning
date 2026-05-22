using E_learningProject.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace E_learningProject.Services;

public class CertificateService : ICertificateService
{
    public string GenerateCertificateNumber(string studentId, int moduleId)
    {
        var safeStudent = string.IsNullOrWhiteSpace(studentId) ? "UNKNOWN" : studentId.Trim().ToUpperInvariant();
        return $"CERT-{safeStudent}-{moduleId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    public byte[] GenerateCertificatePdf(string studentId, string moduleTitle, string certificateCode, DateTime issueDate)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var safeStudent = string.IsNullOrWhiteSpace(studentId) ? "Student" : studentId.Trim();
        var safeModule = string.IsNullOrWhiteSpace(moduleTitle) ? "Training Module" : moduleTitle.Trim();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);

                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    column.Item().AlignCenter().Text("Certificate of Completion")
                        .FontSize(34)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken2);

                    column.Item().AlignCenter().Text("This certificate is proudly awarded to")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken2);

                    column.Item().AlignCenter().Text(safeStudent)
                        .FontSize(28)
                        .Bold()
                        .FontColor(Colors.Black);

                    column.Item().AlignCenter().Text("for successfully completing")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken2);

                    column.Item().AlignCenter().Text(safeModule)
                        .FontSize(20)
                        .SemiBold()
                        .FontColor(Colors.Green.Darken2);

                    column.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Text($"Issue Date: {issueDate:yyyy-MM-dd}").FontSize(12);
                        row.RelativeItem().AlignRight().Text($"Certificate Code: {certificateCode}").FontSize(12);
                    });

                    column.Item().PaddingTop(25).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    column.Item().AlignCenter().Text("E-learning Project")
                        .FontSize(12)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return document.GeneratePdf();
    }
}