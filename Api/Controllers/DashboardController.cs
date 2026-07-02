using System.Globalization;
using Api.Dtos;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("kpis")]
    public async Task<ActionResult<List<DashboardKpiDto>>> GetKpis(CancellationToken cancellationToken = default)
    {
        var data = await dashboardService.GetKpisAsync(cancellationToken);
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
        var items = (await dashboardService.GetLowStockAsync(cancellationToken))
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
        var result = (await dashboardService.GetActivityAsync(limit, cancellationToken))
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
