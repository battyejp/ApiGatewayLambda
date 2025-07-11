#!/bin/bash

# Build the Lambda function
echo "Building Lambda function..."
cd lambda
dotnet publish -c Release -o publish

# Create deployment package
echo "Creating deployment package..."
cd publish
zip -r ../lambda.zip .
cd ..

# Copy to deploy directory
cp lambda.zip ../deploy/

echo "Lambda deployment package created: lambda.zip"
echo "Package copied to deploy directory"
