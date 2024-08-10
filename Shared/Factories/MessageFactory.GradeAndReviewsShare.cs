using System.Runtime.Serialization;
using System.Security.Cryptography;
using Apachi.Shared.Messages;

namespace Apachi.Shared.Factories;

public partial class MessageFactory
{
    public async Task<GradeAndReviewsShareMessage?> GetGradeAndReviewsMessageBySubmissionKeyAsync(
        byte[] submissionKey,
        List<byte[]> reviewerPublicKeys
    )
    {
        var gradeAndReviewsEntries = await GetEntriesAsync(ProtocolStep.GradeAndReviewsShare);

        foreach (var gradeAndReviewsEntry in gradeAndReviewsEntries)
        {
            try
            {
                var gradeAndReviewsMessage = await GradeAndReviewsShareMessage.DeserializeAsync(
                    gradeAndReviewsEntry.Data,
                    submissionKey,
                    reviewerPublicKeys
                );
                return gradeAndReviewsMessage;
            }
            catch (Exception exception) when (exception is CryptographicException or SerializationException)
            {
                continue;
            }
        }

        return null;
    }
}
