using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;

namespace Apachi.AvaloniaApp.Services;

public class ReviewerService : IReviewerService
{
    private readonly ISessionService _sessionService;
    private readonly IApiService _apiService;

    public ReviewerService(ISessionService sessionService, IApiService apiService)
    {
        _sessionService = sessionService;
        _apiService = apiService;
    }

    public async Task<List<ReviewableSubmissionDto>> GetReviewableSubmissionsAsync()
    {
        var reviewerId = _sessionService.UserId!.Value;
        var queryParameters = new Dictionary<string, string>() { { "reviewerId", reviewerId.ToString() } };
        var reviewableSubmissionDtos = await _apiService.GetAsync<List<ReviewableSubmissionDto>>(
            "Reviewer/GetReviewableSubmissions",
            queryParameters
        );
        return reviewableSubmissionDtos;
    }
}
