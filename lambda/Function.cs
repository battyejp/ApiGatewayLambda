using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ApiGatewayLambda;

public class Function
{
    private readonly ILogger<Function> _logger;

    public Function()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information));
        _logger = loggerFactory.CreateLogger<Function>();
    }

    /// <summary>
    /// Lambda function handler for API Gateway proxy integration
    /// </summary>
    /// <param name="request">API Gateway Lambda Proxy Request</param>
    /// <param name="context">Lambda Context</param>
    /// <returns>API Gateway Lambda Proxy Response</returns>
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing request: {request.HttpMethod} {request.Path}");

        // Check if the request method is POST
        if (request.HttpMethod?.ToUpper() != "POST")
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 405,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = JsonSerializer.Serialize(new { error = "Method not allowed. Only POST requests are supported." })
            };
        }

        // Check if request body exists
        if (string.IsNullOrEmpty(request.Body))
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = JsonSerializer.Serialize(new { error = "Request body is required." })
            };
        }

        try
        {
            // Parse the JSON request body
            var requestData = JsonSerializer.Deserialize<RequestModel>(request.Body);

            // Validate firstname and lastname
            if (string.IsNullOrWhiteSpace(requestData?.FirstName) || string.IsNullOrWhiteSpace(requestData?.LastName))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Headers = new Dictionary<string, string>
                    {
                        ["Content-Type"] = "application/json"
                    },
                    Body = JsonSerializer.Serialize(new { error = "Both firstname and lastname are required." })
                };
            }

            // Log the concatenated name as information
            var fullName = $"{requestData.FirstName} {requestData.LastName}";
            context.Logger.LogInformation($"Processing request for: {fullName}");

            // Return success response
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = JsonSerializer.Serialize(new { 
                    message = "Request processed successfully",
                    fullName = fullName
                })
            };
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"JSON parsing error: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = JsonSerializer.Serialize(new { error = "Invalid JSON format in request body." })
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Unexpected error: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = JsonSerializer.Serialize(new { error = "Internal server error." })
            };
        }
    }
}

/// <summary>
/// Request model for the API
/// </summary>
public class RequestModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
