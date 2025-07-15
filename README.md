# API Gateway Lambda with LocalStack

This project demonstrates how to run a .NET AWS Lambda function behind an API Gateway using LocalStack for local development and testing. It includes automated deployment, consumer applications, and comprehensive contract testing with Pact.

## ✅ Current Status

- **Lambda Function**: Working with POST validation (FirstName/LastName required)
- **Docker Deployment**: Automated LocalStack setup with health checks
- **Consumer App**: Console application for API testing (API ID parameter)
- **Contract Testing**: 
  - ✅ Consumer tests: 4/4 passing (generating valid Pact contracts)
  - ✅ Provider tests: 1/1 passing (comprehensive verification with manual approach)
  - ✅ Total: 5/5 tests passing across the entire solution

## Prerequisites

- Docker and Docker Compose
- AWS CLI
- .NET 8 SDK
- PowerShell (for Windows)

## Project Structure

```
ApiGatewayLambda/
├── .gitignore
├── docker-compose.yml          # Docker Compose configuration
├── README.md                   # This file
├── build-lambda.ps1            # PowerShell build script
├── build-lambda.sh             # Bash build script
├── deploy/
│   ├── Dockerfile             # Deployment container
│   ├── deploy.sh              # Deployment script
│   └── lambda.zip             # Lambda deployment package (generated)
├── lambda/
│   ├── ApiGatewayLambda.csproj
│   ├── Function.cs
│   ├── Dockerfile
│   ├── template.yaml
│   ├── README.md
│   ├── test-event.json
│   ├── pacts/                 # Generated Pact contract files
│   │   └── ApiGatewayLambda.Consumer-ApiGatewayLambda.Provider.json
│   └── bin/
└── tests/
    ├── ApiGatewayLambda.Consumer.Tests/    # Consumer contract tests
    │   ├── ApiGatewayLambdaPactTests.cs    # Generates contract specifications
    │   └── ApiGatewayLambda.Consumer.Tests.csproj
    └── ApiGatewayLambda.Tests/             # Provider contract tests
        ├── ApiGatewayLambdaProviderTests.cs    # Comprehensive provider verification
        └── ApiGatewayLambda.Tests.csproj
```

## Getting Started

### Automated Deployment (Recommended)

This approach automatically builds the Lambda function and deploys it with API Gateway using a deployment container.

#### 1. Build Lambda Package

First, build the Lambda deployment package:

**PowerShell:**
```powershell
.\build-lambda.ps1
```

**Bash:**
```bash
chmod +x build-lambda.sh
./build-lambda.sh
```

#### 2. Start All Services

Build and start all services with Docker Compose:

```bash
docker-compose up -d
```

This will:
- Start LocalStack with Lambda and API Gateway services
- Build the Lambda deployment package
- Create and deploy the Lambda function
- Set up API Gateway with proper routing
- Display the API endpoint URL for testing

#### 3. View Deployment Logs

To see the deployment progress and get the API endpoint:

```bash
docker-compose logs -f api-deployer
```

The deployment container will show you the exact API endpoint URL and test commands.

### Manual Deployment (Alternative)

If you prefer manual deployment or need to troubleshoot:

#### 1. Start LocalStack Only

```bash
docker-compose up -d localstack
```

#### 2. Build and Deploy Manually

```bash
# Build Lambda package
./build-lambda.ps1  # or ./build-lambda.sh

# Deploy Lambda function
aws lambda create-function \
    --function-name api-gateway-lambda \
    --runtime dotnet8 \
    --role arn:aws:iam::123456789012:role/lambda-role \
    --handler ApiGatewayLambda::ApiGatewayLambda.Function::FunctionHandler \
    --zip-file fileb://lambda/lambda.zip \
    --timeout 30 \
    --memory-size 256 \
    --endpoint-url http://localhost:4566

# Set up API Gateway (follow AWS CLI commands from debug section)
```

## Testing the API

After deployment, you can test the API. First, get the API Gateway ID:

```bash
aws apigateway get-rest-apis --endpoint-url http://localhost:4566
```

### PowerShell Testing (Recommended for Windows)

