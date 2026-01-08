<template>
  <div class="scraper-grid">
    <div class="grid-header">
      <h2>Scraper - Faschim Data</h2>
      <div class="header-controls">
        <select v-model="selectedSearch" class="search-selector">
          <option value="">Select Search Type</option>
          <option value="Presentate">Presentate</option>
          <option value="DaLavorare">Da Lavorare</option>
          <option value="RestituitaDaConsulente">Restituita Da Consulente</option>
          <option value="Integrata">Integrata</option>
          <option value="Inlavorazione">In lavorazione</option>
          <option value="NonIntegrata">Non Integrata</option>
        </select>
        <div class="date-filter">
          <label class="checkbox-label">
            <input 
              type="checkbox" 
              v-model="useDateFilter" 
              class="date-checkbox"
            />
            Ultima modifica:
          </label>
          <input 
            id="modifiedDate"
            type="date" 
            v-model="modifiedDate" 
            class="date-picker"
            :disabled="!useDateFilter"
          />
        </div>
        <button 
          @click="fetchFromFaschim" 
          class="btn-download"
          :disabled="isLoading || !selectedSearch"
        >
          <ClockIcon v-if="isLoading" class="icon" />
          <ArrowDownTrayIcon v-else class="icon" />
          {{ isLoading ? 'Loading...' : 'Download from Faschim' }}
        </button>
        <button 
          @click="refreshGrid" 
          class="btn-refresh"
          :disabled="isLoading"
          title="Refresh grid from database"
        >
          <ArrowPathIcon class="icon" />
          Refresh
        </button>
        <div class="download-separator"></div>
        <button @click="downloadAsJSON" class="btn-export" title="Download as JSON">
          <ArrowDownTrayIcon class="icon" />
          JSON
        </button>
        <button @click="downloadAsCSV" class="btn-export" title="Download as CSV">
          <ArrowDownTrayIcon class="icon" />
          CSV
        </button>
      </div>
    </div>

    <div v-if="errorMessage" class="error-message">
      <ExclamationTriangleIcon class="icon-inline" />
      {{ errorMessage }}
    </div>

    <!-- Progress indicator -->
    <div v-if="downloadProgress.isDownloading" class="progress-container">
      <div class="progress-header">
        <h3>
          <ArrowDownTrayIcon class="icon-inline" />
          Downloading Data...
        </h3>
        <span class="progress-time">{{ downloadProgress.elapsedTime }}s elapsed</span>
      </div>
      <div class="progress-bar">
        <div 
          class="progress-fill" 
          :style="{ width: downloadProgress.percentage + '%' }"
        ></div>
      </div>
      <div class="progress-details">
        <span>Page {{ downloadProgress.currentPage }} of {{ downloadProgress.totalPages }}</span>
        <span>{{ downloadProgress.recordsFetched.toLocaleString() }} / {{ downloadProgress.totalRecords.toLocaleString() }} records</span>
      </div>
      <div class="progress-message">{{ downloadProgress.message }}</div>
    </div>

    <!-- Success message -->
    <div v-if="downloadProgress.completed && !downloadProgress.isDownloading" class="success-message">
      <CheckIcon class="icon-inline" />
      Download completed! {{ downloadProgress.recordsFetched.toLocaleString() }} records fetched in {{ downloadProgress.elapsedTime }}s
    </div>

    <!-- Extraction Date Filter and Delete Section -->
    <div class="filter-delete-row">
      <div class="extraction-filter">
        <label class="checkbox-label">
          <input 
            type="checkbox" 
            v-model="loadAllData" 
            class="date-checkbox"
            @change="filterByExtractionDate"
          />
          Load All Data
        </label>
        <div v-if="!loadAllData" class="date-filter-group">
          <label for="extractionDateFilter">Filter by Extraction Date:</label>
          <input 
            id="extractionDateFilter"
            type="date" 
            v-model="extractionDateFilter" 
            class="date-picker"
            @change="filterByExtractionDate"
          />
        </div>
      </div>

      <!-- Delete Data Section -->
      <div class="delete-section">
        <span class="delete-info">Delete all data currently displayed in the grid</span>
        <button 
          @click="confirmDelete" 
          class="btn-delete"
          :disabled="isLoading || rowData.length === 0"
        >
          <TrashIcon class="icon" />
          Delete Grid Data
        </button>
      </div>
    </div>

    <!-- Delete Confirmation Dialog -->
    <div v-if="showDeleteConfirm" class="modal-overlay" @click.self="showDeleteConfirm = false">
      <div class="modal-content">
        <h3>
          <ExclamationTriangleIcon class="icon-inline" />
          Confirm Delete
        </h3>
        <p>{{ deleteConfirmMessage }}</p>
        <div class="modal-actions">
          <button @click="executeDelete" class="btn-confirm-delete">
            <CheckIcon class="icon" />
            Confirm Delete
          </button>
          <button @click="showDeleteConfirm = false" class="btn-cancel">
            <XMarkIcon class="icon" />
            Cancel
          </button>
        </div>
      </div>
    </div>

    <ag-grid-vue
      :class="isDarkMode ? 'ag-theme-alpine-dark' : 'ag-theme-alpine'"
      :columnDefs="columnDefs"
      :defaultColDef="defaultColDef"
      :rowData="rowData"
      :enableCellTextSelection="true"
      :ensureDomOrder="true"
      :pagination="true"
      :paginationPageSize="100"
      @grid-ready="onGridReady"
      class="grid-container"
    />

    <div v-if="totalRows !== null" class="row-count">
      <span>Total Records: <strong>{{ totalRows.toLocaleString() }}</strong></span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { AgGridVue } from 'ag-grid-vue3'
