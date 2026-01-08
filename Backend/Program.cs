using Microsoft.EntityFrameworkCore;
using FaschimPivotApp.Backend.Data;
using FaschimPivotApp.Backend.Models;
using System.Text.Json;
using System.Web;
using LiteDB;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:4280")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add EF Core with SQL Server
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add LiteDB as singleton
builder.Services.AddSingleton<LiteDatabase>(provider =>
{
    var dbPath = builder.Configuration["LiteDb:DbPath"] ?? "Data/LiteDb/scraper.db";
    return new LiteDatabase(dbPath);
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Test LiteDB endpoint
app.MapGet("/api/test-litedb", (LiteDatabase liteDb) =>
{
    try
    {
        var testCollection = liteDb.GetCollection<ScraperSearch>("test_collection");
        var testRecord = new ScraperSearch
        {
            SearchName = "Test",
            FetchDate = DateTime.Now
        };
        testCollection.Insert(testRecord);
        var count = testCollection.Count();
        testCollection.DeleteAll();
        
        return Results.Ok(new { status = "LiteDB working", testRecordId = testRecord.Id, countBeforeDelete = count });
    }
    catch (Exception ex)
    {
        return Results.Problem($"LiteDB error: {ex.Message}. Stack: {ex.StackTrace}");
    }
});

// Test Faschim authentication endpoint
app.MapGet("/api/test-faschim-auth", async (IConfiguration config) =>
{
    try
    {
        var cookieContainer = new System.Net.CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true
        };
        
        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(2)
        };
        
        var baseUri = config["FaschimApi:BaseUri"];
        var loginPath = config["FaschimApi:LoginPath"];
        var userName = config["FaschimApi:UserName"];
        var password = config["FaschimApi:Password"];
        
        var loginUrl = $"{baseUri}{loginPath}";
        var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", userName },
            { "password", password },
            { "interni", "false" }
        });
        
        var loginResponse = await httpClient.PostAsync(loginUrl, loginContent);
        var loginResult = await loginResponse.Content.ReadAsStringAsync();
        
        return Results.Ok(new 
        { 
            status = "Auth test complete",
            statusCode = (int)loginResponse.StatusCode,
            success = loginResponse.IsSuccessStatusCode,
            responsePreview = loginResult.Length > 200 ? loginResult.Substring(0, 200) : loginResult,
            cookieCount = cookieContainer.Count
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Auth test error: {ex.Message}. Stack: {ex.StackTrace}");
    }
});

