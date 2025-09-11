using dengue.watch.api.infrastructure.database;
using dengue.watch.api.common.exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Facet.Extensions;

namespace dengue.watch.api.features.denguealerts;

/// <summary>
/// Service for managing dengue alerts
/// </summary>
public interface IDengueAlertService
{
    Task<DengueAlertDto> CreateAlertAsync(CreateDengueAlertRequest request, CancellationToken cancellationToken = default);
    Task<DengueAlertDto> GetAlertByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<DengueAlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DengueAlertDto>> GetAlertsByLocationAsync(string location, CancellationToken cancellationToken = default);
    Task<DengueAlertDto> UpdateAlertAsync(int id, UpdateDengueAlertRequest request, CancellationToken cancellationToken = default);
    Task<bool> ResolveAlertAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAlertAsync(int id, CancellationToken cancellationToken = default);
}

public class DengueAlertService : IDengueAlertService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<DengueAlertHub> _hubContext;
    private readonly ILogger<DengueAlertService> _logger;

    public DengueAlertService(
        ApplicationDbContext context,
        IHubContext<DengueAlertHub> hubContext,
        ILogger<DengueAlertService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<DengueAlertDto> CreateAlertAsync(CreateDengueAlertRequest request, CancellationToken cancellationToken = default)
    {
        var alert = new DengueAlert
        {
            Location = request.Location,
            Description = request.Description,
            Level = request.Level,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.DengueAlerts.Add(alert);
        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast new alert via SignalR
        await BroadcastNewAlertAsync(alert);

        _logger.LogInformation("New dengue alert created: {AlertId} in {Location}", alert.Id, alert.Location);

        return alert.ToFacet<DengueAlert, DengueAlertDto>();
    }

    public async Task<DengueAlertDto> GetAlertByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var alert = await _context.DengueAlerts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (alert == null)
            throw new NotFoundException("DengueAlert", id);

        return alert.ToFacet<DengueAlert, DengueAlertDto>();
    }

    public async Task<IEnumerable<DengueAlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await _context.DengueAlerts
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return alerts.SelectFacets<DengueAlert, DengueAlertDto>();
    }

    public async Task<IEnumerable<DengueAlertDto>> GetAlertsByLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        var alerts = await _context.DengueAlerts
            .Where(a => a.Location.ToLower() == location.ToLower())
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return alerts.SelectFacets<DengueAlert, DengueAlertDto>();
    }

    public async Task<DengueAlertDto> UpdateAlertAsync(int id, UpdateDengueAlertRequest request, CancellationToken cancellationToken = default)
    {
        var alert = await _context.DengueAlerts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (alert == null)
            throw new NotFoundException("DengueAlert", id);

        // Update only provided fields
        if (request.Location != null)
            alert.Location = request.Location;
        
        if (request.Description != null)
            alert.Description = request.Description;
        
        if (request.Level.HasValue)
            alert.Level = request.Level.Value;
        
        if (request.IsActive.HasValue)
            alert.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Dengue alert updated: {AlertId}", alert.Id);

        return alert.ToFacet<DengueAlert, DengueAlertDto>();
    }

    public async Task<bool> ResolveAlertAsync(int id, CancellationToken cancellationToken = default)
    {
        var alert = await _context.DengueAlerts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (alert == null)
            throw new NotFoundException("DengueAlert", id);

        alert.IsActive = false;
        alert.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast alert resolution via SignalR
        await BroadcastAlertResolvedAsync(alert);

        _logger.LogInformation("Dengue alert resolved: {AlertId}", alert.Id);

        return true;
    }

    public async Task<bool> DeleteAlertAsync(int id, CancellationToken cancellationToken = default)
    {
        var alert = await _context.DengueAlerts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (alert == null)
            throw new NotFoundException("DengueAlert", id);

        _context.DengueAlerts.Remove(alert);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Dengue alert deleted: {AlertId}", alert.Id);

        return true;
    }

    private async Task BroadcastNewAlertAsync(DengueAlert alert)
    {
        var groupName = $"alerts-{alert.Location.ToLowerInvariant()}";
        
        await _hubContext.Clients.Group(groupName).SendAsync("NewAlert", new
        {
            Id = alert.Id,
            Location = alert.Location,
            Description = alert.Description,
            Level = alert.Level.ToString(),
            CreatedAt = alert.CreatedAt,
            Type = "NewAlert"
        });

        // Also send to general notifications
        await _hubContext.Clients.All.SendAsync("GeneralAlert", new
        {
            Message = $"New {alert.Level} alert in {alert.Location}: {alert.Description}",
            Location = alert.Location,
            Level = alert.Level.ToString(),
            Timestamp = alert.CreatedAt
        });

        _logger.LogInformation("New alert broadcasted for location {Location} with level {Level}", 
            alert.Location, alert.Level);
    }

    private async Task BroadcastAlertResolvedAsync(DengueAlert alert)
    {
        var groupName = $"alerts-{alert.Location.ToLowerInvariant()}";
        
        await _hubContext.Clients.Group(groupName).SendAsync("AlertResolved", new
        {
            Id = alert.Id,
            Location = alert.Location,
            ResolvedAt = alert.ResolvedAt,
            Type = "AlertResolved"
        });

        _logger.LogInformation("Alert resolution broadcasted for location {Location}", alert.Location);
    }
}