import { fetchDataFromFaschim, fetchScraperData, deleteScraperData, type ScraperData } from '@/services/api'
import type { ColDef, GridApi, GridReadyEvent } from 'ag-grid-community'
import { useDarkMode } from '@/composables/useDarkMode'
import { ArrowDownTrayIcon, ArrowPathIcon, TrashIcon, CheckIcon, XMarkIcon, ClockIcon, ExclamationTriangleIcon } from '@heroicons/vue/24/outline'
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

const { isDarkMode } = useDarkMode()

const gridApi = ref<GridApi | null>(null)
const rowData = ref<ScraperData[]>([])
const totalRows = ref<number | null>(null)
const isLoading = ref<boolean>(false)
const errorMessage = ref<string>('')
const selectedSearch = ref<string>('')

// Date filter - default to yesterday
const getYesterdayDate = () => {
  const yesterday = new Date()
  yesterday.setDate(yesterday.getDate() - 1)
  return yesterday.toISOString().split('T')[0]
}
const modifiedDate = ref<string>(getYesterdayDate())
const useDateFilter = ref<boolean>(false)

// Extraction date filter - default to today
const getTodayDate = () => {
  const today = new Date()
  return today.toISOString().split('T')[0]
}
const extractionDateFilter = ref<string>(getTodayDate())
const allRowData = ref<ScraperData[]>([]) // Store all data before filtering
const loadAllData = ref<boolean>(false) // Default to filtering by extraction date

// Delete controls
const showDeleteConfirm = ref<boolean>(false)
const deleteConfirmMessage = ref<string>('')

// Progress tracking
const downloadProgress = ref({
  isDownloading: false,
  completed: false,
  currentPage: 0,
  totalPages: 0,
  recordsFetched: 0,
  totalRecords: 0,
  percentage: 0,
  elapsedTime: 0,
  message: ''
})

let progressTimer: number | null = null

