namespace Apachi.AvaloniaApp.Data;

public class Review
{
    public Guid Id { get; set; }

    public byte[]? EncryptedSavedAssessment { get; set; }

    public Guid ReviewerId { get; set; }

    public Reviewer Reviewer { get; set; } = null!;

    public Guid SubmissionId { get; set; }
}
