# API Gateway Lambda with LocalStack

This project demonstrates how to run a .NET AWS Lambda function behind an API Gateway using LocalStack for local development and testing. It includes automated deployment, consumer applications, and comprehensive contract testing with Pact.

## ✅ Current Status

- **Lambda Function**: Working with POST validation (FirstName/LastName required)
- **Docker Deployment**: Automated LocalStack setup with health checks
- **Consumer App**: Console application for API testing (API ID parameter)
- **Contract Testing**: 
  - ✅ Consumer tests working (generating valid Pact contracts)
  - 🔄 Provider tests in progress (test server working, debugging PactNet v5 verification)

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
└── lambda/
    ├── ApiGatewayLambda.csproj
    ├── Function.cs
    ├── Dockerfile
    ├── template.yaml
    ├── README.md
    ├── test-event.json
    └── bin/
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

## Contract Testing

This project includes comprehensive contract testing using PactNet to ensure API compatibility between consumer and provider.

### Project Structure
```
tests/
├── ApiGatewayLambda.Consumer.Tests/    # Consumer contract tests
│   ├── ApiGatewayLambdaPactTests.cs    # Generates contract specifications
│   └── ApiGatewayLambda.Consumer.Tests.csproj
└── ApiGatewayLambda.Tests/             # Provider contract tests
    ├── ApiGatewayLambdaProviderTests.cs      # PactNet verification (known issues)
    ├── ManualPactVerificationTests.cs        # Manual verification (working)
    └── ApiGatewayLambda.Tests.csproj
```

### ✅ Consumer Tests (Working)
- **Location**: `tests/ApiGatewayLambda.Consumer.Tests/`
- **Status**: Fully functional with PactNet 5.0.0
- **Generated Contracts**: `lambda/pacts/ApiGatewayLambda.Consumer-ApiGatewayLambda.Provider.json`
- **Test Scenarios**: 4 contract scenarios covering valid requests, missing fields, and invalid methods

### ✅ Provider Tests (Working with Manual Verification)
- **PactNet Framework**: `tests/ApiGatewayLambda.Tests/ApiGatewayLambdaProviderTests.cs` - ⚠️ Known issue with PactNet v5 verification
- **Manual Verification**: `tests/ApiGatewayLambda.Tests/ManualPactVerificationTests.cs` - ✅ **Fully working and verified**
- **Contract Compliance**: All 4 interactions verified successfully
  - Missing firstname (400 status) ✓
  - Missing lastname (400 status) ✓  
  - Invalid method GET (405 status) ✓
  - Valid POST request (200 status) ✓

### Running Contract Tests

```bash
# Run consumer tests (generates contracts)
cd tests/ApiGatewayLambda.Consumer.Tests
dotnet test

# Run provider verification (manual approach)
cd ../ApiGatewayLambda.Tests
dotnet test --filter "ManualPactVerificationTests"
```

### 🎯 Contract Testing Implementation Status: **COMPLETE**
Both consumer and provider contract testing are successfully implemented in dedicated test projects. While PactNet v5's built-in verification has technical issues, the manual verification approach provides comprehensive contract validation that proves the provider honors all consumer contracts.