// Search filter configurations (without date filter to get all records)
const searchFilters: Record<string, string> = {
  'Presentate': '[{"property":"StoricoNominativo.bloccoCompleto","value":"*0","anyMatch":false},{"property":"statoPratica","value":"Presentata","anyMatch":false}]',
  'DaLavorare': '[{"property":"StoricoNominativo.bloccoCompleto","value":"*0","anyMatch":false},{"property":"statoPratica","value":"Da Lavorare","anyMatch":false}]',
  'RestituitaDaConsulente': '[{"property":"StoricoNominativo.bloccoCompleto","value":"*0","anyMatch":false},{"property":"statoPratica","value":"Restituita dal Consulente","anyMatch":false}]',
  'Integrata': '[{"property":"StoricoNominativo.bloccoCompleto","value":"*0","anyMatch":false},{"property":"statoPratica","value":"Integrata","anyMatch":false}]',
  'Inlavorazione': '[{"property":"StoricoNominativo.bloccoCompleto","value":"*0","anyMatch":false},{"property":"statoPratica","value":"In lavorazione","anyMatch":false}]',
  'NonIntegrata': '[{"property":"StoricoNominativo.bloccoCompleto","value":"*0","anyMatch":false},{"property":"statoPratica","value":"Non Integrata","anyMatch":false}]'
}

const columnDefs = ref<ColDef[]>([
  { 
    field: 'id', 
    headerName: 'ID', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    width: 80
  },
  { 
    field: 'extractionDate', 
    headerName: 'Data Estrazione', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 140,
    pinned: 'left'
  },
  { 
    field: 'idPratica', 
    headerName: 'ID Pratica', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 120
  },
  { 
    field: 'numeroProtocollo', 
    headerName: 'Protocollo', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 200
  },
  { 
    field: 'statoPratica', 
    headerName: 'Stato', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 150
  },
  { 
    field: 'tipoPratica', 
    headerName: 'Tipo', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 130
  },
  { 
    field: 'codiceFiscale', 
    headerName: 'CF Socio', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 150
  },
  { 
    field: 'nomePersona', 
    headerName: 'Nome', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 120
  },
  { 
    field: 'cognomePersona', 
    headerName: 'Cognome', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 140
  },
  { 
    field: 'codiceFiscaleBeneficiario', 
    headerName: 'CF Beneficiario', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 160
  },
  { 
    field: 'nomeBeneficiario', 
    headerName: 'Nome Benef.', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 130
  },
  { 
    field: 'cognomeBeneficiario', 
    headerName: 'Cognome Benef.', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 140
  },
  { 
    field: 'dataPresentazione', 
    headerName: 'Data Presentazione', 
    filter: 'agDateColumnFilter', 
    sortable: true,
    width: 160
  },
  { 
    field: 'importoRichiesto', 
    headerName: 'Importo Richiesto', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    width: 140,
    valueFormatter: (params) => {
      if (params.value) {
        return '€ ' + parseFloat(params.value).toFixed(2)
      }
      return ''
    }
  },
  { 
    field: 'importoRiconosciuto', 
    headerName: 'Importo Riconosciuto', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    width: 160,
    valueFormatter: (params) => {
      if (params.value) {
        return '€ ' + parseFloat(params.value).toFixed(2)
      }
      return ''
    }
  },
  { 
    field: 'formaAssistenza', 
    headerName: 'Forma Assistenza', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 150
  },
  { 
    field: 'descrizioneGruppoTariffa', 
    headerName: 'Gruppo Tariffa', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 150
  },
  { 
    field: 'modified', 
    headerName: 'Modificato', 
    filter: 'agDateColumnFilter', 
    sortable: true,
    width: 160
  },
  { 
    field: 'hasConvenzioneFondo', 
    headerName: 'Convenzione Fondo', 
    filter: 'agTextColumnFilter', 
    sortable: true,
    width: 150,
    cellStyle: (params) => {
      if (params.value === 'SI') {
        return { color: '#2e7d32', fontWeight: 'bold' }
      } else if (params.value === 'NO') {
        return { color: '#d32f2f', fontWeight: 'bold' }
      }
      return null
    }
  }
])

