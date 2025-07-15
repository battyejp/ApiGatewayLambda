# Manual test script to verify the test server is working
Write-Host "Starting manual test server verification..."

# Build the project first
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed"
    exit 1
}

# Start a background job to run a simple HTTP server test
$job = Start-Job -ScriptBlock {
    Add-Type -AssemblyName System.Net.Http
    $client = New-Object System.Net.Http.HttpClient
    
    # Wait for server to start
    Start-Sleep -Seconds 2
    
    try {
        # Test basic endpoint
        $response = $client.PostAsync("http://localhost:9223/", [System.Net.Http.StringContent]::new('{"FirstName":"John","LastName":"Doe"}', [System.Text.Encoding]::UTF8, "application/json")).GetAwaiter().GetResult()
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        
        Write-Host "Response Status: $($response.StatusCode)"
        Write-Host "Response Content: $content"
        
        return @{
            StatusCode = [int]$response.StatusCode
            Content = $content
        }
    }
    catch {
        Write-Host "Error: $($_.Exception.Message)"
        return $null
    }
    finally {
        $client.Dispose()
    }
}

Write-Host "Job started, testing HTTP client..."
$result = Receive-Job -Job $job -Wait
Remove-Job -Job $job

if ($result) {
    Write-Host "Test completed successfully"
    Write-Host "Status Code: $($result.StatusCode)"
    Write-Host "Content: $($result.Content)"
} else {
    Write-Host "Test failed"
}
