using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PactNet;
using PactNet.Matchers;
using Xunit;
using Xunit.Abstractions;

namespace ApiGatewayLambda.Consumer.Tests
{
    public class ApiGatewayLambdaPactTests : IDisposable
    {
        private readonly IPactBuilderV4 _pactBuilder;
        private readonly HttpClient _httpClient;
        private readonly ITestOutputHelper _output;

        public ApiGatewayLambdaPactTests(ITestOutputHelper output)
        {
            _output = output;
            _httpClient = new HttpClient();

            // Set pact output directory to lambda/pacts so provider tests can find them
            var pact = Pact.V4("ApiGatewayLambda.Consumer", "ApiGatewayLambda.Provider", new PactConfig
            {
                PactDir = Path.Combine("..", "..", "..", "..", "lambda", "pacts")
            });

            _pactBuilder = pact.WithHttpInteractions();
        }

        [Fact]
        public async Task GetValidRequest_ShouldReturnSuccessResponse()
        {
            // Arrange
            _pactBuilder
                .UponReceiving("A valid request with both names")
                .WithRequest(HttpMethod.Post, "/")
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    FirstName = Match.Type("John"),
                    LastName = Match.Type("Doe")
                })
                .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithJsonBody(new
                {
                    message = Match.Type("Request processed successfully"),
                    fullName = Match.Type("John Doe")
                });

            // Act & Assert
            await _pactBuilder.VerifyAsync(async ctx =>
            {
                _httpClient.BaseAddress = ctx.MockServerUri;
                
                var request = new
                {
                    FirstName = "John",
                    LastName = "Doe"
                };
                
                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _output.WriteLine($"Response Status: {response.StatusCode}");
                _output.WriteLine($"Response: {responseContent}");
                
                // The test should pass if the mock server matches our definition
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }

        [Fact]
        public async Task GetRequestMissingFirstName_ShouldReturnBadRequest()
        {
            // Arrange
            _pactBuilder
                .UponReceiving("A request missing firstname")
                .WithRequest(HttpMethod.Post, "/")
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    LastName = Match.Type("Doe")
                })
                .WillRespond()
                .WithStatus(HttpStatusCode.BadRequest)
                .WithHeader("Content-Type", "application/json")
                .WithJsonBody(new
                {
                    error = Match.Type("Both firstname and lastname are required.")
                });

            await _pactBuilder.VerifyAsync(async ctx =>
            {
                _httpClient.BaseAddress = ctx.MockServerUri;
                
                var request = new
                {
                    LastName = "Doe"
                };
                
                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _output.WriteLine($"Response Status: {response.StatusCode}");
                _output.WriteLine($"Response: {responseContent}");
                
                // The test should pass if the mock server matches our definition
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            });
        }

        [Fact]
        public async Task GetRequestMissingLastName_ShouldReturnBadRequest()
        {
            // Arrange
            _pactBuilder
                .UponReceiving("A request missing lastname")
                .WithRequest(HttpMethod.Post, "/")
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    FirstName = Match.Type("John")
                })
                .WillRespond()
                .WithStatus(HttpStatusCode.BadRequest)
                .WithHeader("Content-Type", "application/json")
                .WithJsonBody(new
                {
                    error = Match.Type("Both firstname and lastname are required.")
                });

            await _pactBuilder.VerifyAsync(async ctx =>
            {
                _httpClient.BaseAddress = ctx.MockServerUri;
                
                var request = new
                {
                    FirstName = "John"
                };
                
                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _output.WriteLine($"Response Status: {response.StatusCode}");
                _output.WriteLine($"Response: {responseContent}");
                
                // The test should pass if the mock server matches our definition
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            });
        }

        [Fact]
        public async Task GetRequestWithInvalidMethod_ShouldReturnMethodNotAllowed()
        {
            // Arrange
            _pactBuilder
                .UponReceiving("A request with invalid method")
                .WithRequest(HttpMethod.Get, "/")
                .WillRespond()
                .WithStatus(HttpStatusCode.MethodNotAllowed)
                .WithHeader("Content-Type", "application/json")
                .WithJsonBody(new
                {
                    error = Match.Type("Method not allowed. Only POST requests are supported.")
                });

            await _pactBuilder.VerifyAsync(async ctx =>
            {
                _httpClient.BaseAddress = ctx.MockServerUri;
                
                var response = await _httpClient.GetAsync("/");
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _output.WriteLine($"Response Status: {response.StatusCode}");
                _output.WriteLine($"Response: {responseContent}");
                
                // The test should pass if the mock server matches our definition
                Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            });
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
