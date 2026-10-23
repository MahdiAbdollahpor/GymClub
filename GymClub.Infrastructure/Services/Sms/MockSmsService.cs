using GymClub.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace GymClub.Infrastructure.Services.Sms;

public class MockSmsService : ISmsService
{
    private readonly ILogger<MockSmsService> _logger;

    public MockSmsService(ILogger<MockSmsService> logger) => _logger = logger;

    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        _logger.LogWarning("📱 [MOCK SMS] to {Phone}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}