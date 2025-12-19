<template>
  <div class="pivot-grid">
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

const columnDefs = ref<ColDef[]>([
  { 
    field: 'id', 
    headerName: 'ID', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    width: 80,
    hide: true
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

      const response = await fetchPivotData(request)
      
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

.ag-theme-alpine {
  width: 100%;
  height: 100%;
}
</style>
