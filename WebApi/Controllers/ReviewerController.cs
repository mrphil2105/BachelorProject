using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.WebApi.Data;
using Microsoft.AspNetCore.Mvc;

namespace Apachi.WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class ReviewerController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ReviewerController> _logger;

    public ReviewerController(IConfiguration configuration, AppDbContext dbContext, ILogger<ReviewerController> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ReviewerRegisteredDto> Register([FromBody] ReviewerRegisterDto registerDto)
    {
        var programCommitteePublicKey = KeyUtils.GetProgramCommitteePublicKey();
        var sharedKey = RandomNumberGenerator.GetBytes(32);

        var programCommitteeEncryptedSharedKey = await EncryptionUtils.AsymmetricEncryptAsync(
            sharedKey,
            programCommitteePublicKey
        );
        var reviewer = new Reviewer
        {
            ReviewerPublicKey = registerDto.ReviewerPublicKey,
            EncryptedSharedKey = programCommitteeEncryptedSharedKey
        };

        _dbContext.Reviewers.Add(reviewer);
        await _dbContext.SaveChangesAsync();

        var reviewerEncryptedSharedKey = await EncryptionUtils.AsymmetricEncryptAsync(
            sharedKey,
            registerDto.ReviewerPublicKey
        );
        var registeredDto = new ReviewerRegisteredDto(reviewer.Id, reviewerEncryptedSharedKey);

        _logger.LogInformation("Reviewer registered new account with id: {Id}", reviewer.Id);
        return registeredDto;
    }
}