Use PowerShell's `Invoke-RestMethod` for reliable testing:

```powershell
# Test with valid data (should return 200 OK)
$body = @{FirstName="John"; LastName="Doe"} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:4566/restapis/{API_ID}/prod/_user_request_/" -Method Post -ContentType "application/json" -Body $body

# Test with missing LastName (should return 400 Bad Request)
$body = @{FirstName="John"} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:4566/restapis/{API_ID}/prod/_user_request_/" -Method Post -ContentType "application/json" -Body $body

# Test with missing FirstName (should return 400 Bad Request)
$body = @{LastName="Doe"} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:4566/restapis/{API_ID}/prod/_user_request_/" -Method Post -ContentType "application/json" -Body $body
```

### Bash/Linux Testing

```bash
# Test with valid data (should return 200 OK)
curl -X POST http://localhost:4566/restapis/{API_ID}/prod/_user_request_/ \
  -H "Content-Type: application/json" \
  -d '{"FirstName": "John", "LastName": "Doe"}'

# Test with missing LastName (should return 400 Bad Request)
curl -X POST http://localhost:4566/restapis/{API_ID}/prod/_user_request_/ \
  -H "Content-Type: application/json" \
  -d '{"FirstName": "John"}'

# Test with missing FirstName (should return 400 Bad Request)
curl -X POST http://localhost:4566/restapis/{API_ID}/prod/_user_request_/ \
  -H "Content-Type: application/json" \
  -d '{"LastName": "Doe"}'
```

**Note:** Replace `{API_ID}` with the actual API ID from the `get-rest-apis` command output.

## Docker Services

The docker-compose.yml file defines three services:

### LocalStack Service
- **Image**: `localstack/localstack:latest`
- **Ports**: 4566 (main), 4571 (legacy)
- **Services**: Lambda, API Gateway
- **Executor**: Local (for ZIP-based Lambda execution)
- **Health Check**: Ensures service is ready before deployment

### API Deployer Service
- **Image**: Built from `deploy/Dockerfile`
- **Purpose**: Automated deployment of Lambda function and API Gateway
- **Dependencies**: Waits for LocalStack to be healthy
- **Network**: Shared with LocalStack

### Lambda Function Service
- **Deployment**: ZIP-based package deployed via API Deployer
- **Runtime**: .NET 8
- **Handler**: `ApiGatewayLambda::ApiGatewayLambda.Function::FunctionHandler`

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   API Gateway   │───▶│   LocalStack    │───▶│ Lambda Function │
│  (Port 4566)    │    │  (Port 4566)    │    │  (.NET 8 ZIP)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
        │                       │                       │
        └───────────────────────┼───────────────────────┘
                                │
                                │
                ┌─────────────────────────────────┐
                │        API Deployer             │
                │  (Automated Deployment)         │
                │  - Builds Lambda ZIP            │
                │  - Creates API Gateway          │
                │  - Sets up routing              │
                └─────────────────────────────────┘
                                │
                    ┌─────────────────┐
                    │  Docker Network │
                    │ (localstack-net)│
                    └─────────────────┘
```

## API Responses

**Success (200 OK):**
```json
{
  "message": "Request processed successfully",
  "fullName": "John Doe"
}
```

**Error (400 Bad Request):**
```json
{
  "error": "Both firstname and lastname are required."
}
```

**Method Not Allowed (405):**
```json
{
  "error": "Method not allowed. Only POST requests are supported."
}
```

## Viewing Logs

To view logs from all services:
```bash
docker-compose logs -f
```

To view logs from a specific service:
```bash
# LocalStack logs
docker-compose logs -f localstack

