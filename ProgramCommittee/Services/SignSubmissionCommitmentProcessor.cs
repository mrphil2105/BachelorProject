using Apachi.Shared.Data;
using Apachi.Shared.Data.Messages;

namespace Apachi.ProgramCommittee.Services;

public class SignSubmissionCommitmentProcessor
    : MessageProcessor<SubmissionIdentityCommitmentsMessage, SubmissionCommitmentSignatureMessage>
{
    public SignSubmissionCommitmentProcessor(LogDbContext logDbContext)
        : base(logDbContext) { }

    public override async Task<SubmissionCommitmentSignatureMessage> ProcessMessageAsync(
        SubmissionIdentityCommitmentsMessage message,
        CancellationToken cancellationToken
    )
    {
        var pcPrivateKey = GetPCPrivateKey();
        var submissionCommitmentSignature = await CalculateSignatureAsync(message.SubmissionCommitment, pcPrivateKey);

        var responseMessage = new SubmissionCommitmentSignatureMessage(submissionCommitmentSignature);
        return responseMessage;
    }
}
