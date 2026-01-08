<template>
  <div class="keplero-compare-grid">
    <!-- Toggle Buttons Row -->
    <div class="toggle-buttons-row">
      <button @click="showStatistics = !showStatistics" class="toggle-stats-btn">
        <ChartBarIcon class="icon" />
        {{ showStatistics ? 'Hide Statistics' : 'Show Statistics' }}
      </button>
      <button @click="showSearchForm = !showSearchForm" class="toggle-search-btn">
        <MagnifyingGlassIcon class="icon" />
        {{ showSearchForm ? 'Hide Search' : 'Show Search' }}
      </button>
      <button @click="showMismatch = !showMismatch" class="toggle-mismatch-btn">
        <ExclamationTriangleIcon class="icon" />
        {{ showMismatch ? 'Hide Mismatch' : 'Show Mismatch' }}
      </button>
    </div>

    <!-- Statistics Cards -->
    <div class="statistics-section" v-if="showStatistics && statistics && !loadingStats">
      <h2>Summary Statistics</h2>
      
      <div class="stats-grid">
        <!-- Total Records -->
        <div class="stat-card total">
          <h3>Totale Pratiche</h3>
          <div class="stat-value">{{ statistics.totalRecords.toLocaleString() }}</div>
        </div>

        <!-- By Coda -->
        <div class="stat-card">
          <h3>By Coda</h3>
          <div class="stat-list">
            <div v-for="item in statistics.byCoda.slice(0, 5)" :key="item.coda" class="stat-item clickable" @click="filterByCoda(item.coda)">
              <span class="stat-label">{{ item.coda }}</span>
              <span class="stat-count">{{ item.count.toLocaleString() }}</span>
            </div>
          </div>
        </div>

        <!-- By Stato Pratica -->
        <div class="stat-card">
          <h3>By Stato Pratica</h3>
          <div class="stat-list">
            <div v-for="item in statistics.byStatoPratica.slice(0, 5)" :key="item.statoPratica" class="stat-item clickable" @click="filterByStatoPratica(item.statoPratica)">
              <span class="stat-label">{{ item.statoPratica }}</span>
              <span class="stat-count">{{ item.count.toLocaleString() }}</span>
            </div>
          </div>
        </div>

        <!-- By Stato -->
        <div class="stat-card">
          <h3>By Stato</h3>
          <div class="stat-list">
            <div v-for="item in statistics.byStato.slice(0, 5)" :key="item.stato" class="stat-item clickable" @click="filterByStato(item.stato)">
              <span class="stat-label">{{ item.stato }}</span>
              <span class="stat-count">{{ item.count.toLocaleString() }}</span>
            </div>
          </div>
        </div>

        <!-- By Stato Pratica Keplero -->
        <div class="stat-card">
          <h3>By Stato Pratica Keplero</h3>
          <div class="stat-list">
            <div v-for="item in statistics.byStatoPraticaKeplero.slice(0, 5)" :key="item.statoPraticaKeplero" class="stat-item clickable" @click="filterByStatoKeplero(item.statoPraticaKeplero)">
              <span class="stat-label">{{ item.statoPraticaKeplero }}</span>
              <span class="stat-count">{{ item.count.toLocaleString() }}</span>
            </div>
          </div>
        </div>

        <!-- By Esito -->
        <div class="stat-card">
          <h3>By Esito</h3>
          <div class="stat-list">
            <div v-for="item in statistics.byEsito.slice(0, 5)" :key="item.esito" class="stat-item clickable" @click="filterByEsito(item.esito)">
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

    <!-- Mismatch Section -->
    <div class="mismatch-section" v-if="showMismatch">
      <div class="mismatch-header">
        <h2>Mismatch Records</h2>
        <div class="mismatch-actions">
          <button @click="runMismatch" class="btn-run-mismatch" :disabled="isLoadingMismatch">
            <ClockIcon v-if="isLoadingMismatch" class="icon" />
            <PlayIcon v-else class="icon" />
            {{ isLoadingMismatch ? 'Running...' : 'Run' }}
          </button>
          <div v-if="mismatchData.length > 0" class="mismatch-download-buttons">
            <button @click="downloadMismatchAsJSON" class="btn-download-mismatch" title="Download as JSON">
              <ArrowDownTrayIcon class="icon" />
              JSON
            </button>
            <button @click="downloadMismatchAsCSV" class="btn-download-mismatch" title="Download as CSV">
              <ArrowDownTrayIcon class="icon" />
              CSV
            </button>
          </div>
        </div>
      </div>
      
      <div v-if="mismatchError" class="error-message">
        <ExclamationTriangleIcon class="icon-inline" />
        {{ mismatchError }}
      </div>
      
      <div v-if="mismatchData.length > 0" class="mismatch-results">
        <div class="mismatch-count">
          Found <strong>{{ mismatchData.length }}</strong> mismatch record{{ mismatchData.length !== 1 ? 's' : '' }}
        </div>
        <ag-grid-vue
          :class="isDarkMode ? 'ag-theme-alpine-dark' : 'ag-theme-alpine'"
          :columnDefs="mismatchColumnDefs"
          :defaultColDef="defaultColDef"
          :rowData="mismatchData"
          :enableCellTextSelection="true"
          :ensureDomOrder="true"
          :pagination="true"
          :paginationPageSize="50"
          style="height: 400px;"
        />
      </div>
      
      <div v-else-if="!isLoadingMismatch && mismatchRan" class="no-mismatch">
        <CheckIcon class="icon-inline" />
        No mismatch records found
      </div>
    </div>

    <!-- Search Form -->
    <div class="search-form" v-if="showSearchForm">
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
          <label for="search-statopratica">Stato Pratica</label>
          <select
            id="search-statopratica"
            v-model="searchFilters.statoPratica"
            @change="handleSearch"
          >
            <option value="">All</option>
            <option v-for="sp in statoPraticaOptions" :key="sp" :value="sp">
              {{ sp }}
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
        <button @click="handleSearch" class="btn-search">
          <MagnifyingGlassIcon class="icon" />
          Search
        </button>
        <button @click="clearSearch" class="btn-clear">
          <span class="icon-text">✕</span>
          Clear
        </button>
      </div>
    </div>

    <div class="grid-header">
      <h2>Keplero Compare Data</h2>
      <div class="header-right">
        <div class="download-buttons">
          <button @click="downloadAsJSON" class="btn-download" title="Download as JSON">
            <ArrowDownTrayIcon class="icon" />
            JSON
          </button>
          <button @click="downloadAsCSV" class="btn-download" title="Download as CSV">
            <ArrowDownTrayIcon class="icon" />
            CSV
          </button>
        </div>
        <div v-if="totalRows !== null" class="row-count">
          <span>Loaded: <strong>{{ loadedRows.toLocaleString() }}</strong></span>
          <span class="separator">|</span>
          <span>Total: <strong>{{ totalRows.toLocaleString() }}</strong></span>
        </div>
      </div>
    </div>
    <ag-grid-vue
      :class="isDarkMode ? 'ag-theme-alpine-dark' : 'ag-theme-alpine'"
      :columnDefs="columnDefs"
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
import { fetchKepleroCompareData, fetchKepleroCompareStatistics, fetchCodaValues, fetchStatoPraticaValues, fetchStatoValues, fetchEsitoValues, fetchStatoKepleroValues, fetchMismatchData } from '@/services/api'
import type { ColDef, GridApi, GridReadyEvent, IDatasource } from 'ag-grid-community'
import type { KepleroCompareStatistics } from '@/services/api'
import { useDarkMode } from '@/composables/useDarkMode'
import { ChartBarIcon, MagnifyingGlassIcon, ExclamationTriangleIcon, PlayIcon, ClockIcon, ArrowDownTrayIcon, CheckIcon } from '@heroicons/vue/24/outline'
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

