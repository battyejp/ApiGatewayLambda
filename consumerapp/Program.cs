using System.Text;
using System.Text.Json;

namespace ApiConsumer;

class Program
{
    private static readonly HttpClient httpClient = new();
    private static ApiGatewayClient? apiClient;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== API Gateway Lambda Consumer ===");
        
        // Check if API ID is provided
        if (args.Length == 0)
        {
            Console.WriteLine("❌ Please provide the API ID as a command-line argument.");
            Console.WriteLine("Usage: dotnet run <api-id>");
            Console.WriteLine("Example: dotnet run jo6ttjff2g");
            return;
        }
        
        var apiId = args[0];
        Console.WriteLine($"📋 Using API ID: {apiId}");
        
        // Construct the API Gateway endpoint
        var apiEndpoint = $"http://localhost:4566/restapis/{apiId}/prod/_user_request_/";
        Console.WriteLine($"🔗 API Gateway endpoint: {apiEndpoint}");
        
        // Create the API client
        apiClient = new ApiGatewayClient(httpClient, apiEndpoint);
        
        Console.WriteLine("Waiting for LocalStack to be ready...");
        
        // Wait for LocalStack to be ready
        await WaitForLocalStackAsync();
        
        Console.WriteLine();
        
        // Test 1: Bad request (missing lastName)
        Console.WriteLine("🧪 Test 1: Bad Request (missing lastName)");
        await TestBadRequestAsync();
        
        Console.WriteLine();
        
        // Test 2: Bad request (missing firstName)
        Console.WriteLine("🧪 Test 2: Bad Request (missing firstName)");
        await TestBadRequestMissingFirstNameAsync();
        
        Console.WriteLine();
        
        // Test 3: Successful request
        Console.WriteLine("🧪 Test 3: Successful Request");
        await TestSuccessfulRequestAsync();
        
        Console.WriteLine();
        Console.WriteLine("✅ All tests completed!");
    }

    static async Task WaitForLocalStackAsync()
    {
        var healthUrl = "http://localhost:4566/_localstack/health";
        var maxAttempts = 30;
        var attempt = 0;
        
        while (attempt < maxAttempts)
        {
            try
            {
                var response = await httpClient.GetAsync(healthUrl);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ LocalStack is ready!");
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // LocalStack not ready yet
            }
            
            attempt++;
            Console.WriteLine($"⏳ Waiting for LocalStack... (attempt {attempt}/{maxAttempts})");
            await Task.Delay(2000);
        }
        
        throw new Exception("LocalStack did not become ready within the expected time");
    }

    static async Task TestBadRequestAsync()
    {
        try
        {
            var response = await apiClient!.SendRequestMissingLastNameAsync("John");
            
            Console.WriteLine($"📤 Sending request: {response.RequestJson}");
            Console.WriteLine($"📥 Response Status: {response.StatusCode}");
            Console.WriteLine($"📥 Response Body: {response.Content}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                Console.WriteLine("✅ Bad request test passed - received expected 400 status");
            }
            else
            {
                Console.WriteLine($"❌ Bad request test failed - expected 400, got {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in bad request test: {ex.Message}");
        }
    }

    static async Task TestBadRequestMissingFirstNameAsync()
    {
        try
        {
            var response = await apiClient!.SendRequestMissingFirstNameAsync("Doe");
            
            Console.WriteLine($"📤 Sending request: {response.RequestJson}");
            Console.WriteLine($"📥 Response Status: {response.StatusCode}");
            Console.WriteLine($"📥 Response Body: {response.Content}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                Console.WriteLine("✅ Bad request test passed - received expected 400 status");
            }
            else
            {
                Console.WriteLine($"❌ Bad request test failed - expected 400, got {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in bad request test: {ex.Message}");
        }
    }

    static async Task TestSuccessfulRequestAsync()
    {
        try
        {
            var response = await apiClient!.SendValidRequestAsync("John", "Doe");
            
            Console.WriteLine($"📤 Sending request: {response.RequestJson}");
            Console.WriteLine($"📥 Response Status: {response.StatusCode}");
            Console.WriteLine($"📥 Response Body: {response.Content}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("✅ Successful request test passed - received expected 200 status");
            }
            else
            {
                Console.WriteLine($"❌ Successful request test failed - expected 200, got {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in successful request test: {ex.Message}");
        }
    }
}
