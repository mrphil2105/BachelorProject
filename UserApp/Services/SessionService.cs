using System.Security.Cryptography;
using Apachi.Shared.Data;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;
using AppReviewer = Apachi.UserApp.Data.Reviewer;
using AppSubmitter = Apachi.UserApp.Data.Submitter;
using LogReviewer = Apachi.Shared.Data.Reviewer;
using LogSubmitter = Apachi.Shared.Data.Submitter;

namespace Apachi.UserApp.Services;

public class SessionService : ISessionService
{
    private const int HashIterations = 250_000;

    private readonly Func<AppDbContext> _appDbContextFactory;
    private readonly Func<LogDbContext> _logDbContextFactory;

    private User? _user;

    public SessionService(Func<AppDbContext> appDbContextFactory, Func<LogDbContext> logDbContextFactory)
    {
        _appDbContextFactory = appDbContextFactory;
        _logDbContextFactory = logDbContextFactory;
    }

    public bool IsLoggedIn => _user != null && AesKey != null && HmacKey != null;

    public bool IsReviewer => _user is AppReviewer;

    public string? Username => _user?.Username;

    public Guid? UserId => _user?.Id;

    public byte[]? AesKey { get; private set; }

    public byte[]? HmacKey { get; private set; }

    public async Task<bool> LoginAsync(string username, string password, bool isReviewer)
    {
        await using var appDbContext = _appDbContextFactory();
        User? user = isReviewer
            ? await appDbContext.Reviewers.FirstOrDefaultAsync(reviewer => reviewer.Username == username)
            : await appDbContext.Submitters.FirstOrDefaultAsync(submitter => submitter.Username == username);

        if (user == null)
        {
            return false;
        }

        var (aesKey, hmacKey, authenticationHash) = await HashPasswordAsync(password, user.PasswordSalt);

        if (!authenticationHash.SequenceEqual(user.AuthenticationHash))
        {
            return false;
        }

        _user = user;
        AesKey = aesKey;
        HmacKey = hmacKey;
        return true;
    }

    public async Task<bool> RegisterAsync(string username, string password, bool isReviewer)
    {
        await using var appDbContext = _appDbContextFactory();
        var hasExistingUser = isReviewer
            ? await appDbContext.Reviewers.AnyAsync(reviewer => reviewer.Username == username)
            : await appDbContext.Submitters.AnyAsync(submitter => submitter.Username == username);

        if (hasExistingUser)
        {
            return false;
        }

        var salt = RandomNumberGenerator.GetBytes(16);
        var (aesKey, hmacKey, authenticationHash) = await HashPasswordAsync(password, salt);

        if (isReviewer)
        {
            var reviewer = await CreateReviewerAsync(username, salt, aesKey, hmacKey, authenticationHash);
            appDbContext.Reviewers.Add(reviewer);
        }
        else
        {
            var submitter = await CreateSubmitterAsync(username, salt, authenticationHash);
            appDbContext.Submitters.Add(submitter);
        }

        await appDbContext.SaveChangesAsync();
        return true;
    }

    public void Logout()
    {
        _user = null;
        AesKey = null;
        HmacKey = null;
    }

    private async Task<AppSubmitter> CreateSubmitterAsync(string username, byte[] salt, byte[] authenticationHash)
    {
        await using var logDbContext = _logDbContextFactory();
        var logSubmitter = new LogSubmitter();
        logDbContext.Submitters.Add(logSubmitter);
        await logDbContext.SaveChangesAsync();

        var appSubmitter = new AppSubmitter
        {
            Id = logSubmitter.Id,
            Username = username,
            PasswordSalt = salt,
            AuthenticationHash = authenticationHash
        };
        return appSubmitter;
    }

    private async Task<AppReviewer> CreateReviewerAsync(
        string username,
        byte[] salt,
        byte[] aesKey,
        byte[] hmacKey,
        byte[] authenticationHash
    )
    {
        var (reviewerPrivateKey, reviewerPublicKey) = await GenerateKeyPairAsync();
        var sharedKey = RandomNumberGenerator.GetBytes(32);

        var pcPublicKey = GetPCPublicKey();
        var pcEncryptedSharedKey = await AsymmetricEncryptAsync(sharedKey, pcPublicKey);
        var sharedKeySignature = await CalculateSignatureAsync(sharedKey, reviewerPrivateKey);

        await using var logDbContext = _logDbContextFactory();
        var logReviewer = new LogReviewer
        {
            PublicKey = reviewerPublicKey,
            EncryptedSharedKey = pcEncryptedSharedKey,
            SharedKeySignature = sharedKeySignature
        };
        logDbContext.Reviewers.Add(logReviewer);
        await logDbContext.SaveChangesAsync();

        var encryptedPrivateKey = await SymmetricEncryptAndMacAsync(reviewerPrivateKey, aesKey, hmacKey);
        var encryptedSharedKey = await SymmetricEncryptAndMacAsync(sharedKey, aesKey, hmacKey);

        var appReviewer = new AppReviewer
        {
            Id = logReviewer.Id,
            Username = username,
            PasswordSalt = salt,
            AuthenticationHash = authenticationHash,
            EncryptedPrivateKey = encryptedPrivateKey,
            EncryptedSharedKey = encryptedSharedKey
        };
        return appReviewer;
    }

    private static async Task<(byte[] AesKey, byte[] HmacKey, byte[] AuthenticationHash)> HashPasswordAsync(
        string password,
        byte[] salt
    )
    {
        var derivedKey = await Task.Factory.StartNew(
            () => Rfc2898DeriveBytes.Pbkdf2(password, salt, HashIterations, HashAlgorithmName.SHA512, 64),
            default,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
        var aesKey = new byte[32];
        var hmacKey = new byte[32];
        Buffer.BlockCopy(derivedKey, 0, aesKey, 0, 32);
        Buffer.BlockCopy(derivedKey, 32, hmacKey, 0, 32);
        var authenticationHash = SHA256.HashData(derivedKey);
        return (aesKey, hmacKey, authenticationHash);
    }
}
