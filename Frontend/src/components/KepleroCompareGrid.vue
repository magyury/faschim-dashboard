<template>
  <div class="keplero-compare-grid">
    <!-- Statistics Cards -->
    <div class="statistics-section" v-if="statistics && !loadingStats">
      <h2>Summary Statistics</h2>
      
      <div class="stats-grid">
        <!-- Total Records -->
        <div class="stat-card total">
          <h3>Total Records</h3>
          <div class="stat-value">{{ statistics.totalRecords.toLocaleString() }}</div>
        </div>

        <!-- By Coda -->
        <div class="stat-card">
          <h3>By Coda</h3>
          <div class="stat-list">
            <div v-for="item in statistics.byCoda.slice(0, 5)" :key="item.coda" class="stat-item">
              <span class="stat-label">{{ item.coda }}</span>
              <span class="stat-count">{{ item.count.toLocaleString() }}</span>
            </div>
          </div>
        </div>

        <!-- By Stato Pratica -->
        <div class="stat-card">
          <h3>By Stato Pratica</h3>
          <div class="stat-list">
            <div v-for="item in statistics.byStatoPratica.slice(0, 5)" :key="item.statoPratica" class="stat-item">
              <span class="stat-label">{{ item.statoPratica }}</span>
              <span class="stat-count">{{ item.count.toLocaleString() }}</span>
            </div>
          </div>
        </div>

        <!-- By Stato -->
        <div class="stat-card">
          <h3>By Stato</h3>
          <div class="stat-list">
            <div v-for="item in statistics.byStato.slice(0, 5)" :key="item.stato" class="stat-item">
              <span class="stat-label">{{ item.stato }}</span>
              <span class="stat-count">{{ item.count.toLocaleString() }}</span>
            </div>
          </div>
        </div>

        <!-- By Stato Pratica Keplero -->
        <div class="stat-card">
          <h3>By Stato Pratica Keplero</h3>
          <div class="stat-list">
            <div v-for="item in statistics.byStatoPraticaKeplero.slice(0, 5)" :key="item.statoPraticaKeplero" class="stat-item">
              <span class="stat-label">{{ item.statoPraticaKeplero }}</span>
              <span class="stat-count">{{ item.count.toLocaleString() }}</span>
            </div>
          </div>
        </div>

        <!-- By Esito -->
        <div class="stat-card">
          <h3>By Esito</h3>
          <div class="stat-list">
            <div v-for="item in statistics.byEsito.slice(0, 5)" :key="item.esito" class="stat-item">
              <span class="stat-label">{{ item.esito }}</span>
              <span class="stat-count">{{ item.count.toLocaleString() }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Loading indicator -->
    <div v-if="loadingStats" class="loading-stats">
      Loading statistics...
    </div>

    <!-- Search Form -->
    <div class="search-form">
      <div class="search-fields">
        <div class="search-field">
          <label for="search-itemid">Item ID</label>
          <input
            id="search-itemid"
            v-model="searchFilters.itemId"
            type="number"
            placeholder="Search by Item ID..."
            @keyup.enter="handleSearch"
          />
        </div>
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
          <label for="search-coda">Coda</label>
          <select
            id="search-coda"
            v-model="searchFilters.coda"
            @change="handleSearch"
          >
            <option value="">All</option>
            <option v-for="coda in codaOptions" :key="coda" :value="coda">
              {{ coda }}
            </option>
          </select>
        </div>
        <div class="search-field">
          <label for="search-stato">Stato</label>
          <select
            id="search-stato"
            v-model="searchFilters.stato"
            @change="handleSearch"
          >
            <option value="">All</option>
            <option v-for="stato in statoOptions" :key="stato" :value="stato">
              {{ stato }}
            </option>
          </select>
        </div>
        <div class="search-field">
          <label for="search-esito">Esito</label>
          <select
            id="search-esito"
            v-model="searchFilters.esito"
            @change="handleSearch"
          >
            <option value="">All</option>
            <option v-for="esito in esitoOptions" :key="esito" :value="esito">
              {{ esito }}
            </option>
          </select>
        </div>
        <div class="search-field">
          <label for="search-dataesito">Data Esito</label>
          <input
            id="search-dataesito"
            v-model="searchFilters.dataEsito"
            type="date"
            @change="handleSearch"
          />
        </div>
        <div class="search-field">
          <label for="search-stato-keplero">Stato Keplero</label>
          <select
            id="search-stato-keplero"
            v-model="searchFilters.statoKeplero"
            @change="handleSearch"
          >
            <option value="">All</option>
            <option v-for="statoKep in statoKepleroOptions" :key="statoKep" :value="statoKep">
              {{ statoKep }}
            </option>
          </select>
        </div>
      </div>
      <div class="search-actions">
        <button @click="handleSearch" class="btn-search">🔍 Search</button>
        <button @click="clearSearch" class="btn-clear">✖ Clear</button>
      </div>
    </div>

    <div class="grid-header">
      <h2>Keplero Compare Data</h2>
      <div v-if="totalRows !== null" class="row-count">
        <span>Loaded: <strong>{{ loadedRows.toLocaleString() }}</strong></span>
        <span class="separator">|</span>
        <span>Total: <strong>{{ totalRows.toLocaleString() }}</strong></span>
      </div>
    </div>
    <ag-grid-vue
      class="ag-theme-alpine"
      :columnDefs="columnDefs"
      :defaultColDef="defaultColDef"
      :rowModelType="'infinite'"
      :datasource="datasource"
      :cacheBlockSize="100"
      :cacheOverflowSize="2"
      :maxConcurrentDatasourceRequests="2"
      :infiniteInitialRowCount="1000"
      :maxBlocksInCache="10"
      @grid-ready="onGridReady"
      @cell-double-clicked="onCellDoubleClicked"
      style="height: 600px;"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { AgGridVue } from 'ag-grid-vue3'
import { fetchKepleroCompareData, fetchKepleroCompareStatistics, fetchCodaValues, fetchStatoValues, fetchEsitoValues, fetchStatoKepleroValues } from '@/services/api'
import type { ColDef, GridApi, GridReadyEvent, IDatasource } from 'ag-grid-community'
import type { KepleroCompareStatistics } from '@/services/api'
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

const gridApi = ref<GridApi | null>(null)
const statistics = ref<KepleroCompareStatistics | null>(null)
const loadingStats = ref<boolean>(true)
const totalRows = ref<number | null>(null)
const loadedRows = ref<number>(0)

const searchFilters = ref({
  itemId: '',
  protocollo: '',
  coda: '',
  stato: '',
  esito: '',
  dataEsito: '',
  statoKeplero: ''
})

const codaOptions = ref<string[]>([])
const statoOptions = ref<string[]>([])
const esitoOptions = ref<string[]>([])
const statoKepleroOptions = ref<string[]>([])

const columnDefs = ref<ColDef[]>([
  { 
    field: 'id', 
    headerName: 'ID', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    width: 100
  },
  { 
    field: 'itemId', 
    headerName: 'Item ID', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    width: 120
  },
  { 
    field: 'protocollo', 
    headerName: 'Protocollo', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 200
  },
  { 
    field: 'coda', 
    headerName: 'Coda', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 150
  },
  { 
    field: 'statoPratica', 
    headerName: 'Stato Pratica', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 180
  },
  { 
    field: 'stato', 
    headerName: 'Stato', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 150
  },
  { 
    field: 'esito', 
    headerName: 'Esito', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 150
  },
  { 
    field: 'dataEsito', 
    headerName: 'Data Esito', 
    filter: 'agDateColumnFilter', 
    sortable: true,
    width: 180,
    valueFormatter: (params) => {
      if (params.value) {
        return new Date(params.value).toLocaleDateString('it-IT')
      }
      return ''
    }
  },
  { 
    field: 'statoPratica_Keplero', 
    headerName: 'Stato Pratica Keplero', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 200
  },
  { 
    field: 'riga', 
    headerName: 'Riga', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    width: 100
  }
])

const defaultColDef = ref<ColDef>({
  resizable: true,
  sortable: true,
  filter: true
})

// Infinite Row Model datasource
const datasource = ref<IDatasource>({
  rowCount: undefined,
  getRows: async (params) => {
    try {
      console.log('Fetching keplero compare rows from', params.startRow, 'to', params.endRow)
      
      // Build filter model with search filters
      const filterModel = params.filterModel || {}
      
      // Add search filters if they exist
      if (searchFilters.value.itemId) {
        filterModel['itemId'] = {
          filterType: 'number',
          type: 'equals',
          filter: searchFilters.value.itemId
        }
      }
      if (searchFilters.value.protocollo) {
        filterModel['protocollo'] = {
          filterType: 'text',
          type: 'contains',
          filter: searchFilters.value.protocollo
        }
      }
      if (searchFilters.value.coda) {
        filterModel['coda'] = {
          filterType: 'text',
          type: 'contains',
          filter: searchFilters.value.coda
        }
      }
      if (searchFilters.value.stato) {
        filterModel['stato'] = {
          filterType: 'text',
          type: 'contains',
          filter: searchFilters.value.stato
        }
      }
      if (searchFilters.value.esito) {
        filterModel['esito'] = {
          filterType: 'text',
          type: 'contains',
          filter: searchFilters.value.esito
        }
      }
      if (searchFilters.value.dataEsito) {
        filterModel['dataEsito'] = {
          filterType: 'text',
          type: 'contains',
          filter: searchFilters.value.dataEsito
        }
      }
      if (searchFilters.value.statoKeplero) {
        filterModel['statoPratica_Keplero'] = {
          filterType: 'text',
          type: 'contains',
          filter: searchFilters.value.statoKeplero
        }
      }
      
      const request = {
        startRow: params.startRow!,
        endRow: params.endRow!,
        sortModel: params.sortModel,
        filterModel: filterModel
      }

      const response = await fetchKepleroCompareData(request)
      
      const { rowData, rowCount } = response
      
      // Update total rows count
      if (rowCount !== undefined) {
        totalRows.value = rowCount
      }
      
      // Update loaded rows count
      loadedRows.value = params.endRow! > rowCount! ? rowCount! : params.endRow!
      
      // Calculate last row
      let lastRow = -1
      if (rowData.length < (params.endRow! - params.startRow!)) {
        lastRow = params.startRow! + rowData.length
      } else if (rowCount !== undefined) {
        lastRow = rowCount
      }

      params.successCallback(rowData, lastRow)
      
      console.log('Loaded', rowData.length, 'rows. Total:', rowCount)
    } catch (error) {
      console.error('Error fetching keplero compare data:', error)
      params.failCallback()
    }
  }
})

const onCellDoubleClicked = (event: any) => {
  // Check if the double-clicked cell is in the Protocollo column
  if (event.column.colId === 'protocollo') {
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
  console.log('Keplero Compare grid ready with Infinite Row Model')
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
    itemId: '',
    protocollo: '',
    coda: '',
    stato: '',
    esito: '',
    dataEsito: '',
    statoKeplero: ''
  }
  if (gridApi.value) {
    totalRows.value = null
    loadedRows.value = 0
    gridApi.value.setGridOption('datasource', datasource.value)
    console.log('Search filters cleared')
  }
}

const loadStatistics = async () => {
  try {
    loadingStats.value = true
    console.log('Loading statistics...')
    statistics.value = await fetchKepleroCompareStatistics()
    console.log('Statistics loaded:', statistics.value)
  } catch (error) {
    console.error('Error loading statistics:', error)
  } finally {
    loadingStats.value = false
  }
}

onMounted(async () => {
  console.log('Component mounted, loading statistics and filter options')
  loadStatistics()
  
  try {
    // Load dropdown options
    codaOptions.value = await fetchCodaValues()
    statoOptions.value = await fetchStatoValues()
    esitoOptions.value = await fetchEsitoValues()
    statoKepleroOptions.value = await fetchStatoKepleroValues()
    console.log('Loaded filter options')
  } catch (error) {
    console.error('Error loading filter options:', error)
  }
})
</script>

<style scoped>
.keplero-compare-grid {
  width: 100%;
  height: 100%;
  padding: 20px;
}

.statistics-section {
  margin-bottom: 30px;
  padding: 20px;
  background: #f8f9fa;
  border-radius: 8px;
}

.statistics-section h2 {
  margin-top: 0;
  margin-bottom: 20px;
  color: #333;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
}

.stat-card {
  background: white;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.stat-card.total {
  grid-column: 1 / -1;
  text-align: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.stat-card h3 {
  margin: 0 0 15px 0;
  font-size: 14px;
  font-weight: 600;
  color: #666;
  text-transform: uppercase;
}

.stat-card.total h3 {
  color: white;
}

.stat-value {
  font-size: 36px;
  font-weight: bold;
  color: white;
}

.stat-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.stat-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: #f8f9fa;
  border-radius: 4px;
}

.stat-label {
  font-size: 14px;
  color: #333;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 70%;
}

.stat-count {
  font-size: 16px;
  font-weight: bold;
  color: #667eea;
}

.loading-stats {
  text-align: center;
  padding: 20px;
  color: #666;
  font-style: italic;
}

.grid-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.grid-header h2 {
  margin: 0;
  color: #2c3e50;
}

.row-count {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 14px;
  color: #666;
}

.row-count .separator {
  color: #ccc;
}

.row-count strong {
  color: #2c3e50;
  font-size: 16px;
}

h2 {
  margin-bottom: 20px;
  color: #2c3e50;
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

.ag-theme-alpine {
  width: 100%;
  height: 100%;
}
</style>
