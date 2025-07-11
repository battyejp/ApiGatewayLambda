#!/bin/bash

# Wait for LocalStack to be ready
echo "Waiting for LocalStack to be ready..."
echo "Testing LocalStack connection..."

# Simple check - just see if we can connect
counter=0
max_attempts=30
while [ $counter -lt $max_attempts ]; do
    if curl -s http://localstack:4566/_localstack/health > /dev/null; then
        echo "LocalStack is responding!"
        break
    fi
    echo "Attempt $((counter + 1))/$max_attempts: LocalStack not ready yet, waiting 5 seconds..."
    sleep 5
    counter=$((counter + 1))
done

if [ $counter -eq $max_attempts ]; then
    echo "ERROR: LocalStack failed to start after $max_attempts attempts"
    exit 1
fi

echo "LocalStack is ready!"

# Wait a bit more for Lambda service to be fully initialized
sleep 10

# Set AWS credentials for LocalStack
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION=us-east-1

# Create Lambda function
echo "Creating Lambda function..."
aws lambda create-function \
    --function-name api-gateway-lambda \
    --runtime dotnet8 \
    --role arn:aws:iam::123456789012:role/lambda-role \
    --handler ApiGatewayLambda::ApiGatewayLambda.Function::FunctionHandler \
    --zip-file fileb:///app/lambda.zip \
    --timeout 30 \
    --memory-size 256 \
    --endpoint-url http://localstack:4566

if [ $? -eq 0 ]; then
    echo "Lambda function created successfully!"
else
    echo "Failed to create Lambda function"
    exit 1
fi

# Create API Gateway
echo "Creating API Gateway..."
API_ID=$(aws apigateway create-rest-api \
    --name api-gateway-lambda-api \
    --endpoint-url http://localstack:4566 \
    --query 'id' \
    --output text)

if [ $? -eq 0 ]; then
    echo "API Gateway created with ID: $API_ID"
else
    echo "Failed to create API Gateway"
    exit 1
fi

# Get root resource ID
ROOT_RESOURCE_ID=$(aws apigateway get-resources \
    --rest-api-id $API_ID \
    --endpoint-url http://localstack:4566 \
    --query 'items[0].id' \
    --output text)

echo "Root resource ID: $ROOT_RESOURCE_ID"

# Create POST method
echo "Creating POST method..."
aws apigateway put-method \
    --rest-api-id $API_ID \
    --resource-id $ROOT_RESOURCE_ID \
    --http-method POST \
    --authorization-type NONE \
    --endpoint-url http://localstack:4566

if [ $? -eq 0 ]; then
    echo "POST method created successfully!"
else
    echo "Failed to create POST method"
    exit 1
fi

# Create Lambda integration
echo "Creating Lambda integration..."
aws apigateway put-integration \
    --rest-api-id $API_ID \
    --resource-id $ROOT_RESOURCE_ID \
    --http-method POST \
    --type AWS_PROXY \
    --integration-http-method POST \
    --uri arn:aws:apigateway:us-east-1:lambda:path/2015-03-31/functions/arn:aws:lambda:us-east-1:000000000000:function:api-gateway-lambda/invocations \
    --endpoint-url http://localstack:4566

if [ $? -eq 0 ]; then
    echo "Lambda integration created successfully!"
else
    echo "Failed to create Lambda integration"
    exit 1
fi

# Deploy API
echo "Deploying API..."
aws apigateway create-deployment \
    --rest-api-id $API_ID \
    --stage-name prod \
    --endpoint-url http://localstack:4566

if [ $? -eq 0 ]; then
    echo "API deployed successfully!"
else
    echo "Failed to deploy API"
    exit 1
fi

# Save API ID to a file for easy reference
echo $API_ID > /app/api-id.txt

echo "============================================"
echo "🎉 Deployment completed successfully!"
echo "============================================"
echo "API Gateway ID: $API_ID"
echo "API Endpoint: http://localhost:4566/restapis/$API_ID/prod/_user_request_/"
echo ""
echo "Test the API with:"
echo "PowerShell:"
echo "\$body = @{FirstName=\"John\"; LastName=\"Doe\"} | ConvertTo-Json"
echo "Invoke-RestMethod -Uri \"http://localhost:4566/restapis/$API_ID/prod/_user_request_/\" -Method Post -ContentType \"application/json\" -Body \$body"
echo ""
echo "Bash/curl:"
echo "curl -X POST http://localhost:4566/restapis/$API_ID/prod/_user_request_/ \\"
echo "  -H \"Content-Type: application/json\" \\"
echo "  -d '{\"FirstName\": \"John\", \"LastName\": \"Doe\"}'"
echo "============================================"

# Keep the container running to show logs
echo "Deployment container will keep running to show this information..."
echo "Press Ctrl+C to stop or use 'docker-compose down' to stop all services"

# Keep container alive
tail -f /dev/null
