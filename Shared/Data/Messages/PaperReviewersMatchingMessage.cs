namespace Apachi.Shared.Data.Messages;

public record PaperReviewersMatchingMessage(
    byte[] ReviewCommitment,
    byte[] ReviewerPublicKeys,
    byte[] ReviewNonce,
    byte[] MatchingSignature, // Signed by K_PC^-1
    byte[] SubmissionReviewProof // Links C(P, r_s) and C(P, r_r)
) : IMessage;
