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
      style="height: 600px;"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { AgGridVue } from 'ag-grid-vue3'
import { fetchKepleroCompareData, fetchKepleroCompareStatistics } from '@/services/api'
import type { ColDef, GridApi, GridReadyEvent, IDatasource } from 'ag-grid-community'
import type { KepleroCompareStatistics } from '@/services/api'
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

const gridApi = ref<GridApi | null>(null)
const statistics = ref<KepleroCompareStatistics | null>(null)
const loadingStats = ref<boolean>(true)
const totalRows = ref<number | null>(null)
const loadedRows = ref<number>(0)

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
      
      const request = {
        startRow: params.startRow!,
        endRow: params.endRow!,
        sortModel: params.sortModel,
        filterModel: params.filterModel
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

const onGridReady = (params: GridReadyEvent) => {
  gridApi.value = params.api
  console.log('Keplero Compare grid ready with Infinite Row Model')
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

onMounted(() => {
  console.log('Component mounted, loading statistics')
  loadStatistics()
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

.ag-theme-alpine {
  width: 100%;
  height: 100%;
}
</style>