const defaultColDef = ref<ColDef>({
  flex: 0,
  minWidth: 100,
  resizable: true,
  sortable: true,
  filter: true
})

const onGridReady = (params: GridReadyEvent) => {
  gridApi.value = params.api
}

const filterByExtractionDate = () => {
  if (loadAllData.value) {
    // Show all data when checkbox is checked
    rowData.value = allRowData.value
  } else if (!extractionDateFilter.value) {
    // No filter date, show empty
    rowData.value = []
  } else {
    // Filter by extraction date - convert from yyyy-MM-dd to dd-MM-yyyy
    const filterDate = extractionDateFilter.value.split('-').reverse().join('-')
    rowData.value = allRowData.value.filter((record: any) => {
      return record.extractionDate === filterDate
    })
  }
  
  totalRows.value = rowData.value.length
  
  // Update grid - use applyTransaction for better performance
  if (gridApi.value) {
    gridApi.value.setGridOption('rowData', rowData.value)
    gridApi.value.refreshCells()
  }
}

const confirmDelete = () => {
  // Get filtered rows from grid API (respects column filters)
  const filteredRows: any[] = []
  if (gridApi.value) {
    gridApi.value.forEachNodeAfterFilter((node) => {
      if (node.data) {
        filteredRows.push(node.data)
      }
    })
  }
  
  const rowCount = filteredRows.length
  if (rowCount === 0) {
    errorMessage.value = 'No records to delete'
    return
  }
  
  deleteConfirmMessage.value = `Are you sure you want to delete ${rowCount.toLocaleString()} record${rowCount !== 1 ? 's' : ''} currently displayed in the grid?\n\nThis action cannot be undone!`
  showDeleteConfirm.value = true
}

// Download functions
const downloadAsJSON = () => {
  try {
    if (rowData.value.length === 0) {
      alert('No data available to download')
      return
    }

    const dataStr = JSON.stringify(rowData.value, null, 2)
    const dataBlob = new Blob([dataStr], { type: 'application/json' })
    const url = URL.createObjectURL(dataBlob)
    const link = document.createElement('a')
    link.href = url
    link.download = `scraper-data-${new Date().toISOString().slice(0, 10)}.json`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  } catch (error) {
    console.error('Error downloading JSON:', error)
    alert('Error downloading JSON file')
  }
}

