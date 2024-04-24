using System.Security.Cryptography;
using Apachi.AvaloniaApp.Data;
using Apachi.ViewModels.Auth;
using Microsoft.EntityFrameworkCore;

namespace Apachi.AvaloniaApp.Auth
{
    public class Session : ISession
    {
        private const int HashIterations = 250_000;

        private readonly Func<AppDbContext> _dbContextFactory;
        private User? _user;

        public Session(Func<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public bool IsLoggedIn => _user != null && AesKey != null && HmacKey != null;

        public string? Username => _user?.Username;

        public UserRole? Role => _user?.Role;

        public ReadOnlyMemory<byte>? AesKey { get; private set; }

        public ReadOnlyMemory<byte>? HmacKey { get; private set; }

        public async Task<bool> LoginAsync(string username, string password)
        {
            await using var dbContext = _dbContextFactory();
            var user = await dbContext
                .Users.FirstOrDefaultAsync(user => user.Username == username)
                .ConfigureAwait(false);

            if (user == null)
            {
                return false;
            }

            var (aesKey, hmacKey, authenticationHash) = await Task
                .Factory.StartNew(
                    () => HashPassword(password, user.PasswordSalt),
                    default,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                )
                .ConfigureAwait(false);

            if (!authenticationHash.SequenceEqual(user.AuthenticationHash))
            {
                return false;
            }

            _user = user;
            AesKey = aesKey;
            HmacKey = hmacKey;
            return true;
        }

        public async Task<bool> RegisterAsync(string username, string password, UserRole role)
        {
            await using var dbContext = _dbContextFactory();
            var hasExistingUser = await dbContext
                .Users.AnyAsync(user => user.Username == username)
                .ConfigureAwait(false);

            if (hasExistingUser)
            {
                return false;
            }

            var salt = RandomNumberGenerator.GetBytes(16);
            var (aesKey, hmacKey, authenticationHash) = await Task
                .Factory.StartNew(
                    () => HashPassword(password, salt),
                    default,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                )
                .ConfigureAwait(false);

            var user = new User
            {
                Username = username,
                PasswordSalt = salt,
                AuthenticationHash = authenticationHash,
                Role = role
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

        public void Logout()
        {
            _user = null;
            AesKey = null;
            HmacKey = null;
        }

        private static (byte[] AesKey, byte[] HmacKey, byte[] AuthenticationHash) HashPassword(
            string password,
            byte[] salt
        )
        {
            var derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, HashIterations, HashAlgorithmName.SHA512, 64);
            var aesKey = new byte[32];
            var hmacKey = new byte[32];
            Buffer.BlockCopy(derivedKey, 0, aesKey, 0, 32);
            Buffer.BlockCopy(derivedKey, 32, hmacKey, 0, 32);
            var authenticationHash = SHA256.HashData(derivedKey);
            return (aesKey, hmacKey, authenticationHash);
        }
    }
}
