<template>
  <div class="pivot-grid">
    <div class="grid-header">
      <div class="header-left">
        <h2>Faschim Pivot Dashboard</h2>
        <button @click="toggleGrouped" class="group-toggle-btn">
          <TableCellsIcon v-if="isGrouped" class="icon" />
          <ChartBarIcon v-else class="icon" />
          {{ isGrouped ? 'Show All Rows' : 'Group by Protocollo' }}
        </button>
      </div>
      <div v-if="totalRows !== null" class="row-count">
        <span>Loaded: <strong>{{ loadedRows.toLocaleString() }}</strong></span>
        <span class="separator">|</span>
        <span>Total: <strong>{{ totalRows.toLocaleString() }}</strong></span>
      </div>
    </div>

    <!-- Search Form -->
    <div class="search-form">
      <div class="search-fields">
        <div class="search-field">
          <label for="search-protocollo">Protocollo</label>
          <input
            id="search-protocollo"
            v-model="searchFilters.protocollo"
            type="text"
            placeholder="Search by Protocollo..."
            @keyup.enter="handleSearch"
          />
        </div>
        <div class="search-field">
          <label for="search-stato-pratica">Stato Pratica</label>
          <select
            id="search-stato-pratica"
            v-model="searchFilters.statoPratica"
            @change="handleSearch"
          >
            <option value="">All</option>
            <option v-for="stato in statoPraticaOptions" :key="stato" :value="stato">
              {{ stato }}
            </option>
          </select>
        </div>
        <div class="search-field">
          <label for="search-inserimento">Inserimento</label>
          <input
            id="search-inserimento"
            v-model="searchFilters.inserimento"
            type="date"
            @change="handleSearch"
          />
        </div>
        <div class="search-field">
          <label for="search-modified">Modificato</label>
          <input
            id="search-modified"
            v-model="searchFilters.modified"
            type="date"
            @change="handleSearch"
          />
        </div>
      </div>
      <div class="search-actions">
        <button @click="handleSearch" class="btn-search">
          <MagnifyingGlassIcon class="icon" />
          Search
        </button>
        <button @click="clearSearch" class="btn-clear">
          <span class="icon-text">✖</span>
          Clear
        </button>
      </div>
    </div>

    <ag-grid-vue
      :class="isDarkMode ? 'ag-theme-alpine-dark' : 'ag-theme-alpine'"
      :columnDefs="isGrouped ? groupedColumnDefs : columnDefs"
      :defaultColDef="defaultColDef"
      :rowModelType="'infinite'"
      :datasource="datasource"
      :cacheBlockSize="100"
      :cacheOverflowSize="2"
      :maxConcurrentDatasourceRequests="2"
      :infiniteInitialRowCount="1000"
      :maxBlocksInCache="10"
      :enableCellTextSelection="true"
      :ensureDomOrder="true"
      @grid-ready="onGridReady"
      @cell-double-clicked="onCellDoubleClicked"
      style="height: 500px; min-height: 400px;"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { AgGridVue } from 'ag-grid-vue3'
import { fetchPivotData, fetchStatoPraticaValues } from '@/services/api'
import type { ColDef, GridApi, GridReadyEvent, IDatasource } from 'ag-grid-community'
import { useDarkMode } from '@/composables/useDarkMode'
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

const { isDarkMode } = useDarkMode()

const gridApi = ref<GridApi | null>(null)
const totalRows = ref<number | null>(null)
const loadedRows = ref<number>(0)
const isGrouped = ref<boolean>(false)

const searchFilters = ref({
  protocollo: '',
  statoPratica: '',
  inserimento: '',
  modified: ''
})

const statoPraticaOptions = ref<string[]>([])

const groupedColumnDefs = ref<ColDef[]>([
  { 
    field: 'numeroProtocollo', 
    headerName: 'Protocollo', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    pinned: 'left',
    width: 150
  },
  { 
    field: 'count', 
    headerName: 'N° Righe', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    width: 100
  },
  { 
    field: 'utenteLiquidatore', 
    headerName: 'Utente Liquidatore', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'dataPresentazione', 
    headerName: 'Presentazione', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'formaAssistenza', 
    headerName: 'Forma Assistenza', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'statoPratica', 
    headerName: 'Stato Pratica', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  }
])

const columnDefs = ref<ColDef[]>([
  { 
    field: 'numeroProtocollo', 
    headerName: 'Protocollo', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 150,
    pinned: 'left'
  },
  { 
    field: 'utenteLiquidatore', 
    headerName: 'Utente Liquidatore', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'dataPresentazione', 
    headerName: 'Presentazione', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'dataInserimento', 
    headerName: 'Inserimento', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'modified', 
    headerName: 'Modified', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'formaAssistenza', 
    headerName: 'Forma Assistenza', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'cognomePersona', 
    headerName: 'Cognome Persona', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'nomePersona', 
    headerName: 'Nome Persona', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'cognomeBeneficiario', 
    headerName: 'Cognome Beneficiario', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'nomeBeneficiario', 
    headerName: 'Nome Beneficiario', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'statoPratica', 
    headerName: 'Stato Pratica', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  }
])

const defaultColDef = ref<ColDef>({
  flex: 1,
  minWidth: 100,
  resizable: true,
  sortable: true,
  filter: true,
  enableCellTextSelection: true
})

