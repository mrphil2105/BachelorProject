using Apachi.Shared.Dtos;

namespace Apachi.ViewModels.Services;

public interface IReviewerService
{
    Task<List<ReviewableSubmissionDto>> GetReviewableSubmissionsAsync();
}
