using Apachi.Shared;
using Apachi.Shared.Dtos;

namespace Apachi.ViewModels.Reviewer;

public class OpenSubmissionModel
{
    private readonly OpenSubmissionDto _openSubmissionDto;

    public OpenSubmissionModel(OpenSubmissionDto openSubmissionDto)
    {
        _openSubmissionDto = openSubmissionDto;
    }

    public Guid Id => _openSubmissionDto.SubmissionId;

    public SubmissionStatus Status => _openSubmissionDto.SubmissionStatus;

    public DateTimeOffset CreatedDate => _openSubmissionDto.CreatedDate;
}