const { isDarkMode } = useDarkMode()

const gridApi = ref<GridApi | null>(null)
const statistics = ref<KepleroCompareStatistics | null>(null)
const loadingStats = ref<boolean>(true)
const showStatistics = ref<boolean>(false)
const showSearchForm = ref<boolean>(false)
const showMismatch = ref<boolean>(false)
const totalRows = ref<number | null>(null)
const loadedRows = ref<number>(0)

// Mismatch state
const mismatchData = ref<Array<{ protocollo: string }>>([])
const isLoadingMismatch = ref<boolean>(false)
const mismatchError = ref<string>('')
const mismatchRan = ref<boolean>(false)

const searchFilters = ref({
  itemId: '',
  protocollo: '',
  coda: '',
  statoPratica: '',
  stato: '',
  esito: '',
  dataEsito: '',
  statoKeplero: ''
})

const codaOptions = ref<string[]>([])
const statoPraticaOptions = ref<string[]>([])
const statoOptions = ref<string[]>([])
const esitoOptions = ref<string[]>([])
const statoKepleroOptions = ref<string[]>([])

const columnDefs = ref<ColDef[]>([
  { 
    field: 'protocollo', 
    headerName: 'Protocollo', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 200,
    pinned: 'left'
  },
  { 
    field: 'itemId', 
    headerName: 'Item ID', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    width: 120
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

const mismatchColumnDefs = ref<ColDef[]>([
  { 
    field: 'protocollo', 
    headerName: 'Protocollo', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    flex: 1
  }
])

const defaultColDef = ref<ColDef>({
  resizable: true,
  sortable: true,
  filter: true,
  enableCellTextSelection: true
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
          filter: String(searchFilters.value.itemId)
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
          type: 'equals',
          filter: searchFilters.value.coda
        }
      }
      if (searchFilters.value.statoPratica) {
        filterModel['statoPratica'] = {
          filterType: 'text',
          type: 'equals',
          filter: searchFilters.value.statoPratica
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
          type: 'equals',
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
          type: 'equals',
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

// Download functions
const downloadAsJSON = async () => {
  try {
    // Get all currently displayed rows from the infinite row model
    const allData: any[] = []
    if (gridApi.value) {
      const rowCount = gridApi.value.getDisplayedRowCount()
      for (let i = 0; i < rowCount; i++) {
        const rowNode = gridApi.value.getDisplayedRowAtIndex(i)
        if (rowNode && rowNode.data) {
          allData.push(rowNode.data)
        }
      }
    }

    if (allData.length === 0) {
      alert('No data available to download')
      return
    }

    const dataStr = JSON.stringify(allData, null, 2)
    const dataBlob = new Blob([dataStr], { type: 'application/json' })
    const url = URL.createObjectURL(dataBlob)
    const link = document.createElement('a')
    link.href = url
    link.download = `keplero-compare-${new Date().toISOString().slice(0, 10)}.json`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  } catch (error) {
    console.error('Error downloading JSON:', error)
    alert('Error downloading JSON file')
  }
}

const downloadAsCSV = async () => {
  try {
    // Get all currently displayed rows from the infinite row model
    const allData: any[] = []
    if (gridApi.value) {
      const rowCount = gridApi.value.getDisplayedRowCount()
      for (let i = 0; i < rowCount; i++) {
        const rowNode = gridApi.value.getDisplayedRowAtIndex(i)
        if (rowNode && rowNode.data) {
          allData.push(rowNode.data)
        }
      }
    }

    if (allData.length === 0) {
      alert('No data available to download')
      return
    }

    // Get column headers
    const headers = columnDefs.value.map(col => col.headerName || col.field || '')
    const fields = columnDefs.value.map(col => col.field || '')

    // Build CSV content
    const csvRows = []
    csvRows.push(headers.join(';'))

    for (const row of allData) {
      const values = fields.map(field => {
        const value = row[field]
        if (value === null || value === undefined) return ''
        // Escape quotes and wrap in quotes if contains semicolon, quote, or newline
        const stringValue = String(value)
        if (stringValue.includes(';') || stringValue.includes('"') || stringValue.includes('\n') || stringValue.includes('\r')) {
          return `"${stringValue.replace(/"/g, '""')}"`
        }
        return stringValue
      })
      csvRows.push(values.join(';'))
    }

    const csvStr = csvRows.join('\r\n')
    // Add UTF-8 BOM for Excel compatibility
    const BOM = '\uFEFF'
    const dataBlob = new Blob([BOM + csvStr], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(dataBlob)
    const link = document.createElement('a')
    link.href = url
    link.download = `keplero-compare-${new Date().toISOString().slice(0, 10)}.csv`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  } catch (error) {
    console.error('Error downloading CSV:', error)
    alert('Error downloading CSV file')
  }
}

// Mismatch download functions
const downloadMismatchAsJSON = () => {
  try {
    if (mismatchData.value.length === 0) {
      alert('No mismatch data available to download')
      return
    }

    const dataStr = JSON.stringify(mismatchData.value, null, 2)
    const dataBlob = new Blob([dataStr], { type: 'application/json' })
    const url = URL.createObjectURL(dataBlob)
    const link = document.createElement('a')
    link.href = url
    link.download = `keplero-mismatch-${new Date().toISOString().slice(0, 10)}.json`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  } catch (error) {
    console.error('Error downloading mismatch JSON:', error)
    alert('Error downloading JSON file')
  }
}

const downloadMismatchAsCSV = () => {
  try {
    if (mismatchData.value.length === 0) {
      alert('No mismatch data available to download')
      return
    }

    // Get column headers from mismatch column definitions
    const headers = mismatchColumnDefs.value.map(col => col.headerName || col.field || '')
    const fields = mismatchColumnDefs.value.map(col => col.field || '')

    // Build CSV content
    const csvRows = []
    csvRows.push(headers.join(';'))

    for (const row of mismatchData.value) {
      const values = fields.map(field => {
        const value = (row as any)[field]
        if (value === null || value === undefined) return ''
        // Escape quotes and wrap in quotes if contains semicolon, quote, or newline
        const stringValue = String(value)
        if (stringValue.includes(';') || stringValue.includes('"') || stringValue.includes('\n') || stringValue.includes('\r')) {
          return `"${stringValue.replace(/"/g, '""')}"`
        }
        return stringValue
      })
      csvRows.push(values.join(';'))
    }

    const csvStr = csvRows.join('\r\n')
    // Add UTF-8 BOM for Excel compatibility
    const BOM = '\uFEFF'
    const dataBlob = new Blob([BOM + csvStr], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(dataBlob)
    const link = document.createElement('a')
    link.href = url
    link.download = `keplero-mismatch-${new Date().toISOString().slice(0, 10)}.csv`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  } catch (error) {
    console.error('Error downloading mismatch CSV:', error)
    alert('Error downloading CSV file')
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

const filterByCoda = (coda: string) => {
  clearSearch()
  searchFilters.value.coda = coda
  handleSearch()
}

const filterByStatoPratica = (statoPratica: string) => {
  clearSearch()
  searchFilters.value.statoPratica = statoPratica
  handleSearch()
}

const filterByStato = (stato: string) => {
  clearSearch()
  searchFilters.value.stato = stato
  handleSearch()
}

const filterByStatoKeplero = (statoKeplero: string) => {
  clearSearch()
  searchFilters.value.statoKeplero = statoKeplero
  handleSearch()
}

const filterByEsito = (esito: string) => {
  clearSearch()
  searchFilters.value.esito = esito
  handleSearch()
}

const clearSearch = () => {
  searchFilters.value = {
    itemId: '',
    protocollo: '',
    coda: '',
    statoPratica: '',
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

const runMismatch = async () => {
  isLoadingMismatch.value = true
  mismatchError.value = ''
  mismatchRan.value = false
  mismatchData.value = []
  
  try {
    const response = await fetchMismatchData()
    
    if (response.success) {
      // Convert protocollo strings to objects for grid
      mismatchData.value = response.protocollos.map(p => ({ protocollo: p }))
      mismatchRan.value = true
    } else {
      mismatchError.value = 'Failed to fetch mismatch data'
    }
  } catch (error: any) {
    console.error('Error running mismatch:', error)
    mismatchError.value = error.message || 'An error occurred while fetching mismatch data'
  } finally {
    isLoadingMismatch.value = false
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
    statoPraticaOptions.value = await fetchStatoPraticaValues()
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
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.toggle-buttons-row {
  display: flex;
  gap: 10px;
  margin-bottom: 15px;
}

.toggle-stats-btn,
.toggle-search-btn,
.toggle-mismatch-btn {
  padding: 10px 20px;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 600;
  transition: all 0.2s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.toggle-mismatch-btn {
  background: #ff9800;
}

.toggle-stats-btn:hover,
.toggle-search-btn:hover,
.toggle-mismatch-btn:hover {
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
  transform: translateY(-1px);
}

.toggle-stats-btn:hover,
.toggle-search-btn:hover {
  background: #5568d3;
}

.toggle-mismatch-btn:hover {
  background: #f57c00;
}

.toggle-stats-btn:active,
.toggle-search-btn:active,
.toggle-mismatch-btn:active {
  transform: translateY(0);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
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

.mismatch-section {
  margin-bottom: 30px;
  padding: 20px;
  background: #fff3e0;
  border-radius: 8px;
  border: 2px solid #ff9800;
}

.mismatch-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.mismatch-header h2 {
  margin: 0;
  color: #e65100;
}

.mismatch-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.mismatch-download-buttons {
  display: flex;
  gap: 0.5rem;
}

.btn-download-mismatch {
  background-color: #4caf50;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.btn-download-mismatch:hover {
  background-color: #45a049;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.15);
  transform: translateY(-1px);
}

.btn-download-mismatch:active {
  transform: translateY(0);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-run-mismatch {
  padding: 12px 24px;
  background: #ff9800;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 16px;
  font-weight: 600;
  transition: all 0.2s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-run-mismatch:hover:not(:disabled) {
  background: #f57c00;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
  transform: translateY(-1px);
}

.btn-run-mismatch:active:not(:disabled) {
  transform: translateY(0);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-run-mismatch:disabled {
  background: #b0bec5;
  cursor: not-allowed;
  opacity: 0.7;
}

.mismatch-results {
  margin-top: 20px;
}

.mismatch-count {
  margin-bottom: 10px;
  padding: 10px;
  background: white;
  border-radius: 4px;
  font-size: 14px;
  color: #333;
}

.mismatch-count strong {
  font-weight: 700;
  color: #ff9800;
}

.no-mismatch {
  padding: 20px;
  background: #e8f5e9;
  border-radius: 4px;
  text-align: center;
  color: #2e7d32;
  font-weight: 600;
  font-size: 16px;
}

.error-message {
  padding: 15px;
  background: #ffebee;
  color: #c62828;
  border-radius: 4px;
  margin-bottom: 15px;
  border-left: 4px solid #d32f2f;
  font-weight: 500;
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

.stat-item.clickable {
  cursor: pointer;
  transition: all 0.2s ease;
}

.stat-item.clickable:hover {
  background: #e3e7f7;
  transform: translateX(4px);
  box-shadow: 0 2px 4px rgba(102, 126, 234, 0.15);
}

.stat-item.clickable:active {
  transform: translateX(2px);
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

.header-right {
  display: flex;
  align-items: center;
  gap: 20px;
}

.download-buttons {
  display: flex;
  gap: 0.5rem;
}

.btn-download {
  background-color: #4caf50;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.btn-download:hover {
  background-color: #45a049;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.15);
  transform: translateY(-1px);
}

.btn-download:active {
  transform: translateY(0);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
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

/* Dark Mode Support */
:global(#app.dark-mode) .keplero-compare-grid {
  color: #e0e0e0;
}

:global(#app.dark-mode) .statistics-section {
  background: #2a2a2a;
  color: #e0e0e0;
}

:global(#app.dark-mode) .statistics-section h2 {
  color: #e0e0e0;
}

:global(#app.dark-mode) .stat-card {
  background: #1f1f1f;
  border-color: #404040;
}

:global(#app.dark-mode) .stat-card h3 {
  color: #b0b0b0;
}

:global(#app.dark-mode) .stat-value,
:global(#app.dark-mode) .stat-count {
  color: #8b9cff;
}

:global(#app.dark-mode) .stat-item {
  background: #2d2d2d;
}

:global(#app.dark-mode) .stat-item.clickable:hover {
  background: #3a3a4f;
}

:global(#app.dark-mode) .stat-label {
  color: #d0d0d0;
}

:global(#app.dark-mode) .search-form {
  background: #2a2a2a;
  border-color: #404040;
}

:global(#app.dark-mode) .search-field label {
  color: #b0b0b0;
}

:global(#app.dark-mode) .search-field input,
:global(#app.dark-mode) .search-field select {
  background: #1f1f1f;
  border-color: #404040;
  color: #e0e0e0;
}

:global(#app.dark-mode) .search-field input:focus,
:global(#app.dark-mode) .search-field select:focus {
  border-color: #667eea;
  background: #252525;
}

:global(#app.dark-mode) .loading-stats {
  color: #b0b0b0;
}
</style>
