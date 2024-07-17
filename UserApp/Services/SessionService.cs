using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;

namespace Apachi.UserApp.Services;

public class SessionService : ISessionService
{
    private const int HashIterations = 250_000;

    private readonly Func<AppDbContext> _dbContextFactory;
    private readonly IApiService _apiService;

    private User? _user;

    public SessionService(Func<AppDbContext> dbContextFactory, IApiService apiService)
    {
        _dbContextFactory = dbContextFactory;
        _apiService = apiService;
    }

    public bool IsLoggedIn => _user != null && AesKey != null && HmacKey != null;

    public bool IsReviewer => _user is Reviewer;

    public string? Username => _user?.Username;

    public Guid? UserId => _user?.Id;

    public byte[]? AesKey { get; private set; }

    public byte[]? HmacKey { get; private set; }

    public async Task<bool> LoginAsync(string username, string password, bool isReviewer)
    {
        await using var dbContext = _dbContextFactory();
        User? user = isReviewer
            ? await dbContext.Reviewers.FirstOrDefaultAsync(reviewer => reviewer.Username == username)
            : await dbContext.Submitters.FirstOrDefaultAsync(submitter => submitter.Username == username);

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
        await using var dbContext = _dbContextFactory();
        var hasExistingUser = isReviewer
            ? await dbContext.Reviewers.AnyAsync(reviewer => reviewer.Username == username)
            : await dbContext.Submitters.AnyAsync(submitter => submitter.Username == username);

        if (hasExistingUser)
        {
            return false;
        }

        var salt = RandomNumberGenerator.GetBytes(16);
        var (aesKey, hmacKey, authenticationHash) = await HashPasswordAsync(password, salt);

        if (isReviewer)
        {
            var reviewer = await CreateReviewerAsync(username, salt, aesKey, hmacKey, authenticationHash);
            dbContext.Reviewers.Add(reviewer);
        }
        else
        {
            var submitter = new Submitter
            {
                Username = username,
                PasswordSalt = salt,
                AuthenticationHash = authenticationHash
            };
            dbContext.Submitters.Add(submitter);
        }

        await dbContext.SaveChangesAsync();
        return true;
    }

    public void Logout()
    {
        _user = null;
        AesKey = null;
        HmacKey = null;
    }

    private async Task<Reviewer> CreateReviewerAsync(
        string username,
        byte[] salt,
        byte[] aesKey,
        byte[] hmacKey,
        byte[] authenticationHash
    )
    {
        var (publicKey, privateKey) = await KeyUtils.GenerateKeyPairAsync();

        var registerDto = new ReviewerRegisterDto(publicKey);
        var registeredDto = await _apiService.PostAsync<ReviewerRegisterDto, ReviewerRegisteredDto>(
            "Reviewer/Register",
            registerDto
        );

        var sharedKey = await EncryptionUtils.AsymmetricDecryptAsync(registeredDto.EncryptedSharedKey, privateKey);

        var encryptedPrivateKey = await EncryptionUtils.SymmetricEncryptAsync(privateKey, aesKey, hmacKey);
        var encryptedSharedKey = await EncryptionUtils.SymmetricEncryptAsync(sharedKey, aesKey, hmacKey);

        var reviewer = new Reviewer
        {
            Id = registeredDto.ReviewerId,
            Username = username,
            PasswordSalt = salt,
            AuthenticationHash = authenticationHash,
            EncryptedPrivateKey = encryptedPrivateKey,
            EncryptedSharedKey = encryptedSharedKey
        };
        return reviewer;
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
