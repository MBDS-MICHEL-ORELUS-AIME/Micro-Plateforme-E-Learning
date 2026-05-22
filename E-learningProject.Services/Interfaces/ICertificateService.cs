namespace E_learningProject.Services.Interfaces;

public interface ICertificateService
{
    string GenerateCertificateNumber(string studentId, int moduleId);
    byte[] GenerateCertificatePdf(string studentId, string moduleTitle, string certificateCode, DateTime issueDate);
}