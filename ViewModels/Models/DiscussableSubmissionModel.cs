namespace Apachi.ViewModels.Models;

public record DiscussableSubmissionModel(byte[] PaperHash, List<DiscussReviewModel> Reviews, DateTime CreatedDate);
