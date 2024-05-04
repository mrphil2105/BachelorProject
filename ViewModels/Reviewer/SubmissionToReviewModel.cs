using Apachi.Shared;
using Apachi.Shared.Dtos;

namespace Apachi.ViewModels.Reviewer;

public class SubmissionToReviewModel
{
    private readonly SubmissionToReviewDto _submissionToReviewDto;

    public SubmissionToReviewModel(SubmissionToReviewDto submissionToReviewDto)
    {
        _submissionToReviewDto = submissionToReviewDto;
    }

    public Guid Id => _submissionToReviewDto.SubmissionId;

    public SubmissionStatus Status => _submissionToReviewDto.SubmissionStatus;

    public DateTimeOffset CreatedDate => _submissionToReviewDto.CreatedDate;
}
