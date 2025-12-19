<template>
  <div class="pivot-grid">
    <div class="grid-header">
      <div class="header-left">
        <h2>Faschim Pivot Dashboard</h2>
        <button @click="toggleGrouped" class="group-toggle-btn">
          {{ isGrouped ? '📋 Show All Rows' : '📊 Group by Protocollo' }}
        </button>
      </div>
      <div v-if="totalRows !== null" class="row-count">
        <span>Loaded: <strong>{{ loadedRows.toLocaleString() }}</strong></span>
        <span class="separator">|</span>
        <span>Total: <strong>{{ totalRows.toLocaleString() }}</strong></span>
      </div>
    </div>
    <ag-grid-vue
      class="ag-theme-alpine"
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
      :enableRangeSelection="true"
      @grid-ready="onGridReady"
      style="height: 600px;"
    />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { AgGridVue } from 'ag-grid-vue3'
import { fetchPivotData } from '@/services/api'
import type { ColDef, GridApi, GridReadyEvent, IDatasource } from 'ag-grid-community'
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

const gridApi = ref<GridApi | null>(null)
const totalRows = ref<number | null>(null)
const loadedRows = ref<number>(0)
const isGrouped = ref<boolean>(false)

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
    field: 'id', 
    headerName: 'ID', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    width: 80,
    pinned: 'left'
  },
  { 
    field: 'utenteLiquidatore', 
    headerName: 'Utente Liquidatore', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'numeroProtocollo', 
    headerName: 'Protocollo', 
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
      
      const request = {
        startRow: params.startRow!,
        endRow: params.endRow!,
        sortModel: params.sortModel,
        filterModel: params.filterModel
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

const onGridReady = (params: GridReadyEvent) => {
  gridApi.value = params.api
  console.log('Grid ready with Infinite Row Model (AG Grid Community)')
}
</script>

<style scoped>
.pivot-grid {
  width: 100%;
  height: 100%;
  padding: 20px;
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
