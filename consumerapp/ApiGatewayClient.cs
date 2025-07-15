using System.Net;
using System.Text;
using System.Text.Json;

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

    public async Task<ApiResponse> SendValidRequestAsync(string firstName, string lastName)
    {
        var requestData = new
        {
            FirstName = firstName,
            LastName = lastName
        };
        
        return await SendRequestAsync(requestData);
    }

    public async Task<ApiResponse> SendRequestMissingFirstNameAsync(string lastName)
    {
        var requestData = new
        {
            LastName = lastName
        };
        
        return await SendRequestAsync(requestData);
    }

    public async Task<ApiResponse> SendRequestMissingLastNameAsync(string firstName)
    {
        var requestData = new
        {
            FirstName = firstName
        };
        
        return await SendRequestAsync(requestData);
    }

    public async Task<ApiResponse> SendGetRequestAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(_baseUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            return new ApiResponse
            {
                StatusCode = response.StatusCode,
                Content = responseContent,
                IsSuccess = response.IsSuccessStatusCode
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

    private async Task<ApiResponse> SendRequestAsync(object requestData)
    {
        try
        {
            var jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
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
