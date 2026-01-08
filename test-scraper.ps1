Write-Host "Testing Faschim Scraper Endpoint" -ForegroundColor Cyan

$filter = '[{"property":"StoricoNominativo.bloccoCompleto","value":"*0","anyMatch":false},{"property":"statoPratica","value":"Da Lavorare","anyMatch":false}]'

$encodedFilter = [System.Uri]::EscapeDataString($filter)
$url = "http://localhost:5000/api/scraper/fetch-from-faschim?filter=$encodedFilter&searchName=DaLavorare"

Write-Host "`nRequest URL:" -ForegroundColor Yellow
Write-Host $url

Write-Host "`nSending request..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 120
    
    Write-Host "`n[SUCCESS] HTTP Status: $($response.StatusCode)" -ForegroundColor Green
    
    $json = $response.Content | ConvertFrom-Json
    
    Write-Host "[SUCCESS] API Success: $($json.success)" -ForegroundColor Green
    Write-Host "[SUCCESS] Record Count: $($json.count)" -ForegroundColor Green
    
    if ($json.count -gt 0) {
        Write-Host "`nFirst record sample:" -ForegroundColor Green
        Write-Host ($json.data[0] | ConvertTo-Json -Depth 2)
    } 
    else {
        Write-Host "`nNo records returned (empty result)" -ForegroundColor Yellow
    }
    
} 
catch {
    Write-Host "`n[ERROR] Request failed:" -ForegroundColor Red
    Write-Host "  Message: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "`n  Error Details:" -ForegroundColor Yellow
        try {
            $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host ($errorJson | ConvertTo-Json -Depth 3)
        } 
        catch {
            Write-Host $_.ErrorDetails.Message
        }
    }
}

Write-Host "`nTest complete." -ForegroundColor Cyan