const downloadAsCSV = () => {
  try {
    if (rowData.value.length === 0) {
      alert('No data available to download')
      return
    }

    // Get column headers
    const headers = columnDefs.value.map(col => col.headerName || col.field || '')
    const fields = columnDefs.value.map(col => col.field || '')

    // Build CSV content
    const csvRows = []
    csvRows.push(headers.join(';'))

    for (const row of rowData.value) {
      const values = fields.map(field => {
        const value = row[field as keyof ScraperData]
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
    link.download = `scraper-data-${new Date().toISOString().slice(0, 10)}.csv`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  } catch (error) {
    console.error('Error downloading CSV:', error)
    alert('Error downloading CSV file')
  }
}

const executeDelete = async () => {
  showDeleteConfirm.value = false
  isLoading.value = true
  errorMessage.value = ''
  
  try {
    // Get IDs of filtered records from grid API (respects column filters)
    const filteredRows: any[] = []
    if (gridApi.value) {
      gridApi.value.forEachNodeAfterFilter((node) => {
        if (node.data) {
          filteredRows.push(node.data)
        }
      })
    }
    
    const recordIds = filteredRows.map((record: any) => record.id)
    
    if (recordIds.length === 0) {
      errorMessage.value = 'No records to delete'
      isLoading.value = false
      return
    }
    
    // Delete records by their IDs
    const result = await deleteScraperData(recordIds)
    
    if (result.success) {
      // Refresh grid after deletion
      await refreshGrid()
      
      // Show success message
      errorMessage.value = ''
      alert(`Successfully deleted ${result.deletedCount} record${result.deletedCount !== 1 ? 's' : ''}`)
    }
    
  } catch (error: any) {
    console.error('Error deleting data:', error)
    errorMessage.value = error.message || 'An error occurred while deleting data'
  } finally {
    isLoading.value = false
  }
}

const refreshGrid = async () => {
  isLoading.value = true
  errorMessage.value = ''
  
  try {
    const scraperData = await fetchScraperData()
    
    // Extract data records from the response
    const allRecords: any[] = []
    if (Array.isArray(scraperData)) {
      scraperData.forEach((search: any) => {
        if (search.dataRecords && Array.isArray(search.dataRecords)) {
          search.dataRecords.forEach((record: any) => {
            try {
              const parsed = JSON.parse(record.rawContent)
              allRecords.push({
                id: record.id,
                searchId: record.searchId,
                hasConvenzioneFondo: record.hasConvenzioneFondo,
                extractionDate: record.extractionDate,
                ...parsed
              })
            } catch (e) {
              console.error('Failed to parse record:', e)
            }
          })
        }
      })
    }
    
    // Store all data
    allRowData.value = allRecords
    
    // Apply extraction date filter
    filterByExtractionDate()
    
  } catch (error: any) {
    console.error('Error refreshing grid:', error)
    errorMessage.value = error.message || 'An error occurred while refreshing data'
  } finally {
    isLoading.value = false
  }
}

const fetchFromFaschim = async () => {
  if (!selectedSearch.value) {
    errorMessage.value = 'Please select a search type'
    return
  }
  
  isLoading.value = true
  errorMessage.value = ''
  
  // Initialize progress
  downloadProgress.value = {
    isDownloading: true,
    completed: false,
    currentPage: 0,
    totalPages: 0,
    recordsFetched: 0,
    totalRecords: 0,
    percentage: 0,
    elapsedTime: 0,
    message: 'Initializing download...'
  }
  
  // Start timer
  const startTime = Date.now()
  progressTimer = window.setInterval(() => {
    downloadProgress.value.elapsedTime = Math.floor((Date.now() - startTime) / 1000)
  }, 1000)
  
  try {
    // Build filter with date dynamically
    const baseFilter = JSON.parse(searchFilters[selectedSearch.value])
    
    // Only add date filter if checkbox is enabled
    if (useDateFilter.value) {
      const dateFilter = {
        property: 'Pratica.modified',
        value: modifiedDate.value,
        anyMatch: true
      }
      baseFilter.push(dateFilter)
    }
    
    const filter = JSON.stringify(baseFilter)
    
    downloadProgress.value.message = 'Authenticating with Faschim API...'
    
    const response = await fetchDataFromFaschim(filter, selectedSearch.value)
    
    // Update progress from backend response
    if (response.totalRecords !== undefined) {
      downloadProgress.value.totalRecords = response.totalRecords
      downloadProgress.value.recordsFetched = response.recordsFetched || 0
      downloadProgress.value.totalPages = response.totalPages || 1
      downloadProgress.value.currentPage = response.totalPages || 1
      downloadProgress.value.percentage = response.totalRecords > 0 
        ? Math.round((downloadProgress.value.recordsFetched / response.totalRecords) * 100) 
        : 0
      downloadProgress.value.message = 'Processing downloaded data...'
    }
    
    if (response.success) {
      // Fetch the actual data from LiteDB
      downloadProgress.value.message = 'Loading data into grid...'
      const scraperData = await fetchScraperData()
      
      // Extract data records from the response
      const allRecords: any[] = []
      if (Array.isArray(scraperData)) {
        scraperData.forEach((search: any) => {
          if (search.dataRecords && Array.isArray(search.dataRecords)) {
            search.dataRecords.forEach((record: any) => {
              try {
                const parsed = JSON.parse(record.rawContent)
                allRecords.push({
                  id: record.id,
                  searchId: record.searchId,
                  hasConvenzioneFondo: record.hasConvenzioneFondo,
                  extractionDate: record.extractionDate,
                  ...parsed
                })
              } catch (e) {
                console.error('Failed to parse record:', e)
              }
            })
          }
        })
      }
      
      // Store all data
      allRowData.value = allRecords
      
      // Apply extraction date filter
      filterByExtractionDate()
      
      downloadProgress.value.completed = true
      downloadProgress.value.message = `Download completed! ${rowData.value.length} records loaded.`
      
      // Hide progress after 5 seconds
      setTimeout(() => {
        downloadProgress.value.isDownloading = false
      }, 5000)
    } else {
      errorMessage.value = 'Failed to fetch data from Faschim'
      downloadProgress.value.isDownloading = false
    }
  } catch (error: any) {
    console.error('Error fetching from Faschim:', error)
    errorMessage.value = error.message || 'An error occurred while fetching data'
    downloadProgress.value.isDownloading = false
  } finally {
    isLoading.value = false
    if (progressTimer) {
      clearInterval(progressTimer)
      progressTimer = null
    }
  }
}

// Load data on component mount
onMounted(() => {
  refreshGrid()
})
</script>

<style scoped>
.scraper-grid {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.grid-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  padding-bottom: 0.5rem;
  border-bottom: 2px solid #e0e0e0;
}

.grid-header h2 {
  margin: 0;
  color: #2c3e50;
  font-size: 1.5rem;
}

.header-controls {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.search-selector {
  padding: 0.75rem 1rem;
  border: 2px solid #1976d2;
  border-radius: 6px;
  font-size: 1rem;
  color: #2c3e50;
  background-color: white;
  cursor: pointer;
  transition: all 0.3s ease;
  min-width: 200px;
}

.search-selector:hover {
  border-color: #1565c0;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.search-selector:focus {
  outline: none;
  border-color: #1565c0;
  box-shadow: 0 0 0 3px rgba(25, 118, 210, 0.1);
}

.btn-download {
  background-color: #1976d2;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-download:hover:not(:disabled) {
  background-color: #1565c0;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
  transform: translateY(-1px);
}

.btn-download:active:not(:disabled) {
  transform: translateY(0);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-download:disabled {
  background-color: #b0bec5;
  cursor: not-allowed;
  opacity: 0.7;
}

.btn-refresh {
  background-color: #43a047;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-refresh:hover:not(:disabled) {
  background-color: #388e3c;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
  transform: translateY(-1px);
}

.btn-refresh:active:not(:disabled) {
  transform: translateY(0);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-refresh:disabled {
  background-color: #b0bec5;
  cursor: not-allowed;
  opacity: 0.7;
}

.date-filter {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.95rem;
  color: #2c3e50;
  font-weight: 500;
  cursor: pointer;
  user-select: none;
}

.date-checkbox {
  width: 18px;
  height: 18px;
  cursor: pointer;
  accent-color: #1976d2;
}

.date-picker {
  padding: 0.75rem 1rem;
  border: 2px solid #1976d2;
  border-radius: 6px;
  font-size: 1rem;
  color: #2c3e50;
  background-color: white;
  cursor: pointer;
  transition: all 0.3s ease;
  min-width: 160px;
}

.date-picker:hover {
  border-color: #1565c0;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.date-picker:focus {
  outline: none;
  border-color: #1565c0;
  box-shadow: 0 0 0 3px rgba(25, 118, 210, 0.1);
}

.date-picker:disabled {
  background-color: #f5f5f5;
  border-color: #ccc;
  color: #999;
  cursor: not-allowed;
  opacity: 0.6;
}

.filter-delete-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.extraction-filter {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background-color: #f5f5f5;
  border-radius: 6px;
  border: 1px solid #e0e0e0;
  flex-wrap: wrap;
  flex: 1;
}

.extraction-filter .date-filter-group {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.extraction-filter label {
  font-size: 1rem;
  color: #2c3e50;
  font-weight: 500;
}

.btn-clear-filter {
  background-color: #f44336;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-clear-filter:hover:not(:disabled) {
  background-color: #d32f2f;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
  transform: translateY(-1px);
}

.btn-clear-filter:active:not(:disabled) {
  transform: translateY(0);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-clear-filter:disabled {
  background-color: #b0bec5;
  cursor: not-allowed;
  opacity: 0.7;
}

.delete-section {
  background-color: #fff3e0;
  border: 2px solid #ff9800;
  border-radius: 6px;
  padding: 1rem;
  display: flex;
  align-items: center;
  gap: 1rem;
}

.delete-info {
  font-size: 0.95rem;
  color: #e65100;
  font-weight: 500;
}

.btn-delete {
  background-color: #f44336;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-delete:hover:not(:disabled) {
  background-color: #d32f2f;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
  transform: translateY(-1px);
}

.btn-delete:active:not(:disabled) {
  transform: translateY(0);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-delete:disabled {
  background-color: #b0bec5;
  cursor: not-allowed;
  opacity: 0.7;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.6);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  animation: fadeIn 0.2s ease-out;
}

.modal-content {
  background-color: white;
  border-radius: 8px;
  padding: 2rem;
  max-width: 500px;
  width: 90%;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
  animation: slideUp 0.3s ease-out;
}

.modal-content h3 {
  margin: 0 0 1rem 0;
  color: #d32f2f;
  font-size: 1.3rem;
}

.modal-content p {
  margin: 0 0 1.5rem 0;
  color: #2c3e50;
  line-height: 1.6;
  white-space: pre-line;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
}

.btn-confirm-delete {
  background-color: #f44336;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
}

.btn-confirm-delete:hover {
  background-color: #d32f2f;
  transform: translateY(-1px);
}

.btn-cancel {
  background-color: #757575;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
}

.btn-cancel:hover {
  background-color: #616161;
  transform: translateY(-1px);
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

@keyframes slideUp {
  from {
    transform: translateY(20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.error-message {
  background-color: #ffebee;
  color: #c62828;
  padding: 1rem;
  border-radius: 6px;
  margin-bottom: 1rem;
  border-left: 4px solid #d32f2f;
  font-weight: 500;
}

.success-message {
  background-color: #e8f5e9;
  color: #2e7d32;
  padding: 1rem;
  border-radius: 6px;
  margin-bottom: 1rem;
  border-left: 4px solid #4caf50;
  font-weight: 500;
  animation: slideIn 0.3s ease-out;
}

.progress-container {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 1.5rem;
  border-radius: 8px;
  margin-bottom: 1rem;
  color: white;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  animation: slideIn 0.3s ease-out;
}

.progress-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.progress-header h3 {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
}

.progress-time {
  font-size: 0.9rem;
  opacity: 0.9;
  font-weight: 500;
}

.progress-bar {
  width: 100%;
  height: 24px;
  background-color: rgba(255, 255, 255, 0.3);
  border-radius: 12px;
  overflow: hidden;
  margin-bottom: 1rem;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #4caf50 0%, #8bc34a 100%);
  border-radius: 12px;
  transition: width 0.5s ease-out;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding-right: 0.5rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.progress-details {
  display: flex;
  justify-content: space-between;
  font-size: 0.95rem;
  margin-bottom: 0.5rem;
  font-weight: 500;
}

.progress-message {
  text-align: center;
  font-style: italic;
  opacity: 0.95;
  margin-top: 0.5rem;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.grid-container {
  min-height: 400px;
  height: 500px;
}

.row-count {
  display: flex;
  gap: 1rem;
  align-items: center;
  padding: 0.75rem 1rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border-radius: 6px;
  margin-top: 1rem;
  font-size: 0.95rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  flex-shrink: 0;
}

.row-count strong {
  font-weight: 700;
  font-size: 1.1rem;
}
</style>