// Create Infinite Row Model datasource (Community edition compatible)
const datasource = ref<IDatasource>({
  rowCount: undefined, // Will be set when we get data
  getRows: async (params) => {
    try {
      console.log('Fetching rows from', params.startRow, 'to', params.endRow)
      
      // Build filter model with search filters
      const filterModel = params.filterModel || {}
      
      // Add search filters if they exist
      if (searchFilters.value.protocollo) {
        filterModel['numeroProtocollo'] = {
          filterType: 'text',
          type: 'contains',
          filter: searchFilters.value.protocollo
        }
      }
      if (searchFilters.value.statoPratica) {
        filterModel['statoPratica'] = {
          filterType: 'text',
          type: 'contains',
          filter: searchFilters.value.statoPratica
        }
      }
      if (searchFilters.value.inserimento) {
        filterModel['dataInserimento'] = {
          filterType: 'text',
          type: 'contains',
          filter: searchFilters.value.inserimento
        }
      }
      if (searchFilters.value.modified) {
        filterModel['modified'] = {
          filterType: 'text',
          type: 'contains',
          filter: searchFilters.value.modified
        }
      }
      
      const request = {
        startRow: params.startRow!,
        endRow: params.endRow!,
        sortModel: params.sortModel,
        filterModel: filterModel
      }

      // Use different endpoint based on grouped state
      const url = isGrouped.value ? 'http://localhost:5000/api/pivot-data-grouped' : 'http://localhost:5000/api/pivot-data'
      
      const fetchResponse = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
      })
      
      const response = await fetchResponse.json()
      
      // Update total rows count
      if (response.rowCount !== undefined) {
        totalRows.value = response.rowCount
      }
      
      // Update loaded rows count
      loadedRows.value = params.startRow! + response.rowData.length
      
      // Calculate last row
      let lastRow = -1
      if (response.rowData.length < (params.endRow! - params.startRow!)) {
        lastRow = params.startRow! + response.rowData.length
      } else if (response.rowCount !== undefined) {
        lastRow = response.rowCount
      }

      params.successCallback(response.rowData, lastRow)
      
      console.log('Loaded', response.rowData.length, 'rows. Total:', response.rowCount)
    } catch (error) {
      console.error('Error fetching data:', error)
      params.failCallback()
    }
  }
})

const toggleGrouped = () => {
  isGrouped.value = !isGrouped.value
  // Reset and reload grid
  if (gridApi.value) {
    totalRows.value = null
    loadedRows.value = 0
    gridApi.value.setGridOption('datasource', datasource.value)
  }
}

const handleSearch = () => {
  if (gridApi.value) {
    totalRows.value = null
    loadedRows.value = 0
    gridApi.value.setGridOption('datasource', datasource.value)
    console.log('Search filters applied:', searchFilters.value)
  }
}

const clearSearch = () => {
  searchFilters.value = {
    protocollo: '',
    statoPratica: '',
    inserimento: '',
    modified: ''
  }
  if (gridApi.value) {
    totalRows.value = null
    loadedRows.value = 0
    gridApi.value.setGridOption('datasource', datasource.value)
    console.log('Search filters cleared')
  }
}

const onCellDoubleClicked = (event: any) => {
  // Check if the double-clicked cell is in the Protocollo column
  if (event.column.colId === 'numeroProtocollo') {
    const protocolloValue = event.value
    if (protocolloValue) {
      searchFilters.value.protocollo = protocolloValue
      console.log('Protocollo field populated with:', protocolloValue)
      // Automatically trigger search
      handleSearch()
    }
  }
}

const onGridReady = (params: GridReadyEvent) => {
  gridApi.value = params.api
  console.log('Grid ready with Infinite Row Model (AG Grid Community)')
}

onMounted(async () => {
  try {
    statoPraticaOptions.value = await fetchStatoPraticaValues()
    console.log('Loaded StatoPratica options:', statoPraticaOptions.value.length)
  } catch (error) {
    console.error('Error loading StatoPratica options:', error)
  }
})
</script>

<style scoped>
.pivot-grid {
  width: 100%;
  height: 100%;
  overflow-y: auto;
}

.grid-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
  padding: 10px 0;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 20px;
}

.grid-header h2 {
  margin: 0;
  font-size: 1.5rem;
  color: #333;
}

.group-toggle-btn {
  padding: 8px 16px;
  background-color: #1976d2;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  transition: background-color 0.2s;
}

.group-toggle-btn:hover {
  background-color: #1565c0;
}

.group-toggle-btn:active {
  background-color: #0d47a1;
}

.search-form {
  background: #f8f9fa;
  padding: 20px;
  border-radius: 8px;
  margin-bottom: 20px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.search-fields {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 15px;
  margin-bottom: 15px;
}

.search-field {
  display: flex;
  flex-direction: column;
}

.search-field label {
  font-size: 0.85rem;
  font-weight: 600;
  color: #555;
  margin-bottom: 5px;
}

.search-field input {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.9rem;
  transition: border-color 0.2s;
}

.search-field select {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.9rem;
  transition: border-color 0.2s;
  background-color: white;
  cursor: pointer;
}

.search-field input:focus,
.search-field select:focus {
  outline: none;
  border-color: #1976d2;
  box-shadow: 0 0 0 2px rgba(25, 118, 210, 0.1);
}

.search-actions {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
}

.btn-search,
.btn-clear {
  padding: 8px 20px;
  border: none;
  border-radius: 4px;
  font-size: 0.9rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-search {
  background-color: #1976d2;
  color: white;
}

.btn-search:hover {
  background-color: #1565c0;
}

.btn-clear {
  background-color: #666;
  color: white;
}

.btn-clear:hover {
  background-color: #555;
}

.row-count {
  font-size: 1rem;
  color: #666;
  display: flex;
  gap: 10px;
  align-items: center;
}

.row-count .separator {
  color: #ccc;
}

.row-count strong {
  color: #1976d2;
  font-size: 1.1rem;
}

.ag-theme-alpine {
  width: 100%;
  height: 100%;
}
</style>
