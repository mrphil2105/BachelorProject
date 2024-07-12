using System.Security.Cryptography;
using System.Text;
using Apachi.Shared.Dtos;
using Apachi.WebApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]

public class AppendOnlyLogController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;

    public AppendOnlyLogController(IConfiguration configuration, AppDbContext dbContext)
    {
        _configuration = _configuration;
        _dbContext = dbContext;
    }

    // TODO: Add verification of hash chain
    [HttpPost]
    public async Task<ResultDto> AddLogEntry([FromBody] LogEntryDto logEntryDto)
    {
        try
        {
            var previousEntry =
                await _dbContext.LogEntries.OrderByDescending(log => log.Timestamp).FirstOrDefaultAsync();
            var previousHash = previousEntry?.CurrentHash ?? new byte[32];

            var logEntry = new LogEntry
            {
                Id = Guid.NewGuid(),
                Message = logEntryDto.Message,
                PreviousHash = previousHash,
                CurrentHash = ComputeHash(logEntryDto, previousHash),
                UserPublicKey = logEntryDto.UserPublicKey
            };

            _dbContext.LogEntries.Add(logEntry);
            await _dbContext.SaveChangesAsync();

            return new ResultDto(true, "Log entry added successfully.");
        }
        catch (Exception e)
        {
            return new ResultDto(false, $"Error occured when adding log entry: {e.Message}");
        }
    }

    [HttpGet]
    public async Task<List<LogEntryDto>> GetAllLogEntriesAsync()
    {
        var logEntryDtos = await _dbContext.LogEntries
            .OrderBy(log => log.Timestamp)
            .Select(log => new LogEntryDto(log.Message, log.Timestamp, log.UserPublicKey))
            .ToListAsync();

        return logEntryDtos;
    }

    private byte[] ComputeHash(LogEntryDto logEntryDto, byte[] previousHash)
    {
        var rawData = Encoding.UTF8.GetBytes($"{logEntryDto.Timestamp}{logEntryDto.Message}");
        var combinedData = new byte[rawData.Length + previousHash.Length];
        
        Buffer.BlockCopy(previousHash, 0, combinedData, 0, previousHash.Length);
        Buffer.BlockCopy(rawData, 0, combinedData, previousHash.Length, rawData.Length);

        return SHA256.HashData(combinedData);
    }
}