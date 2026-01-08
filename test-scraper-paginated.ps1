# Paginated Scraper Test - Download all data with 5000 records per page
param(
    [string]$SearchName = "DaLavorare-Paginated",
    [string]$Filter = '[{"property":"StoricoNominativo.bloccoCompleto","value":"*0","anyMatch":false},{"property":"statoPratica","value":"Da Lavorare","anyMatch":false}]',
    [int]$Limit = 5000
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Paginated Faschim Scraper Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5000/api/test-faschim-fetch"
$encodedFilter = [System.Web.HttpUtility]::UrlEncode($Filter)

try {
    # First request to get total count
    Write-Host "[1] Fetching first page to get total count..." -ForegroundColor Yellow
    $page1Url = "$baseUrl`?searchName=$SearchName-Page1&filter=$encodedFilter&limit=$Limit"
    $response1 = Invoke-WebRequest -Uri $page1Url -UseBasicParsing -TimeoutSec 120
    $data1 = $response1.Content | ConvertFrom-Json
    
    Write-Host "  Status: $($data1.status)" -ForegroundColor Green
    Write-Host "  File: $($data1.filePath)" -ForegroundColor Gray
    Write-Host "  Size: $([math]::Round($data1.dataLength / 1MB, 2)) MB" -ForegroundColor Gray
    
    # Parse the first response file to get total
    $firstFile = "Backend/$($data1.filePath)"
    if (Test-Path $firstFile) {
        $firstData = Get-Content $firstFile | ConvertFrom-Json
        $total = $firstData.total
        $recordsFetched = $firstData.data.Count
        
        Write-Host ""
        Write-Host "[INFO] Total records available: $total" -ForegroundColor Cyan
        Write-Host "[INFO] Records in page 1: $recordsFetched" -ForegroundColor Cyan
        
        # Calculate total pages
        $totalPages = [math]::Ceiling($total / $Limit)
        Write-Host "[INFO] Total pages needed: $totalPages (with limit=$Limit)" -ForegroundColor Cyan
        Write-Host ""
        
        # Loop through remaining pages
        if ($totalPages -gt 1) {
            for ($page = 2; $page -le $totalPages; $page++) {
                $start = ($page - 1) * $Limit
                Write-Host "[$page/$totalPages] Fetching page $page (start=$start, limit=$Limit)..." -ForegroundColor Yellow
                
                $pageUrl = "$baseUrl`?searchName=$SearchName-Page$page&filter=$encodedFilter&limit=$Limit&start=$start&page=$page"
                $response = Invoke-WebRequest -Uri $pageUrl -UseBasicParsing -TimeoutSec 120
                $data = $response.Content | ConvertFrom-Json
                Write-Host "  File: $($data.filePath)" -ForegroundColor Gray
                Write-Host "  Size: $([math]::Round($data.dataLength / 1MB, 2)) MB" -ForegroundColor Gray
                
                Start-Sleep -Milliseconds 500 # Rate limiting
            }
        }
        
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Summary:" -ForegroundColor Green
        Write-Host "  Total records: $total" -ForegroundColor White
        Write-Host "  Page size: $Limit" -ForegroundColor White
        Write-Host "  Total pages: $totalPages" -ForegroundColor White
        Write-Host "  Page 1 downloaded: $recordsFetched records" -ForegroundColor White
        Write-Host "========================================" -ForegroundColor Green
        
    } else {
        Write-Host "[ERROR] Could not find response file: $firstFile" -ForegroundColor Red
    }
    
} catch {
    Write-Host "[ERROR] Request failed:" -ForegroundColor Red
    Write-Host "  Message: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
