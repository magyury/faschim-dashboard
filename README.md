# FaschimPivotApp

Dashboard application for visualizing millions of rows from Azure SQL Server using Server-Side Row Model (SSRM).

## Stack

- **Backend**: .NET 9 Minimal API + EF Core + SQL Server
- **Frontend**: Vue 3 + Vite + AG-Grid Enterprise (SSRM)

## Project Structure

```
FaschimPivotApp/
├── Backend/                  # .NET 9 Minimal API
│   ├── Program.cs            # All endpoints: /api/pivot-data, CORS, EF setup
│   ├── Models/               # PivotRequest.cs, SQL table entities
│   ├── Data/                 # MyDbContext.cs (EF Core SQL Server)
│   ├── appsettings.json      # SQL Azure connection string
│   └── FaschimPivotApp.Backend.csproj
├── Frontend/                 # Vue 3 + Vite + ag-Grid Enterprise
│   ├── src/
│   │   ├── components/       # PivotGrid.vue (ag-grid-vue SSRM)
│   │   ├── views/            # HomeView.vue
│   │   ├── services/         # api.js (axios to /api/pivot-data)
│   │   └── App.vue
│   ├── vite.config.js        # proxy: { '/api': 'http://localhost:5000' }
│   ├── package.json          # ag-grid-enterprise, @ag-grid-vue3
│   └── index.html
├── .gitignore
└── README.md
```

## Getting Started

### Prerequisites

- .NET 9 SDK
- Node.js 18+ and npm
- Azure SQL Server instance
- AG-Grid Enterprise license (for production)

### Setup

1. **Configure Database Connection**
   
   Edit `Backend/appsettings.json` and update the connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-database;..."
     }
   }
   ```

2. **Update Entity Models**
   
   Edit `Backend/Models/YourTableEntity.cs` to match your SQL table schema
   Edit `Backend/Data/MyDbContext.cs` to configure entity mappings

3. **Update Grid Columns**
   
   Edit `Frontend/src/components/PivotGrid.vue` columnDefs to match your table columns

### Running Locally

**Terminal 1 - Backend:**
```powershell
cd Backend
dotnet restore
dotnet run
# Backend runs on http://localhost:5000
```

**Terminal 2 - Frontend:**
```powershell
cd Frontend
npm install
npm run dev
# Frontend runs on http://localhost:5173
```

Open browser at http://localhost:5173

### Development Commands

**Backend:**
```powershell
cd Backend
dotnet restore          # Restore packages
dotnet build            # Build project
dotnet run              # Run development server
dotnet ef migrations add InitialCreate  # Add migration
dotnet ef database update               # Apply migration
```

**Frontend:**
```powershell
cd Frontend
npm install             # Install dependencies
npm run dev             # Run development server
npm run build           # Build for production
npm run preview         # Preview production build
```

## Features

- ✅ Server-Side Row Model (SSRM) for handling millions of rows
- ✅ Dynamic filtering and sorting
- ✅ Pagination (100 rows per page, 10 blocks cache)
- ✅ EF Core integration with SQL Server
- ✅ CORS configured for local development
- ✅ No Swashbuckle dependency (minimal API)

## Next Steps

1. Configure your actual SQL table entity in `Backend/Models/YourTableEntity.cs`
2. Update DbContext in `Backend/Data/MyDbContext.cs`
3. Implement filtering and sorting logic in `Backend/Program.cs`
4. Add AG-Grid Enterprise license key in `Frontend/src/main.js`
5. Customize grid columns in `Frontend/src/components/PivotGrid.vue`

## Deployment

For Azure Static Web Apps deployment, configure the `staticwebapp.config.json` with API routes pointing to your backend Azure App Service or Function.

## License

Private
