# Build the Lambda function
Write-Host "Building Lambda function..." -ForegroundColor Green
Set-Location lambda
dotnet publish -c Release -o publish

# Create deployment package
Write-Host "Creating deployment package..." -ForegroundColor Green
Set-Location publish
Compress-Archive -Path * -DestinationPath ..\lambda.zip -Force
Set-Location ..

# Copy to deploy directory
Copy-Item lambda.zip ..\deploy\

# Return to root directory
Set-Location ..

Write-Host "Lambda deployment package created: lambda.zip" -ForegroundColor Green
Write-Host "Package copied to deploy directory" -ForegroundColor Green
