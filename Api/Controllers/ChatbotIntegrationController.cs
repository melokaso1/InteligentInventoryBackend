using Api.Dtos;
using Api.Filters;
using Api.Mapping;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ChatbotApiKey]
[ApiController]
[Route("api/chatbot")]
public sealed class ChatbotIntegrationController(IProductService productService) : ControllerBase
{
    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 15);

        var result = await productService.GetProductsAsync(
            new ProductQueryModel { Query = q, Page = safePage, PageSize = safePageSize },
            cancellationToken);

        return Ok(result.ToPagedDto(p => p.ToProductDto()));
    }
}
