public record SubmissionModel(
    ReadOnlyMemory<byte> EncryptedPaper,
    ReadOnlyMemory<byte> RandomnessSubmitter,
    ReadOnlyMemory<byte> RandomnessReviewer,
    ReadOnlyMemory<byte> EncryptedSubmissionKey,
    ReadOnlyMemory<byte> SubmissionSignature
);
