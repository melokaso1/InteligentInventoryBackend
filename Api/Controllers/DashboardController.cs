using System.Globalization;
using Api.Caching;
using Api.Dtos;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService dashboardService, IMemoryCache cache) : ControllerBase
{
    [HttpGet("kpis")]
    public async Task<ActionResult<List<DashboardKpiDto>>> GetKpis(CancellationToken cancellationToken = default)
    {
        var data = await EndpointCache.GetOrCreateAsync(
            cache,
            "dashboard:kpis",
            async ct => await dashboardService.GetKpisAsync(ct),
            cancellationToken);

        return Ok(
            data.Select(
                x => new DashboardKpiDto
                {
                    Id = x.Id,
                    Label = x.Label,
                    Value = x.Value,
                    Change = x.Change,
                    ChangeType = x.ChangeType,
                    Icon = x.Icon,
                    IconBg = x.IconBg,
                    IconColor = x.IconColor,
                }).ToList());
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<LowStockItemDto>>> GetLowStock(CancellationToken cancellationToken = default)
    {
        var items = (await EndpointCache.GetOrCreateAsync(
                cache,
                "dashboard:low-stock",
                async ct => await dashboardService.GetLowStockAsync(ct),
                cancellationToken))
            .Select(
                x => new LowStockItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Sku = x.Sku,
                    CurrentStock = x.CurrentStock,
                    ReorderLevel = x.ReorderLevel,
                    Status = x.Status,
                })
            .ToList();

        return Ok(items);
    }

    [HttpGet("activity")]
    public async Task<ActionResult<List<ActivityItemDto>>> GetActivity(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = (await EndpointCache.GetOrCreateAsync(
                cache,
                $"dashboard:activity:{limit}",
                async ct => await dashboardService.GetActivityAsync(limit, ct),
                cancellationToken))
            .Select(
                x => new ActivityItemDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    Time = x.Time,
                    DotBg = x.DotBg,
                    DotBorder = x.DotBorder,
                })
            .ToList();

        return Ok(result);
    }
}
