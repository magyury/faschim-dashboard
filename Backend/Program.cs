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
        policy.WithOrigins("http://localhost:5173", "http://localhost:4280")
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

app.Run();
