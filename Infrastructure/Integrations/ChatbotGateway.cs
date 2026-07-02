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
                new FastApiChatRequest(request.SessionId, request.Message, statePayload),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Chatbot respondió {StatusCode}: {Body}", (int)response.StatusCode, body);
                throw new InvalidOperationException("El servicio de chatbot no está disponible en este momento.");
            }

            var payload = await response.Content.ReadFromJsonAsync<FastApiChatResponse>(cancellationToken);
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
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Timeout al contactar al chatbot en {Endpoint}", endpoint);
            throw new InvalidOperationException(
                "El servicio de chatbot no respondió a tiempo. Verifica que FastAPI esté en ejecución en http://localhost:8000.",
                ex);
        }
    }

    public async Task<bool> PingHealthAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Chatbot:BaseUrl"] ?? "http://localhost:8000";
        var endpoint = $"{baseUrl.TrimEnd('/')}/health";

        try
        {
            using var response = await httpClient.GetAsync(endpoint, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Health check del chatbot falló en {Endpoint}", endpoint);
            return false;
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
                    UnitPrice = payload.OperationSummary.UnitPrice,
                    Subtotal = payload.OperationSummary.Subtotal,
                    Tax = payload.OperationSummary.Tax,
                    Total = payload.OperationSummary.Total,
                },
        };

    private sealed record FastApiChatRequest(
        [property: JsonPropertyName("sessionId")] string SessionId,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("state")] object? State);

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
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("tax")]
        public decimal Tax { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }
    }
}