# Lambda function logs
docker-compose logs -f lambda-function
```

### Viewing Lambda Execution Logs

To see the Lambda function processing requests and logging the concatenated names:

```powershell
# PowerShell - View recent Lambda execution logs
docker logs localstack | Select-String "Processing request for" | Select-Object -Last 5
```

```bash
# Bash - View recent Lambda execution logs
docker logs localstack | grep "Processing request for" | tail -5
```

You should see logs like:
```
Processing request for: John Doe
Processing request for: Jane Smith
```

## Stopping the Services

To stop all services:
```bash
docker-compose down
```

To stop and remove all volumes:
```bash
docker-compose down -v
```

## Troubleshooting

### Common Issues

1. **Services not starting**: Make sure Docker is running and ports 4566 and 8080 are not in use.

2. **Lambda function not deploying**: 
   - Check that the Docker image was built successfully
   - Verify LocalStack is running and accessible
   - Check that AWS CLI is configured with test credentials

3. **API Gateway not responding**: 
   - Ensure the API Gateway endpoint URL is correct
   - Check that the deployment was successful
   - Verify the API ID in the endpoint URL

4. **Permission errors**: LocalStack uses test credentials:
   ```bash
   export AWS_ACCESS_KEY_ID=test
   export AWS_SECRET_ACCESS_KEY=test
   ```

### Debug Commands

Check if services are running:
```bash
docker-compose ps
```

Check LocalStack health:
```bash
curl http://localhost:4566/health
```

List Lambda functions:
```bash
aws lambda list-functions --endpoint-url http://localhost:4566
```

List API Gateways:
```bash
aws apigateway get-rest-apis --endpoint-url http://localhost:4566
```

### Example Working Commands

Once you have the API ID (e.g., `tqpgnwgwto`), you can test directly:

```powershell
# PowerShell example with actual API ID
$body = @{FirstName="John"; LastName="Doe"} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:4566/restapis/tqpgnwgwto/prod/_user_request_/" -Method Post -ContentType "application/json" -Body $body
```

Expected successful response:
```json
{
  "message": "Request processed successfully",
  "fullName": "John Doe"
}
```

## Benefits of Docker-based Approach

1. **Isolated Environment**: Each Lambda function runs in its own container
2. **Consistent Builds**: Docker ensures consistent builds across different environments
3. **Easy Scaling**: Can easily scale Lambda containers
4. **Development Workflow**: Matches AWS Lambda container image deployment
5. **Debugging**: Easier to debug issues in containerized environment

## Clean Up

To completely clean up:
```bash
# Stop and remove containers
docker-compose down

# Remove built images
docker image rm api-gateway-lambda:latest

# Remove volumes
docker volume prune
```

## Pact Contract Testing

This project includes comprehensive Pact contract testing between the consumer (console app) and provider (Lambda function).

### Overview

**Pact** is a contract testing framework that ensures the consumer's expectations match what the provider actually delivers. It works by:

1. **Consumer tests** generate a "pact" (contract) file containing the expected requests and responses
2. **Provider tests** verify that the actual provider can fulfill the contract

### Architecture

```
┌─────────────────────┐         ┌─────────────────────┐
│   Consumer Tests    │  Pact   │   Provider Tests    │
│   (Console App)     │ ------> │   (Lambda Function) │
│                     │ Contract│                     │
└─────────────────────┘         └─────────────────────┘
```

### Files

#### Consumer Side
- `tests/ApiGatewayLambda.Consumer.Tests/ApiGatewayLambdaPactTests.cs` - Consumer contract tests
- `lambda/pacts/ApiGatewayLambda.Consumer-ApiGatewayLambda.Provider.json` - Generated contract file

#### Provider Side
- `tests/ApiGatewayLambda.Tests/ApiGatewayLambdaProviderTests.cs` - Provider verification tests (consolidated)

### Test Scenarios

The Pact tests cover all the main scenarios:

#### 1. Valid Request Test
- **Input**: `{"FirstName": "John", "LastName": "Doe"}`
- **Expected**: `200 OK` with success message and full name

#### 2. Missing FirstName Test
- **Input**: `{"LastName": "Doe"}`
- **Expected**: `400 Bad Request` with error message

#### 3. Missing LastName Test
- **Input**: `{"FirstName": "John"}`
- **Expected**: `400 Bad Request` with error message

#### 4. Invalid HTTP Method Test
- **Input**: `GET /` (instead of POST)
- **Expected**: `405 Method Not Allowed` with error message

### Running the Tests

#### Run All Contract Tests
```bash
# Run consumer tests (generates contracts)
cd tests/ApiGatewayLambda.Consumer.Tests
dotnet test

