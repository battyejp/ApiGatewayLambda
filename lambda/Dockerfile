# Use the official .NET 8 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /src

# Copy the project file and restore dependencies
COPY ApiGatewayLambda.csproj .
RUN dotnet restore

# Copy the source code
COPY . .

# Build and publish the application
RUN dotnet publish -c Release -o /app/publish

# Use the official AWS Lambda .NET 8 runtime image
FROM public.ecr.aws/lambda/dotnet:8

# Copy the published application to the Lambda task root
COPY --from=build /app/publish ${LAMBDA_TASK_ROOT}

# Set the Lambda function handler
CMD ["ApiGatewayLambda::ApiGatewayLambda.Function::FunctionHandler"]
