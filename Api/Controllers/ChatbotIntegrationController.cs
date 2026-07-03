using Api.Dtos;
using Api.Filters;
using Api.Mapping;
using Application.Abstractions;
using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ChatbotApiKey]
[ApiController]
[Route("api/chatbot")]
public sealed class ChatbotIntegrationController(
    IProductService productService,
    ICustomerRepository customerRepository) : ControllerBase
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
            new ProductQueryModel
            {
                Query = q,
                Page = safePage,
                PageSize = safePageSize,
                AvailableForSaleOnly = true,
            },
            cancellationToken);

        return Ok(result.ToPagedDto(p => p.ToProductDto()));
    }

    [HttpGet("customers/delivery-address")]
    public async Task<ActionResult<ChatbotCustomerDeliveryAddressDto>> GetCustomerDeliveryAddress(
        [FromQuery] string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("El correo del cliente es obligatorio.");
        }

        var customer = await customerRepository.GetByEmailAsync(email.Trim(), cancellationToken);
        if (customer is null
            || string.IsNullOrWhiteSpace(customer.SavedDeliveryAddress)
            || string.IsNullOrWhiteSpace(customer.SavedDeliveryCity))
        {
            return Ok(new ChatbotCustomerDeliveryAddressDto());
        }

        return Ok(
            new ChatbotCustomerDeliveryAddressDto
            {
                DeliveryAddress = customer.SavedDeliveryAddress,
                DeliveryCity = customer.SavedDeliveryCity,
            });
    }
}
