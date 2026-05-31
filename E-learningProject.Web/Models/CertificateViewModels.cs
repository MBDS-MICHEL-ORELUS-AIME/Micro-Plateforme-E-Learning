namespace E_learningProject.Web.Models;

public class LearnerCertificateItemViewModel
{
    public int ModuleId { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;
    public string? CertificateCode { get; set; }
    public DateTime? IssueDate { get; set; }
    public bool HasCertificate { get; set; }
}

public class LearnerCertificatesViewModel
{
    public string? StudentId { get; set; }
    public List<LearnerCertificateItemViewModel> ExistingCertificates { get; set; } = new();
    public List<LearnerCertificateItemViewModel> AvailableCertificates { get; set; } = new();
}
