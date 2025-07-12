# API Consumer

This console application tests the API Gateway Lambda function through LocalStack.

## Features

- Accepts API ID as a command-line argument for direct testing
- Automatically waits for LocalStack to be ready
- Performs comprehensive testing:
  1. **Bad Request Test 1**: Missing `lastName` field (expects 400 Bad Request)
  2. **Bad Request Test 2**: Missing `firstName` field (expects 400 Bad Request)
  3. **Success Test**: Valid request with both `firstName` and `lastName` (expects 200 OK)

## Getting the API ID

First, you need to get the API ID from LocalStack. Start your services and then run:

```bash
# Get the API ID from LocalStack
aws apigateway get-rest-apis --endpoint-url http://localhost:4566
```

Or check the deployment logs to find the API ID that was created.

## Running the Consumer

### Standalone (Recommended)

**PowerShell:**
```powershell
.\run-consumer.ps1 jo6ttjff2g
```

**Bash:**
```bash
chmod +x run-consumer.sh
./run-consumer.sh jo6ttjff2g
```

**Direct dotnet command:**
```bash
dotnet run jo6ttjff2g
```

### With Docker Compose

The consumer can still be run with Docker Compose, but you'll need to update the command in docker-compose.yml to include the API ID.

## Building the Consumer

**PowerShell:**
```powershell
.\build-consumer.ps1
```

**Bash:**
```bash
chmod +x build-consumer.sh
./build-consumer.sh
```

## Expected Output

When running successfully, you should see output like:

```
=== API Gateway Lambda Consumer ===
📋 Using API ID: jo6ttjff2g
🔗 API Gateway endpoint: http://localhost:4566/restapis/jo6ttjff2g/prod/_user_request_/
Waiting for LocalStack to be ready...
✅ LocalStack is ready!

🧪 Test 1: Bad Request (missing lastName)
📤 Sending request: {"FirstName":"John"}
📥 Response Status: BadRequest
📥 Response Body: {"error":"Both firstname and lastname are required."}
✅ Bad request test passed - received expected 400 status

🧪 Test 2: Bad Request (missing firstName)
📤 Sending request: {"LastName":"Doe"}
📥 Response Status: BadRequest
📥 Response Body: {"error":"Both firstname and lastname are required."}
✅ Bad request test passed - received expected 400 status

🧪 Test 3: Successful Request
📤 Sending request: {"FirstName":"John","LastName":"Doe"}
📥 Response Status: OK
📥 Response Body: {"message":"Request processed successfully","fullName":"John Doe"}
✅ Successful request test passed - received expected 200 status

✅ All tests completed!
```

## How It Works

1. **API ID Input**: The consumer accepts the API ID as a command-line argument
2. **Endpoint Construction**: It constructs the API Gateway endpoint URL using the provided API ID
3. **Health Check**: The consumer checks if LocalStack is ready by calling the health endpoint
4. **Testing**: It performs three different HTTP POST requests to test various scenarios
5. **Validation**: Each test validates the response status code and displays the results

## Configuration

The consumer is configured to:
- Accept API ID as the first command-line argument
- Connect to LocalStack at `http://localhost:4566`
- Wait up to 30 attempts (60 seconds) for LocalStack to be ready
- Use the standard API Gateway endpoint format: `/restapis/{api-id}/prod/_user_request_/`

## Dependencies

- .NET 8.0
- System.Text.Json (for JSON serialization)
- HttpClient (for HTTP requests)
