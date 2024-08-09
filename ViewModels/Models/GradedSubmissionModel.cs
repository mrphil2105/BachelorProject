namespace Apachi.ViewModels.Models;

public record GradedSubmissionModel(Guid SubmissionId, byte[] PaperHash, int Grade, List<ReviewModel> Reviews);
