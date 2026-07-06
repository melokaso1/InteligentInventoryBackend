using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Abstractions;
using Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations;

public sealed class ChatbotGateway(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<ChatbotGateway> logger) : IChatbotGateway
{
    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ChatMessageResult> SendMessageAsync(
        ChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            throw new InvalidOperationException("El identificador de sesión es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("El mensaje no puede estar vacío.");
        }

        var baseUrl = configuration["Chatbot:BaseUrl"] ?? "http://localhost:8000";
        var endpoint = $"{baseUrl.TrimEnd('/')}/chat/message";

        try
        {
            object? statePayload = null;
            if (!string.IsNullOrWhiteSpace(request.StateJson))
            {
                statePayload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.StateJson);
            }

            using var response = await httpClient.PostAsJsonAsync(
                endpoint,
                new FastApiChatRequest(
                    request.SessionId,
                    request.Message,
                    statePayload,
                    request.CustomerName,
                    request.CustomerEmail),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Chatbot respondió {StatusCode}: {Body}", (int)response.StatusCode, body);
                throw new InvalidOperationException("El servicio de chatbot no está disponible en este momento.");
            }

            var payload = await response.Content.ReadFromJsonAsync<FastApiChatResponse>(JsonReadOptions, cancellationToken);
            if (payload is null)
            {
                throw new InvalidOperationException("Respuesta inválida del servicio de chatbot.");
            }

            return Map(payload);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "No se pudo contactar al chatbot en {Endpoint}", endpoint);
            throw new InvalidOperationException(
                "No se pudo conectar con el chatbot FastAPI en el puerto 8000. Verifica que el servicio esté en ejecución (cd LLMChatBot && python run.py).",
                ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Timeout al contactar al chatbot en {Endpoint}", endpoint);
            throw new InvalidOperationException(
                "El servicio de chatbot no respondió a tiempo. Verifica que FastAPI esté en ejecución en http://localhost:8000.",
                ex);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error al deserializar respuesta del chatbot en {Endpoint}", endpoint);
            throw new InvalidOperationException(
                "No se pudo interpretar la respuesta del chatbot. Inténtalo de nuevo en un momento.",
                ex);
        }
    }

    public async Task<bool> PingHealthAsync(CancellationToken cancellationToken = default)
    {
        var status = await GetHealthStatusAsync(cancellationToken);
        return status is not null;
    }

    public async Task<ChatbotHealthStatus?> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Chatbot:BaseUrl"] ?? "http://localhost:8000";
        var endpoint = $"{baseUrl.TrimEnd('/')}/health";

        try
        {
           using var response = await httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<FastApiHealthResponse>(cancellationToken);
            if (payload is null)
            {
                return null;
            }

            return new ChatbotHealthStatus
            {
                Status = payload.Status ?? "ok",
                Chatbot = payload.Service ?? "available",
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Health check del chatbot falló (timeout) en {Endpoint}", endpoint);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Health check del chatbot falló en {Endpoint}", endpoint);
            return null;
        }
    }

    private static ChatMessageResult Map(FastApiChatResponse payload) =>
        new()
        {
            Response = payload.Response,
            State = payload.State,
            StateJson = payload.StateJson is null
                ? null
                : JsonSerializer.Serialize(payload.StateJson),
            InvoiceNumber = payload.InvoiceNumber,
            Chips = payload.Chips,
            OperationSummary = payload.OperationSummary is null
                ? null
                : new ChatOperationSummary
                {
                    TransactionId = payload.OperationSummary.TransactionId,
                    Status = payload.OperationSummary.Status,
                    ProductCode = payload.OperationSummary.ProductCode,
                    ProductName = payload.OperationSummary.ProductName,
                    Quantity = payload.OperationSummary.Quantity,
                    MeasureUnit = payload.OperationSummary.MeasureUnit ?? "unit",
                    UnitPrice = payload.OperationSummary.UnitPrice,
                    Subtotal = payload.OperationSummary.Subtotal,
                    Tax = payload.OperationSummary.Tax,
                    Total = payload.OperationSummary.Total,
                    LineItems = payload.OperationSummary.LineItems?.Select(item => new ChatCartLineItem
                    {
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        MeasureUnit = item.MeasureUnit,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal,
                    }).ToList(),
                },
            Offers = payload.Offers?.Select(o => new ChatProductOffer
            {
                ProductCode = o.ProductCode,
                ProductName = o.ProductName,
                UnitPrice = o.UnitPrice,
                Stock = o.Stock,
                SaleUnit = o.SaleUnit ?? "unit",
            }).ToList(),
            OffersTotalCount = payload.OffersTotalCount,
        };

    private sealed class FastApiHealthResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("service")]
        public string? Service { get; set; }
    }

    private sealed record FastApiChatRequest(
        [property: JsonPropertyName("sessionId")] string SessionId,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("state")] object? State,
        [property: JsonPropertyName("customerName")] string? CustomerName = null,
        [property: JsonPropertyName("customerEmail")] string? CustomerEmail = null);

    private sealed class FastApiChatResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = "idle";

        [JsonPropertyName("stateJson")]
        public JsonElement? StateJson { get; set; }

        [JsonPropertyName("invoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("chips")]
        public List<string>? Chips { get; set; }

        [JsonPropertyName("operationSummary")]
        public FastApiOperationSummary? OperationSummary { get; set; }

        [JsonPropertyName("offers")]
        public List<FastApiProductOffer>? Offers { get; set; }

        [JsonPropertyName("offersTotalCount")]
        public int? OffersTotalCount { get; set; }
    }

    private sealed class FastApiProductOffer
    {
        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("stock")]
        public decimal Stock { get; set; }

        [JsonPropertyName("saleUnit")]
        public string? SaleUnit { get; set; }
    }

    private sealed class FastApiCartLineItem
    {
        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("measureUnit")]
        public string? MeasureUnit { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }
    }

    private sealed class FastApiOperationSummary
    {
        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("measureUnit")]
        public string? MeasureUnit { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("tax")]
        public decimal Tax { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("lineItems")]
        public List<FastApiCartLineItem>? LineItems { get; set; }
    }
}
