using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Apachi.AvaloniaApp.Data;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;

namespace Apachi.AvaloniaApp.Services;

public class SessionService : ISessionService
{
    private const int HashIterations = 250_000;

    private readonly Func<AppDbContext> _dbContextFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    private Submitter? _submitter;
    private Reviewer? _reviewer;

    public SessionService(Func<AppDbContext> dbContextFactory, IHttpClientFactory httpClientFactory)
    {
        _dbContextFactory = dbContextFactory;
        _httpClientFactory = httpClientFactory;
    }

    public bool IsLoggedIn => (_submitter != null || _reviewer != null) && AesKey != null && HmacKey != null;

    public bool IsReviewer => _reviewer != null;

    public string? Username => _submitter?.Username ?? _reviewer?.Username;

    public Guid? UserId => _submitter?.Id ?? _reviewer?.Id;

    public byte[]? AesKey { get; private set; }

    public byte[]? HmacKey { get; private set; }

    public async Task<bool> LoginAsync(string username, string password, bool isReviewer)
    {
        await using var dbContext = _dbContextFactory();
        Submitter? submitter = null;
        Reviewer? reviewer = null;

        if (isReviewer)
        {
            reviewer = await dbContext.Reviewers.FirstOrDefaultAsync(reviewer => reviewer.Username == username);
        }
        else
        {
            submitter = await dbContext.Submitters.FirstOrDefaultAsync(submitter => submitter.Username == username);
        }

        if (submitter == null && reviewer == null)
        {
            return false;
        }

        var salt = submitter?.PasswordSalt ?? reviewer!.PasswordSalt;
        var userAuthenticationHash = submitter?.AuthenticationHash ?? reviewer!.AuthenticationHash;

        var (aesKey, hmacKey, authenticationHash) = await HashPasswordAsync(password, salt);

        if (!authenticationHash.SequenceEqual(userAuthenticationHash))
        {
            return false;
        }

        _submitter = submitter;
        _reviewer = reviewer;
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
            var submitter = CreateSubmitter(username, salt, authenticationHash);
            dbContext.Submitters.Add(submitter);
        }

        await dbContext.SaveChangesAsync();
        return true;
    }

    public void Logout()
    {
        _submitter = null;
        _reviewer = null;
        AesKey = null;
        HmacKey = null;
    }

    private static Submitter CreateSubmitter(string username, byte[] salt, byte[] authenticationHash)
    {
        var submitter = new Submitter
        {
            Username = username,
            PasswordSalt = salt,
            AuthenticationHash = authenticationHash
        };
        return submitter;
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
        var registerJson = JsonSerializer.Serialize(registerDto);
        var jsonContent = new StringContent(registerJson, Encoding.UTF8, "application/json");

        var httpClient = _httpClientFactory.CreateClient();
        using var response = await httpClient.PostAsync("Reviewer/Register", jsonContent);
        var registeredJson = await response.Content.ReadAsStringAsync();
        var registeredDto = JsonSerializer.Deserialize<ReviewerRegisteredDto>(registeredJson)!;
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
