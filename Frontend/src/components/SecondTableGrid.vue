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
      @grid-ready="onGridReady"
      style="height: 500px; min-height: 400px;"
    />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { AgGridVue } from 'ag-grid-vue3'
import { fetchSecondTableData } from '@/services/api'
import type { ColDef, GridApi, GridReadyEvent, IDatasource } from 'ag-grid-community'
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

const gridApi = ref<GridApi | null>(null)

// TODO: Update these columns to match your SecondTable schema
const columnDefs = ref<ColDef[]>([
  { 
    field: 'id', 
    headerName: 'ID', 
    filter: 'agNumberColumnFilter', 
    sortable: true,
    sort: 'asc'
  },
  { 
    field: 'column1', 
    headerName: 'Column 1', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'column2', 
    headerName: 'Column 2', 
    filter: 'agTextColumnFilter', 
    sortable: true 
  },
  { 
    field: 'column3', 
    headerName: 'Column 3', 
    filter: 'agNumberColumnFilter', 
    sortable: true 
  },
  { 
    field: 'column4', 
    headerName: 'Column 4', 
    filter: 'agDateColumnFilter', 
    sortable: true 
  }
])

const defaultColDef = ref<ColDef>({
  flex: 1,
  minWidth: 100,
  resizable: true,
  sortable: true,
  filter: true
})

const datasource = ref<IDatasource>({
  rowCount: undefined,
  getRows: async (params) => {
    try {
      console.log('Fetching second table rows from', params.startRow, 'to', params.endRow)
      
      const request = {
        startRow: params.startRow!,
        endRow: params.endRow!,
        sortModel: params.sortModel,
        filterModel: params.filterModel
      }

      const response = await fetchSecondTableData(request)
      
      let lastRow = -1
      if (response.rowData.length < (params.endRow! - params.startRow!)) {
        lastRow = params.startRow! + response.rowData.length
      } else if (response.rowCount !== undefined) {
        lastRow = response.rowCount
      }

      params.successCallback(response.rowData, lastRow)
      
      console.log('Loaded', response.rowData.length, 'rows from second table. Total:', response.rowCount)
    } catch (error) {
      console.error('Error fetching second table data:', error)
      params.failCallback()
    }
  }
})

const onGridReady = (params: GridReadyEvent) => {
  gridApi.value = params.api
  console.log('Second table grid ready with Infinite Row Model')
}
</script>

<style scoped>
.pivot-grid {
  width: 100%;
  height: 100%;
  overflow-y: auto;
}

.ag-theme-alpine {
  width: 100%;
  height: 100%;
}
</style>
