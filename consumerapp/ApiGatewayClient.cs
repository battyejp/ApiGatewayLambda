using System.Net;
using System.Text;
using System.Text.Json;
using ConsumerApp;

namespace ApiConsumer;

public class ApiGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ApiGatewayClient(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    public async Task<ApiResponse> SendRequestAsync(Person person, string marketId = "POL1")
    {
        try
        {
            var jsonContent = JsonSerializer.Serialize(person);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            content.Headers.Add("X-Market-Id", marketId);

            var response = await _httpClient.PostAsync(_baseUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new ApiResponse
            {
                StatusCode = response.StatusCode,
                Content = responseContent,
                IsSuccess = response.IsSuccessStatusCode,
                RequestJson = jsonContent
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = ex.Message,
                IsSuccess = false
            };
        }
    }
}

public class ApiResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string RequestJson { get; set; } = string.Empty;
}
