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
                    var searchValue = filterValue.Filter;
                    query = colId switch
                    {
                        "utenteLiquidatore" => query.Where(x => x.UtenteLiquidatore != null && x.UtenteLiquidatore.Contains(searchValue)),
                        "numeroProtocollo" => query.Where(x => x.NumeroProtocollo.Contains(searchValue)),
                        "dataPresentazione" => query.Where(x => x.DataPresentazione.Contains(searchValue)),
                        "dataInserimento" => query.Where(x => x.DataInserimento.Contains(searchValue)),
                        "modified" => query.Where(x => x.Modified.Contains(searchValue)),
                        "descrizioneGruppoTariffa" => query.Where(x => x.DescrizioneGruppoTariffa != null && x.DescrizioneGruppoTariffa.Contains(searchValue)),
                        "formaAssistenza" => query.Where(x => x.FormaAssistenza.Contains(searchValue)),
                        "importoRichiesto" => query.Where(x => x.ImportoRichiesto != null && x.ImportoRichiesto.Contains(searchValue)),
                        "importoRiconosciuto" => query.Where(x => x.ImportoRiconosciuto != null && x.ImportoRiconosciuto.Contains(searchValue)),
                        "dataPagamento" => query.Where(x => x.DataPagamento != null && x.DataPagamento.Contains(searchValue)),
                        "cognomePersona" => query.Where(x => x.CognomePersona.Contains(searchValue)),
                        "nomePersona" => query.Where(x => x.NomePersona.Contains(searchValue)),
                        "cognomeBeneficiario" => query.Where(x => x.CognomeBeneficiario.Contains(searchValue)),
                        "nomeBeneficiario" => query.Where(x => x.NomeBeneficiario.Contains(searchValue)),
                        "unisalInviato" => query.Where(x => x.UnisalInviato != null && x.UnisalInviato.Contains(searchValue)),
                        "statoPratica" => query.Where(x => x.StatoPratica.Contains(searchValue)),
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

app.Run();
