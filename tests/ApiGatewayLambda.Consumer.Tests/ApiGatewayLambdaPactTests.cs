using System.Net;
using PactNet;
using PactNet.Matchers;
using Xunit.Abstractions;
using ApiConsumer;
using ConsumerApp;

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
        public async Task ConsumerApp_ValidRequest_ShouldReturnSuccessResponse()
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
                // Use the actual consumer application's API client
                var apiClient = new ApiGatewayClient(_httpClient, ctx.MockServerUri.ToString());
                var response = await apiClient.SendRequestAsync(new Person{ FirstName = "John", LastName = "Doe" });
                
                _output.WriteLine($"Request: {response.RequestJson}");
                _output.WriteLine($"Response Status: {response.StatusCode}");
                _output.WriteLine($"Response: {response.Content}");
                
                // The test should pass if the mock server matches our definition
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(response.IsSuccess);
            });
        }

        [Fact]
        public async Task ConsumerApp_RequestMissingFirstName_ShouldReturnBadRequest()
        {
            // Arrange
            _pactBuilder
                .UponReceiving("A request missing firstname")
                .WithRequest(HttpMethod.Post, "/")
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    FirstName = null as string,
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
                // Use the actual consumer application's API client
                var apiClient = new ApiGatewayClient(_httpClient, ctx.MockServerUri.ToString());
                var response = await apiClient.SendRequestAsync(new Person{ LastName = "Doe" } );
                
                _output.WriteLine($"Request: {response.RequestJson}");
                _output.WriteLine($"Response Status: {response.StatusCode}");
                _output.WriteLine($"Response: {response.Content}");
                
                // The test should pass if the mock server matches our definition
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.False(response.IsSuccess);
            });
        }

        [Fact]
        public async Task ConsumerApp_RequestMissingLastName_ShouldReturnBadRequest()
        {
            // Arrange
            _pactBuilder
                .UponReceiving("A request missing lastname")
                .WithRequest(HttpMethod.Post, "/")
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    FirstName = Match.Type("John"),
                    LastName = null as string,
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
                // Use the actual consumer application's API client
                var apiClient = new ApiGatewayClient(_httpClient, ctx.MockServerUri.ToString());
                var response = await apiClient.SendRequestAsync(new Person{ FirstName = "John" });
                
                _output.WriteLine($"Request: {response.RequestJson}");
                _output.WriteLine($"Response Status: {response.StatusCode}");
                _output.WriteLine($"Response: {response.Content}");
                
                // The test should pass if the mock server matches our definition
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.False(response.IsSuccess);
            });
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
