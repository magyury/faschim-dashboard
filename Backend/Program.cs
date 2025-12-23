using Microsoft.EntityFrameworkCore;
using FaschimPivotApp.Backend.Data;
using FaschimPivotApp.Backend.Models;
using System.Text.Json;

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

var app = builder.Build();

app.UseCors("AllowFrontend");

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

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

                if (filterValue.FilterType == "text" && !string.IsNullOrEmpty(filterValue.Filter))
                {
                    var searchValue = filterValue.Filter.ToLower();
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

                if (filterValue.FilterType == "text" && !string.IsNullOrEmpty(filterValue.Filter))
                {
                    var searchValue = filterValue.Filter.ToLower();
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

                if (filterValue.FilterType == "text" && !string.IsNullOrEmpty(filterValue.Filter))
                {
                    var searchValue = filterValue.Filter.ToLower();
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
                "id" => isAscending ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id),
                "column1" => isAscending ? query.OrderBy(x => x.Column1) : query.OrderByDescending(x => x.Column1),
                "column2" => isAscending ? query.OrderBy(x => x.Column2) : query.OrderByDescending(x => x.Column2),
                _ => query.OrderBy(x => x.Id)
            };
        }
        else
        {
            query = query.OrderBy(x => x.Id);
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

                if (filterValue.FilterType == "text" && !string.IsNullOrEmpty(filterValue.Filter))
                {
                    var searchValue = filterValue.Filter.ToLower();
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
                else if (filterValue.FilterType == "number" && filterValue.Filter != null)
                {
                    if (int.TryParse(filterValue.Filter, out var numValue))
                    {
                        query = colId.ToLower() switch
                        {
                            "itemid" => query.Where(x => x.ItemId == numValue),
                            "riga" => query.Where(x => x.Riga == numValue),
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
                "id" => isAscending ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id),
                "itemid" => isAscending ? query.OrderBy(x => x.ItemId) : query.OrderByDescending(x => x.ItemId),
                "protocollo" => isAscending ? query.OrderBy(x => x.Protocollo) : query.OrderByDescending(x => x.Protocollo),
                "coda" => isAscending ? query.OrderBy(x => x.Coda) : query.OrderByDescending(x => x.Coda),
                "statopratica" => isAscending ? query.OrderBy(x => x.StatoPratica) : query.OrderByDescending(x => x.StatoPratica),
                "stato" => isAscending ? query.OrderBy(x => x.Stato) : query.OrderByDescending(x => x.Stato),
                "esito" => isAscending ? query.OrderBy(x => x.Esito) : query.OrderByDescending(x => x.Esito),
                "dataesito" => isAscending ? query.OrderBy(x => x.DataEsito) : query.OrderByDescending(x => x.DataEsito),
                "statopratica_keplero" => isAscending ? query.OrderBy(x => x.StatoPratica_Keplero) : query.OrderByDescending(x => x.StatoPratica_Keplero),
                "riga" => isAscending ? query.OrderBy(x => x.Riga) : query.OrderByDescending(x => x.Riga),
                _ => isAscending ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id)
            };
        }
        else
        {
            query = query.OrderBy(x => x.Id);
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

app.Run();