// Test Faschim data fetch and save to file
app.MapGet("/api/test-faschim-fetch", async (IConfiguration config, string? filter, string? searchName, int? limit, int? start, int? page) =>
{
    try
    {
        var cookieContainer = new System.Net.CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true
        };
        
        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(2)
        };
        
        var baseUri = config["FaschimApi:BaseUri"];
        var loginPath = config["FaschimApi:LoginPath"];
        var dataPath = config["FaschimApi:DataPath"];
        var userName = config["FaschimApi:UserName"];
        var password = config["FaschimApi:Password"];
        
        // Step 1: Authenticate
        var loginUrl = $"{baseUri}{loginPath}";
        var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", userName! },
            { "password", password! },
            { "interni", "false" }
        });
        
        var loginResponse = await httpClient.PostAsync(loginUrl, loginContent);
        if (!loginResponse.IsSuccessStatusCode)
        {
            return Results.Problem($"Auth failed: {loginResponse.StatusCode}");
        }
        
        // Step 2: Fetch data
        var dataUrl = $"{baseUri}{dataPath}";
        var queryParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
        queryParams.Add("_dc", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        queryParams.Add("page", (page ?? 1).ToString());
        queryParams.Add("start", (start ?? 0).ToString());
        queryParams.Add("limit", (limit ?? 10000).ToString());
        queryParams.Add("sort", "[{\"property\":\"Pratica.dataPresentazione\",\"direction\":\"ASC\"},{\"property\":\"Pratica.numeroProtocollo\",\"direction\":\"ASC\"}]");
        
        if (!string.IsNullOrEmpty(filter))
        {
            var processedFilter = filter.Replace("${DateTime.Today.AddDays(-1).ToString(\"yyyy-MM-dd\")}", 
                DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"));
            queryParams.Add("filter", processedFilter);
        }
        
        dataUrl += "?" + queryParams.ToString();
        
        var dataResponse = await httpClient.GetAsync(dataUrl);
        var jsonData = await dataResponse.Content.ReadAsStringAsync();
        
        // Save to file
        var fileName = $"faschim-response-{searchName ?? "test"}-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        var filePath = Path.Combine("Data", "LiteDb", fileName);
        await System.IO.File.WriteAllTextAsync(filePath, jsonData);
        
        return Results.Ok(new 
        { 
            status = "Data fetched and saved",
            statusCode = (int)dataResponse.StatusCode,
            success = dataResponse.IsSuccessStatusCode,
            filePath = filePath,
            dataLength = jsonData.Length,
            responsePreview = jsonData.Length > 500 ? jsonData.Substring(0, 500) : jsonData
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Fetch test error: {ex.Message}. Stack: {ex.StackTrace}");
    }
});

// Get distinct StatoPratica values for filter dropdown
app.MapGet("/api/stato-pratica-values", async (MyDbContext db) =>
{
    try
    {
        var values = await db.FullKeplero
            .Select(x => x.StatoPratica)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        
        return Results.Ok(values);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Get distinct Coda values for Keplero Compare filter dropdown
app.MapGet("/api/keplero-compare/coda-values", async (MyDbContext db) =>
{
    try
    {
        var values = await db.KepleroCompare
            .Where(x => x.Coda != null)
            .Select(x => x.Coda!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        
        return Results.Ok(values);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Get distinct Stato values for Keplero Compare filter dropdown
app.MapGet("/api/keplero-compare/stato-values", async (MyDbContext db) =>
{
    try
    {
        var values = await db.KepleroCompare
            .Where(x => x.Stato != null)
            .Select(x => x.Stato!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        
        return Results.Ok(values);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Get distinct Esito values for Keplero Compare filter dropdown
app.MapGet("/api/keplero-compare/esito-values", async (MyDbContext db) =>
{
    try
    {
        var values = await db.KepleroCompare
            .Where(x => x.Esito != null)
            .Select(x => x.Esito!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        
        return Results.Ok(values);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Get distinct StatoPratica_Keplero values for Keplero Compare filter dropdown
app.MapGet("/api/keplero-compare/stato-keplero-values", async (MyDbContext db) =>
{
    try
    {
        var values = await db.KepleroCompare
            .Select(x => x.StatoPratica_Keplero)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        
        return Results.Ok(values);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// SSRM endpoint for ag-Grid
app.MapPost("/api/pivot-data", async (PivotRequest request, MyDbContext db) =>
{
    try
    {
        var query = db.FullKeplero.AsQueryable();

        // Apply filters
        if (request.FilterModel != null)
        {
            foreach (var filter in request.FilterModel)
            {
                var colId = filter.Key;
                var filterValue = filter.Value;

                if (filterValue.FilterType == "text" && filterValue.Filter != null)
                {
                    var searchValue = filterValue.Filter.ToString()?.ToLower() ?? "";
                    query = colId switch
                    {
                        "utenteLiquidatore" => query.Where(x => x.UtenteLiquidatore != null && x.UtenteLiquidatore.ToLower().Contains(searchValue)),
                        "numeroProtocollo" => query.Where(x => x.NumeroProtocollo.ToLower().Contains(searchValue)),
                        "dataPresentazione" => query.Where(x => x.DataPresentazione.ToLower().Contains(searchValue)),
                        "dataInserimento" => query.Where(x => x.DataInserimento.ToLower().Contains(searchValue)),
                        "modified" => query.Where(x => x.Modified.ToLower().Contains(searchValue)),
                        "descrizioneGruppoTariffa" => query.Where(x => x.DescrizioneGruppoTariffa != null && x.DescrizioneGruppoTariffa.ToLower().Contains(searchValue)),
                        "formaAssistenza" => query.Where(x => x.FormaAssistenza.ToLower().Contains(searchValue)),
                        "importoRichiesto" => query.Where(x => x.ImportoRichiesto != null && x.ImportoRichiesto.ToLower().Contains(searchValue)),
                        "importoRiconosciuto" => query.Where(x => x.ImportoRiconosciuto != null && x.ImportoRiconosciuto.ToLower().Contains(searchValue)),
                        "dataPagamento" => query.Where(x => x.DataPagamento != null && x.DataPagamento.ToLower().Contains(searchValue)),
                        "cognomePersona" => query.Where(x => x.CognomePersona.ToLower().Contains(searchValue)),
                        "nomePersona" => query.Where(x => x.NomePersona.ToLower().Contains(searchValue)),
                        "cognomeBeneficiario" => query.Where(x => x.CognomeBeneficiario.ToLower().Contains(searchValue)),
                        "nomeBeneficiario" => query.Where(x => x.NomeBeneficiario.ToLower().Contains(searchValue)),
                        "unisalInviato" => query.Where(x => x.UnisalInviato != null && x.UnisalInviato.ToLower().Contains(searchValue)),
                        "statoPratica" => query.Where(x => x.StatoPratica.ToLower().Contains(searchValue)),
                        _ => query
                    };
                }
            }
        }

        // Apply sorting
        if (request.SortModel != null && request.SortModel.Count > 0)
        {
            var firstSort = request.SortModel[0];
            var isAscending = firstSort.Sort == "asc";

            query = firstSort.ColId switch
            {
                "utenteLiquidatore" => isAscending ? query.OrderBy(x => x.UtenteLiquidatore) : query.OrderByDescending(x => x.UtenteLiquidatore),
                "numeroProtocollo" => isAscending ? query.OrderBy(x => x.NumeroProtocollo) : query.OrderByDescending(x => x.NumeroProtocollo),
                "dataPresentazione" => isAscending ? query.OrderBy(x => x.DataPresentazione) : query.OrderByDescending(x => x.DataPresentazione),
                "dataInserimento" => isAscending ? query.OrderBy(x => x.DataInserimento) : query.OrderByDescending(x => x.DataInserimento),
                "modified" => isAscending ? query.OrderBy(x => x.Modified) : query.OrderByDescending(x => x.Modified),
                "descrizioneGruppoTariffa" => isAscending ? query.OrderBy(x => x.DescrizioneGruppoTariffa) : query.OrderByDescending(x => x.DescrizioneGruppoTariffa),
                "formaAssistenza" => isAscending ? query.OrderBy(x => x.FormaAssistenza) : query.OrderByDescending(x => x.FormaAssistenza),
                "importoRichiesto" => isAscending ? query.OrderBy(x => x.ImportoRichiesto) : query.OrderByDescending(x => x.ImportoRichiesto),
                "importoRiconosciuto" => isAscending ? query.OrderBy(x => x.ImportoRiconosciuto) : query.OrderByDescending(x => x.ImportoRiconosciuto),
                "dataPagamento" => isAscending ? query.OrderBy(x => x.DataPagamento) : query.OrderByDescending(x => x.DataPagamento),
                "cognomePersona" => isAscending ? query.OrderBy(x => x.CognomePersona) : query.OrderByDescending(x => x.CognomePersona),
                "nomePersona" => isAscending ? query.OrderBy(x => x.NomePersona) : query.OrderByDescending(x => x.NomePersona),
                "cognomeBeneficiario" => isAscending ? query.OrderBy(x => x.CognomeBeneficiario) : query.OrderByDescending(x => x.CognomeBeneficiario),
                "nomeBeneficiario" => isAscending ? query.OrderBy(x => x.NomeBeneficiario) : query.OrderByDescending(x => x.NomeBeneficiario),
                "unisalInviato" => isAscending ? query.OrderBy(x => x.UnisalInviato) : query.OrderByDescending(x => x.UnisalInviato),
                "statoPratica" => isAscending ? query.OrderBy(x => x.StatoPratica) : query.OrderByDescending(x => x.StatoPratica),
                _ => query
            };
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var data = await query
            .Skip(request.StartRow)
            .Take(request.EndRow - request.StartRow)
            .ToListAsync();

        return Results.Ok(new
        {
            rowData = data,
            rowCount = totalCount
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Grouped by Protocollo endpoint
app.MapPost("/api/pivot-data-grouped", async (PivotRequest request, MyDbContext db) =>
{
    try
    {
        var query = db.FullKeplero.AsQueryable();

        // Apply filters first
        if (request.FilterModel != null)
        {
            foreach (var filter in request.FilterModel)
            {
                var colId = filter.Key;
                var filterValue = filter.Value;

                if (filterValue.FilterType == "text" && filterValue.Filter != null)
                {
                    var searchValue = filterValue.Filter.ToString()?.ToLower() ?? "";
                    query = colId switch
                    {
                        "numeroProtocollo" => query.Where(x => x.NumeroProtocollo.ToLower().Contains(searchValue)),
                        "utenteLiquidatore" => query.Where(x => x.UtenteLiquidatore != null && x.UtenteLiquidatore.ToLower().Contains(searchValue)),
                        "dataPresentazione" => query.Where(x => x.DataPresentazione.ToLower().Contains(searchValue)),
                        "statoPratica" => query.Where(x => x.StatoPratica.ToLower().Contains(searchValue)),
                        "formaAssistenza" => query.Where(x => x.FormaAssistenza.ToLower().Contains(searchValue)),
                        _ => query
                    };
                }
            }
        }

        // Group by NumeroProtocollo
        var groupedQuery = query
            .GroupBy(x => x.NumeroProtocollo)
            .Select(g => new ProtocolloGrouped
            {
                NumeroProtocollo = g.Key,
                Count = g.Count(),
                UtenteLiquidatore = g.Select(x => x.UtenteLiquidatore).FirstOrDefault(),
                DataPresentazione = g.Select(x => x.DataPresentazione).FirstOrDefault(),
                StatoPratica = g.Select(x => x.StatoPratica).FirstOrDefault(),
                FormaAssistenza = g.Select(x => x.FormaAssistenza).FirstOrDefault()
            });

        // Apply sorting
        if (request.SortModel != null && request.SortModel.Count > 0)
        {
            var firstSort = request.SortModel[0];
            var isAscending = firstSort.Sort == "asc";

            groupedQuery = firstSort.ColId switch
            {
                "numeroProtocollo" => isAscending ? groupedQuery.OrderBy(x => x.NumeroProtocollo) : groupedQuery.OrderByDescending(x => x.NumeroProtocollo),
                "count" => isAscending ? groupedQuery.OrderBy(x => x.Count) : groupedQuery.OrderByDescending(x => x.Count),
                "utenteLiquidatore" => isAscending ? groupedQuery.OrderBy(x => x.UtenteLiquidatore) : groupedQuery.OrderByDescending(x => x.UtenteLiquidatore),
                "dataPresentazione" => isAscending ? groupedQuery.OrderBy(x => x.DataPresentazione) : groupedQuery.OrderByDescending(x => x.DataPresentazione),
                "statoPratica" => isAscending ? groupedQuery.OrderBy(x => x.StatoPratica) : groupedQuery.OrderByDescending(x => x.StatoPratica),
                "formaAssistenza" => isAscending ? groupedQuery.OrderBy(x => x.FormaAssistenza) : groupedQuery.OrderByDescending(x => x.FormaAssistenza),
                _ => groupedQuery.OrderBy(x => x.NumeroProtocollo)
            };
        }
        else
        {
            groupedQuery = groupedQuery.OrderBy(x => x.NumeroProtocollo);
        }

        // Get total count before pagination
        var totalCount = await groupedQuery.CountAsync();

        // Apply pagination
        var data = await groupedQuery
            .Skip(request.StartRow)
            .Take(request.EndRow - request.StartRow)
            .ToListAsync();

        return Results.Ok(new
        {
            rowData = data,
            rowCount = totalCount
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Second table endpoint - add your table-specific logic
app.MapPost("/api/pivot-data-second", async (PivotRequest request, MyDbContext db) =>
{
    try
    {
        var query = db.SecondTable.AsQueryable();

        // Apply filters (customize based on your SecondTable columns)
        if (request.FilterModel != null)
        {
            foreach (var filter in request.FilterModel)
            {
                var colId = filter.Key;
                var filterValue = filter.Value;

                if (filterValue.FilterType == "text" && filterValue.Filter != null)
                {
                    var searchValue = filterValue.Filter.ToString()?.ToLower() ?? "";
                    // TODO: Add your column filters here
                    query = colId switch
                    {
                        "column1" => query.Where(x => x.Column1 != null && x.Column1.ToLower().Contains(searchValue)),
                        "column2" => query.Where(x => x.Column2 != null && x.Column2.ToLower().Contains(searchValue)),
                        _ => query
                    };
                }
            }
        }

        // Apply sorting (customize based on your SecondTable columns)
        if (request.SortModel != null && request.SortModel.Count > 0)
        {
            var firstSort = request.SortModel[0];
            var isAscending = firstSort.Sort == "asc";

            query = firstSort.ColId switch
            {
                "column1" => isAscending ? query.OrderBy(x => x.Column1) : query.OrderByDescending(x => x.Column1),
                "column2" => isAscending ? query.OrderBy(x => x.Column2) : query.OrderByDescending(x => x.Column2),
                _ => query.OrderBy(x => x.Column1)
            };
        }
        else
        {
            query = query.OrderBy(x => x.Column1);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var data = await query
            .Skip(request.StartRow)
            .Take(request.EndRow - request.StartRow)
            .ToListAsync();

        return Results.Ok(new
        {
            rowData = data,
            rowCount = totalCount
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Endpoint for keplero_compare table
app.MapPost("/api/keplero-compare", async (PivotRequest request, MyDbContext db) =>
{
    try
    {
        var query = db.KepleroCompare.AsQueryable();

        // Apply filters
        if (request.FilterModel != null)
        {
            foreach (var filter in request.FilterModel)
            {
                var colId = filter.Key;
                var filterValue = filter.Value;

                if (filterValue.FilterType == "text" && filterValue.Filter != null)
                {
                    var searchValue = filterValue.Filter.ToString()?.ToLower() ?? "";
                    var filterType = filterValue.Type?.ToLower() ?? "contains";
                    
                    if (filterType == "equals")
                    {
                        query = colId.ToLower() switch
                        {
                            "protocollo" => query.Where(x => x.Protocollo != null && x.Protocollo.ToLower() == searchValue),
                            "coda" => query.Where(x => x.Coda != null && x.Coda.ToLower() == searchValue),
                            "statopratica" => query.Where(x => x.StatoPratica != null && x.StatoPratica.ToLower() == searchValue),
                            "stato" => query.Where(x => x.Stato != null && x.Stato.ToLower() == searchValue),
                            "esito" => query.Where(x => x.Esito != null && x.Esito.ToLower() == searchValue),
                            "statopratica_keplero" => query.Where(x => x.StatoPratica_Keplero.ToLower() == searchValue),
                            _ => query
                        };
                    }
                    else // contains
                    {
                        query = colId.ToLower() switch
                        {
                            "protocollo" => query.Where(x => x.Protocollo != null && x.Protocollo.ToLower().Contains(searchValue)),
                            "coda" => query.Where(x => x.Coda != null && x.Coda.ToLower().Contains(searchValue)),
                            "statopratica" => query.Where(x => x.StatoPratica != null && x.StatoPratica.ToLower().Contains(searchValue)),
                            "stato" => query.Where(x => x.Stato != null && x.Stato.ToLower().Contains(searchValue)),
                            "esito" => query.Where(x => x.Esito != null && x.Esito.ToLower().Contains(searchValue)),
                            "statopratica_keplero" => query.Where(x => x.StatoPratica_Keplero.ToLower().Contains(searchValue)),
                            _ => query
                        };
                    }
                }
                else if (filterValue.FilterType == "number" && filterValue.Filter != null)
                {
                    // Handle both string and numeric values from AG Grid
                    var filterStr = filterValue.Filter.ToString();
                    if (filterStr != null && int.TryParse(filterStr, out var numValue))
                    {
                        var filterType = filterValue.Type?.ToLower() ?? "equals";
                        
                        query = (colId.ToLower(), filterType) switch
                        {
                            ("itemid", "equals") => query.Where(x => x.ItemId == numValue),
                            ("itemid", "notequal") => query.Where(x => x.ItemId != numValue),
                            ("itemid", "lessthan") => query.Where(x => x.ItemId < numValue),
                            ("itemid", "lessthanorequal") => query.Where(x => x.ItemId <= numValue),
                            ("itemid", "greaterthan") => query.Where(x => x.ItemId > numValue),
                            ("itemid", "greaterthanorequal") => query.Where(x => x.ItemId >= numValue),
                            ("riga", "equals") => query.Where(x => x.Riga == numValue),
                            ("riga", "notequal") => query.Where(x => x.Riga != numValue),
                            ("riga", "lessthan") => query.Where(x => x.Riga < numValue),
                            ("riga", "lessthanorequal") => query.Where(x => x.Riga <= numValue),
                            ("riga", "greaterthan") => query.Where(x => x.Riga > numValue),
                            ("riga", "greaterthanorequal") => query.Where(x => x.Riga >= numValue),
                            _ => query
                        };
                    }
                }
            }
        }

        // Apply sorting
        if (request.SortModel != null && request.SortModel.Count > 0)
        {
            var firstSort = request.SortModel[0];
            var isAscending = firstSort.Sort == "asc";

            query = firstSort.ColId.ToLower() switch
            {                
                "itemid" => isAscending ? query.OrderBy(x => x.ItemId) : query.OrderByDescending(x => x.ItemId),
                "protocollo" => isAscending ? query.OrderBy(x => x.Protocollo) : query.OrderByDescending(x => x.Protocollo),
                "coda" => isAscending ? query.OrderBy(x => x.Coda) : query.OrderByDescending(x => x.Coda),
                "statopratica" => isAscending ? query.OrderBy(x => x.StatoPratica) : query.OrderByDescending(x => x.StatoPratica),
                "stato" => isAscending ? query.OrderBy(x => x.Stato) : query.OrderByDescending(x => x.Stato),
                "esito" => isAscending ? query.OrderBy(x => x.Esito) : query.OrderByDescending(x => x.Esito),
                "dataesito" => isAscending ? query.OrderBy(x => x.DataEsito) : query.OrderByDescending(x => x.DataEsito),
                "statopratica_keplero" => isAscending ? query.OrderBy(x => x.StatoPratica_Keplero) : query.OrderByDescending(x => x.StatoPratica_Keplero),
                "riga" => isAscending ? query.OrderBy(x => x.Riga) : query.OrderByDescending(x => x.Riga),
                 _ => isAscending ? query.OrderBy(x => x.Protocollo) : query.OrderByDescending(x => x.Protocollo)
            };
        }
        else
        {
            query = query.OrderBy(x => x.Protocollo);
        }
       
        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var data = await query
            .Skip(request.StartRow)
            .Take(request.EndRow - request.StartRow)
            .ToListAsync();

        return Results.Ok(new
        {
            rowData = data,
            rowCount = totalCount
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Statistics endpoint for Keplero Compare
app.MapGet("/api/keplero-compare/statistics", async (MyDbContext db) =>
{
    try
    {
        var totalRecords = await db.KepleroCompare.CountAsync();
        
        // Group by Coda
        var byCoda = await db.KepleroCompare
            .GroupBy(x => x.Coda)
            .Select(g => new { Coda = g.Key ?? "N/A", Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // Group by StatoPratica
        var byStatoPratica = await db.KepleroCompare
            .GroupBy(x => x.StatoPratica)
            .Select(g => new { StatoPratica = g.Key ?? "N/A", Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // Group by Stato
        var byStato = await db.KepleroCompare
            .GroupBy(x => x.Stato)
            .Select(g => new { Stato = g.Key ?? "N/A", Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // Group by StatoPratica_Keplero
        var byStatoPraticaKeplero = await db.KepleroCompare
            .GroupBy(x => x.StatoPratica_Keplero)
            .Select(g => new { StatoPraticaKeplero = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // Group by Esito
        var byEsito = await db.KepleroCompare
            .GroupBy(x => x.Esito)
            .Select(g => new { Esito = g.Key ?? "N/A", Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return Results.Ok(new
        {
            totalRecords,
            byCoda,
            byStatoPratica,
            byStato,
            byStatoPraticaKeplero,
            byEsito
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Scraper endpoint - Fetch data from Faschim
app.MapGet("/api/scraper/fetch-from-faschim", async (HttpContext context, LiteDatabase liteDb, IConfiguration config, string? filter, string? searchName) =>
{
    try
    {
        // Configure HttpClient with cookie container and extended timeout
        var cookieContainer = new System.Net.CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true
        };
        
        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(10) // 10 minute timeout for large datasets
        };
        
        // Get configuration from appsettings
        var baseUri = config["FaschimApi:BaseUri"];
        var loginPath = config["FaschimApi:LoginPath"];
        var dataPath = config["FaschimApi:DataPath"];
        var userName = config["FaschimApi:UserName"];
        var password = config["FaschimApi:Password"];
        
        if (string.IsNullOrEmpty(baseUri) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            return Results.Problem("Faschim API configuration is missing", statusCode: 500);
        }
        
        // Step 1: Authenticate using form-urlencoded
        var loginUrl = $"{baseUri}{loginPath}";
        var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", userName },
            { "password", password },
            { "interni", "false" }
        });
        
        var loginResponse = await httpClient.PostAsync(loginUrl, loginContent);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            return Results.Problem($"Authentication failed: {loginResponse.StatusCode} - {errorContent}", statusCode: 401);
        }
        
        // Login successful - cookies are automatically handled by HttpClient
        var loginResult = await loginResponse.Content.ReadAsStringAsync();
        
        // Step 2: Fetch data with filter - cookies/session are maintained in httpClient
        var dataUrl = $"{baseUri}{dataPath}";
        
        // Get LiteDB collections
        var searchesCollection = liteDb.GetCollection<ScraperSearch>("scraper_searches");
        var dataRecordsCollection = liteDb.GetCollection<ScraperDataRecord>("scraper_data_records");
        
        // Set extraction date for this download
        var extractionDate = DateTime.Today.ToString("dd-MM-yyyy");
        
        var fetchDate = DateTime.Now;
        var processedFilter = string.IsNullOrEmpty(filter) ? "" : filter.Replace("${DateTime.Today.AddDays(-1).ToString(\"yyyy-MM-dd\")}", 
            DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"));
        
        // Step 1: Fetch first page to get total count
        var limitPerPage = 5000;
        var queryParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
        queryParams.Add("_dc", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        queryParams.Add("page", "1");
        queryParams.Add("start", "0");
        queryParams.Add("limit", limitPerPage.ToString());
        queryParams.Add("sort", "[{\"property\":\"Pratica.dataPresentazione\",\"direction\":\"ASC\"},{\"property\":\"Pratica.numeroProtocollo\",\"direction\":\"ASC\"}]");
        if (!string.IsNullOrEmpty(processedFilter))
        {
            queryParams.Add("filter", processedFilter);
        }
        
        var firstPageUrl = dataUrl + "?" + queryParams.ToString();
        var firstPageResponse = await httpClient.GetAsync(firstPageUrl);
        
        if (!firstPageResponse.IsSuccessStatusCode)
        {
            var errorContent = await firstPageResponse.Content.ReadAsStringAsync();
            return Results.Problem($"Data fetch failed: {firstPageResponse.StatusCode} - {errorContent}", statusCode: 500);
        }
        
        var firstPageJson = await firstPageResponse.Content.ReadAsStringAsync();
        var firstPageData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(firstPageJson);
        
        // Parse metadata from first page
        bool success = false;
        int codErrore = 0;
        string message = "";
        string debugMessage = "";
        int totalRecords = 0;
        
        if (firstPageData.TryGetProperty("success", out var successElement))
        {
            if (successElement.ValueKind == JsonValueKind.True)
                success = true;
            else if (successElement.ValueKind == JsonValueKind.False)
                success = false;
            else if (successElement.ValueKind == JsonValueKind.Number)
                success = successElement.GetInt32() != 0;
        }
        if (firstPageData.TryGetProperty("codErrore", out var codErroreElement))
            codErrore = codErroreElement.GetInt32();
        if (firstPageData.TryGetProperty("message", out var messageElement))
            message = messageElement.GetString() ?? "";
        if (firstPageData.TryGetProperty("debugMessage", out var debugMessageElement))
            debugMessage = debugMessageElement.GetString() ?? "";
        if (firstPageData.TryGetProperty("total", out var totalElement))
            totalRecords = totalElement.GetInt32();
        
        // Calculate total pages
        var totalPages = (int)Math.Ceiling((double)totalRecords / limitPerPage);
        
        // Create search metadata record
        var searchRecord = new ScraperSearch
        {
            SearchName = searchName ?? "Unknown",
            FilterUsed = processedFilter,
            FetchDate = fetchDate,
            Success = success,
            CodErrore = codErrore,
            Message = message,
            DebugMessage = debugMessage,
            TotalRecords = totalRecords,
            RecordsFetched = 0
        };
        
        // Insert search record and get the generated ID
        searchesCollection.Insert(searchRecord);
        var searchId = searchRecord.Id; // ID is auto-populated after insert
        
        // Collect all data records from all pages
        var allDataRecords = new List<ScraperDataRecord>();
        
        // Process first page data
        if (firstPageData.TryGetProperty("data", out var firstDataArray) && firstDataArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in firstDataArray.EnumerateArray())
            {
                string? hasConvenzioneFondo = null;
                if (item.TryGetProperty("hasConvenzioneFondo", out var convElement))
                {
                    hasConvenzioneFondo = convElement.ToString();
                }
                
                var dataRecord = new ScraperDataRecord
                {
                    SearchId = searchId,
                    HasConvenzioneFondo = hasConvenzioneFondo,
                    RawContent = item.GetRawText(),
                    ExtractionDate = extractionDate
                };
                allDataRecords.Add(dataRecord);
            }
        }
        
        // Fetch remaining pages if needed
        for (int page = 2; page <= totalPages; page++)
        {
            var start = (page - 1) * limitPerPage;
            var pageQueryParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
            pageQueryParams.Add("_dc", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
            pageQueryParams.Add("page", page.ToString());
            pageQueryParams.Add("start", start.ToString());
            pageQueryParams.Add("limit", limitPerPage.ToString());
            pageQueryParams.Add("sort", "[{\"property\":\"Pratica.dataPresentazione\",\"direction\":\"ASC\"},{\"property\":\"Pratica.numeroProtocollo\",\"direction\":\"ASC\"}]");
            if (!string.IsNullOrEmpty(processedFilter))
            {
                pageQueryParams.Add("filter", processedFilter);
            }
            
            var pageUrl = dataUrl + "?" + pageQueryParams.ToString();
            var pageResponse = await httpClient.GetAsync(pageUrl);
            
            if (!pageResponse.IsSuccessStatusCode)
            {
                var errorContent = await pageResponse.Content.ReadAsStringAsync();
                return Results.Problem($"Data fetch failed on page {page}: {pageResponse.StatusCode} - {errorContent}", statusCode: 500);
            }
            
            var pageJson = await pageResponse.Content.ReadAsStringAsync();
            var pageData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(pageJson);
            
            // Process page data
            if (pageData.TryGetProperty("data", out var pageDataArray) && pageDataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in pageDataArray.EnumerateArray())
                {
                    string? hasConvenzioneFondo = null;
                    if (item.TryGetProperty("hasConvenzioneFondo", out var convElement))
                    {
                        hasConvenzioneFondo = convElement.ToString();
                    }
                    
                    var dataRecord = new ScraperDataRecord
                    {
                        SearchId = searchId,
                        HasConvenzioneFondo = hasConvenzioneFondo,
                        RawContent = item.GetRawText(),
                        ExtractionDate = extractionDate
                    };
                    allDataRecords.Add(dataRecord);
                }
            }
            
            // Small delay to avoid overwhelming the API
            await Task.Delay(500);
        }
        
        // Bulk insert all data records
        if (allDataRecords.Count > 0)
        {
            dataRecordsCollection.InsertBulk(allDataRecords);
        }
        
        // Update search record with actual fetched count
        searchRecord.RecordsFetched = allDataRecords.Count;
        searchesCollection.Update(searchRecord);
        
        return Results.Ok(new 
        { 
            success = true, 
            searchId = searchId,
            searchName = searchName,
            totalRecords = totalRecords,
            recordsFetched = allDataRecords.Count,
            totalPages = totalPages,
            faschimSuccess = success,
            faschimCodErrore = codErrore,
            faschimMessage = message
        });
    }
    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
    {
        return Results.Problem($"Request timeout: The Faschim API took too long to respond. Try with a more specific filter or try again later.", statusCode: 504);
    }
    catch (TaskCanceledException ex)
    {
        return Results.Problem($"Request timeout: {ex.Message}", statusCode: 504);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"HTTP request failed: {ex.Message}. Stack: {ex.StackTrace}", statusCode: 500);
    }
    catch (Exception ex)
    {
        var innerMsg = ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "";
        return Results.Problem(detail: $"{ex.Message}{innerMsg}. Stack: {ex.StackTrace}", statusCode: 500);
    }
});

// Get all scraper data from LiteDB
app.MapGet("/api/scraper/data", (LiteDatabase liteDb) =>
{
    try
    {
        var searchesCollection = liteDb.GetCollection<ScraperSearch>("scraper_searches");
        var dataRecordsCollection = liteDb.GetCollection<ScraperDataRecord>("scraper_data_records");
        
        // Get all searches
        var searches = searchesCollection.FindAll().OrderByDescending(x => x.FetchDate).ToList();
        
        // Get all data records
        var dataRecords = dataRecordsCollection.FindAll().ToList();
        
        // Build response with searches and their related data
        var result = searches.Select(search => new
        {
            search.Id,
            search.SearchName,
            search.FilterUsed,
            search.FetchDate,
            search.Success,
            search.CodErrore,
            search.Message,
            search.DebugMessage,
            search.TotalRecords,
            search.RecordsFetched,
            DataRecords = dataRecords
                .Where(dr => dr.SearchId == search.Id)
                .Select(dr => new
                {
                    dr.Id,
                    dr.SearchId,
                    dr.HasConvenzioneFondo,
                    dr.RawContent,
                    dr.ExtractionDate
                })
                .ToList()
        }).ToList();
        
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Delete scraper data by record IDs
app.MapPost("/api/scraper/data/delete", async (LiteDatabase liteDb, HttpRequest request) =>
{
    try
    {
        var dataRecordsCollection = liteDb.GetCollection<ScraperDataRecord>("scraper_data_records");
        
        // Read the JSON body
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var ids = System.Text.Json.JsonSerializer.Deserialize<int[]>(body, options);
        
        if (ids == null || ids.Length == 0)
        {
            return Results.BadRequest(new { success = false, message = "No record IDs provided" });
        }
        
        int deletedCount = 0;
        foreach (var id in ids)
        {
            if (dataRecordsCollection.Delete(id))
            {
                deletedCount++;
            }
        }
        
        return Results.Ok(new { success = true, deletedCount = deletedCount });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Endpoint to get mismatch records from stored procedure
app.MapGet("/api/keplero-compare/mismatch", async (MyDbContext db, IConfiguration config) =>
{
    try
    {
        var storedProcName = config["KepleroCompare:MismatchStoredProcedure"] ?? "get_keplero_compare_mismatch";
        
        // Call stored procedure - assuming it returns protocollo values
        var protocollos = await db.Database
            .SqlQueryRaw<string>($"EXEC {storedProcName}")
            .ToListAsync();
        
        return Results.Ok(new { success = true, protocollos = protocollos, count = protocollos.Count });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error executing mismatch stored procedure: {ex.Message}");
    }
});

app.Run();
