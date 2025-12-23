# Two Tables Setup Complete! 🎉

## What's Been Created

### Backend (.NET)
- ✅ **Two table entities:**
  - `FullKeplero` → `FullKeplero_Backup` table
  - `SecondTable` → `YourSecondTableName` table
  
- ✅ **Two API endpoints:**
  - `POST /api/pivot-data` → First table
  - `POST /api/pivot-data-second` → Second table

### Frontend (Vue)
- ✅ **Two grid components:**
  - `PivotGrid.vue` → First table
  - `SecondTableGrid.vue` → Second table
  
- ✅ **Two views:**
  - `/` → FullKeplero Table
  - `/second-table` → Second Table

- ✅ **Navigation:** Tab links in header to switch between tables

## Configuration Needed

### 1. Update Second Table Name
Edit `Backend/appsettings.json`:
```json
"Database": {
  "TableName": "FullKeplero_Backup",
  "SecondTableName": "YOUR_ACTUAL_SECOND_TABLE_NAME"  ← Change this
}
```

### 2. Update SecondTable Entity
Edit `Backend/Models/SecondTable.cs` to match your actual table schema:
```csharp
public class SecondTable
{
    public int Id { get; set; }
    public string? YourColumn1 { get; set; }
    public DateTime? YourColumn2 { get; set; }
    // Add your actual columns
}
```

### 3. Update SecondTable Filters/Sorts
Edit `Backend/Program.cs` in the `/api/pivot-data-second` endpoint around line 230:
- Update the `switch` statement in filtering section
- Update the `switch` statement in sorting section
- Match the column names from your SecondTable entity

### 4. Update Frontend Column Definitions
Edit `Frontend/src/components/SecondTableGrid.vue` around line 30:
```typescript
const columnDefs = ref<ColDef[]>([
  { field: 'id', headerName: 'ID' },
  { field: 'yourColumn1', headerName: 'Your Column 1' },
  // Match your SecondTable properties
])
```

## Running the Project

**Terminal 1 - Backend:**
```powershell
cd Backend
dotnet run
```

**Terminal 2 - Frontend:**
```powershell
cd Frontend
npm install
npm run dev
```

## Testing

1. **Open:** http://localhost:5173
2. **You should see:** Navigation with "FullKeplero Table" and "Second Table" links
3. **Click "FullKeplero Table"** → Should show first table data
4. **Click "Second Table"** → Should show second table data

## API Endpoints

```http
### Health Check
GET http://localhost:5000/api/health

### First Table
POST http://localhost:5000/api/pivot-data
Content-Type: application/json

{
  "startRow": 0,
  "endRow": 100,
  "sortModel": [],
  "filterModel": {}
}

### Second Table
POST http://localhost:5000/api/pivot-data-second
Content-Type: application/json

{
  "startRow": 0,
  "endRow": 100,
  "sortModel": [],
  "filterModel": {}
}
```

## Troubleshooting

### Backend errors:
- Check your `SecondTableName` in appsettings.json
- Verify the table exists in your database
- Check that SecondTable entity matches your SQL table schema

### Frontend errors:
- Run `npm install` if you see missing dependencies
- Check browser console for API errors
- Verify Vite proxy is forwarding to correct backend port

## Next Steps to Customize Second Table

1. Get your second table schema from SQL Server
2. Update `Backend/Models/SecondTable.cs` with actual columns
3. Update filtering logic in `Backend/Program.cs` → `/api/pivot-data-second`
4. Update sorting logic in `Backend/Program.cs` → `/api/pivot-data-second`
5. Update `Frontend/src/components/SecondTableGrid.vue` columnDefs
6. Test scrolling, filtering, sorting for both tables!
