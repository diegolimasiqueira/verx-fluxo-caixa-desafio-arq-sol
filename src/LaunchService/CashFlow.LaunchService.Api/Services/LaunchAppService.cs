using CashFlow.LaunchService.Api.Data;
using CashFlow.LaunchService.Api.Domain;
using CashFlow.LaunchService.Api.Domain.Events;
using CashFlow.LaunchService.Api.DTOs;
using CashFlow.Observability;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.LaunchService.Api.Services;

public class LaunchAppService(LaunchDbContext db, IPublishEndpoint publisher, ILogger<LaunchAppService> logger)
{
    public async Task<LaunchResponse> RegisterAsync(RegisterLaunchRequest request, CancellationToken ct)
    {
        var type = Enum.Parse<LaunchType>(request.Type, ignoreCase: true);
        var launch = Launch.Create(request.Date, request.Amount, type, request.Description);

        db.Launches.Add(launch);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[business] Launch registered. Id={LaunchId} Date={Date} Type={Type} Amount={Amount}",
            launch.Id, launch.Date, launch.Type, launch.Amount);

        await publisher.Publish(new LaunchRegisteredEvent
        {
            LaunchId = launch.Id,
            Date = launch.Date,
            Amount = launch.Amount,
            Type = launch.Type.ToString().ToLowerInvariant(),
            CreatedAt = launch.CreatedAt
        }, ct);

        logger.LogDebug("[application] LaunchRegistered event published for LaunchId={LaunchId}", launch.Id);

        CashFlowMeters.LaunchRegistrations.Add(1);

        return MapToResponse(launch);
    }

    public async Task<IReadOnlyList<LaunchResponse>> GetByDateAsync(DateOnly date, CancellationToken ct)
    {
        logger.LogDebug("[application] Querying launches for Date={Date}", date);

        var launches = await db.Launches
            .AsNoTracking()
            .Where(l => l.Date == date)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);

        return launches.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<LaunchResponse>> GetByPeriodAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        logger.LogDebug("[application] Querying launches for period From={From} To={To}", from, to);

        var launches = await db.Launches
            .AsNoTracking()
            .Where(l => l.Date >= from && l.Date <= to)
            .OrderBy(l => l.Date).ThenBy(l => l.CreatedAt)
            .ToListAsync(ct);

        return launches.Select(MapToResponse).ToList();
    }

    private static LaunchResponse MapToResponse(Launch l) =>
        new(l.Id, l.Date, l.Amount, l.Type.ToString().ToLowerInvariant(), l.Description, l.CreatedAt);
}
