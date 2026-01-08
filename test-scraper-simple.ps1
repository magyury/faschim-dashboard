# Test Scraper endpoint with simple request
Write-Host "Testing Faschim Scraper Endpoint (Simple Test)" -ForegroundColor Cyan
Write-Host ""

$url = "http://localhost:5000/api/scraper/fetch-from-faschim?searchName=TestSearch"

Write-Host "Request URL:" -ForegroundColor Yellow
Write-Host $url
Write-Host ""
Write-Host "Sending request..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing
    
    Write-Host "[SUCCESS] HTTP Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host ""
    
    $json = $response.Content | ConvertFrom-Json
    
    Write-Host "Response:" -ForegroundColor Cyan
    Write-Host "  Success: $($json.success)"
    Write-Host "  Search ID: $($json.searchId)"
    Write-Host "  Search Name: $($json.searchName)"
    Write-Host "  Total Records: $($json.totalRecords)"
    Write-Host "  Records Fetched: $($json.recordsFetched)"
    Write-Host "  Faschim Success: $($json.faschimSuccess)"
    Write-Host "  Faschim CodErrore: $($json.faschimCodErrore)"
    Write-Host "  Faschim Message: $($json.faschimMessage)"
    
} catch {
    Write-Host "[ERROR] Request failed:" -ForegroundColor Red
    Write-Host "  Message: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
