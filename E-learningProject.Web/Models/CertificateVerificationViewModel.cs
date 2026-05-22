namespace E_learningProject.Web.Models;

public class CertificateVerificationViewModel
{
    public string? SearchCode { get; set; }
    public bool Searched { get; set; }
    public bool IsValid { get; set; }
    public string? StudentId { get; set; }
    public string? ModuleTitle { get; set; }
    public DateTime? IssueDate { get; set; }
    public string? CertificateCode { get; set; }
}