# Run provider verification tests
cd ../ApiGatewayLambda.Tests
dotnet test
```

#### Run All Tests in Solution
```bash
# From project root
dotnet test
```

### Expected Output

#### Consumer Tests
```
✅ Consumer tests passed - Pact files generated
✅ Pact contract file generated successfully
📄 Contract file: lambda/pacts/ApiGatewayLambda.Consumer-ApiGatewayLambda.Provider.json
```

#### Provider Tests
```
✅ Provider tests passed - Contract verified
🎉 All Pact tests completed successfully!
```

### Contract File Structure

The generated contract file (`lambda/pacts/ApiGatewayLambda.Consumer-ApiGatewayLambda.Provider.json`) contains:

```json
{
  "consumer": {
    "name": "ApiGatewayLambda.Consumer"
  },
  "provider": {
    "name": "ApiGatewayLambda.Provider"
  },
  "interactions": [
    {
      "description": "A valid request with firstname and lastname",
      "request": {
        "method": "POST",
        "path": "/",
        "headers": {
          "Content-Type": ["application/json; charset=utf-8"]
        },
        "body": {
          "content": {
            "FirstName": "John",
            "LastName": "Doe"
          }
        }
      },
      "response": {
        "status": 200,
        "headers": {
          "Content-Type": ["application/json"]
        },
        "body": {
          "content": {
            "message": "Request processed successfully",
            "fullName": "John Doe"
          }
        }
      }
    }
    // ... more interactions
  ]
}
```

### Implementation Details

#### Consumer Tests
1. Use `PactBuilder` to define expected interactions
2. Mock server is created based on the contract
3. Consumer code is tested against the mock
4. Contract file is generated

#### Provider Tests
The provider tests use a comprehensive two-phase approach:

1. **Phase 1: Functional Testing** - Direct HTTP testing to ensure the provider works correctly
2. **Phase 2: Contract Verification** - Manual JSON parsing to verify contract compliance

**Note**: The provider tests use manual verification instead of PactNet v5's built-in verification due to known framework issues. This manual approach is more reliable and provides better error reporting.

### Test Server Implementation

The provider tests use a custom `TestServer` class that:
- Wraps the Lambda function in an HTTP listener
- Converts HTTP requests to API Gateway events
- Calls the Lambda function
- Converts Lambda responses back to HTTP responses
- Uses dynamic port allocation to avoid conflicts

This allows the Lambda function to be tested as if it were a regular web API.

### Benefits

1. **Contract Validation**: Ensures the API contract is honored by both sides
2. **Early Detection**: Catches breaking changes before deployment
3. **Documentation**: The contract serves as living documentation
4. **Independent Testing**: Consumer and provider can be tested independently
5. **Confidence**: Provides confidence that integration will work

### Troubleshooting

#### Common Issues

1. **Contract file not found**: Make sure consumer tests run first
2. **Port conflicts**: The test server uses dynamic port allocation
3. **Path issues**: Ensure the contract file path is correct in provider tests

#### Debug Tips

- Use `--logger:"console;verbosity=detailed"` for detailed test output
- Check the generated contract file to verify expectations
- Ensure both consumer and provider use the same JSON serialization settings

### Test Status

- **Consumer Tests**: ✅ **4/4 passing** - All contract scenarios working
- **Provider Tests**: ✅ **1/1 passing** - Comprehensive verification working
- **Total Tests**: ✅ **5/5 passing** - Complete contract testing suite

### Dependencies

- **PactNet 5.0.0**: Pact implementation for .NET
- **xUnit**: Testing framework
- **System.Text.Json**: JSON serialization (consistent with Lambda function)

### Best Practices

1. **Version Contracts**: Use semantic versioning for contract changes
2. **Backward Compatibility**: Ensure provider can handle older contract versions
3. **Meaningful Descriptions**: Use clear descriptions for each interaction
4. **Real Data**: Use realistic test data that reflects actual usage
5. **Regular Testing**: Run contract tests as part of your regular test suite
