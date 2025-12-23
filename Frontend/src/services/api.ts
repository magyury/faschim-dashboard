import axios from 'axios'

const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json'
  }
})

export interface PivotRequest {
  startRow: number
  endRow: number
  sortModel?: Array<{ colId: string; sort: string }>
  filterModel?: Record<string, any> | null
}

export interface PivotResponse {
  rowData: any[]
  rowCount: number
}

export const fetchPivotData = async (request: PivotRequest): Promise<PivotResponse> => {
  try {
    const response = await apiClient.post<PivotResponse>('/pivot-data', request)
    return response.data
  } catch (error) {
    console.error('API Error:', error)
    throw error
  }
}

// Second table API
export const fetchSecondTableData = async (request: PivotRequest): Promise<PivotResponse> => {
  try {
    const response = await apiClient.post<PivotResponse>('/pivot-data-second', request)
    return response.data
  } catch (error) {
    console.error('API Error (Second Table):', error)
    throw error
  }
}

export const checkHealth = async () => {
  try {
    const response = await apiClient.get('/health')
    return response.data
  } catch (error) {
    console.error('Health check failed:', error)
    throw error
  }
}

export const fetchStatoPraticaValues = async (): Promise<string[]> => {
  try {
    const response = await apiClient.get<string[]>('/stato-pratica-values')
    return response.data
  } catch (error) {
    console.error('Error fetching StatoPratica values:', error)
    throw error
  }
}

// Keplero Compare APIs
export interface KepleroCompareStatistics {
  totalRecords: number
  byCoda: Array<{ coda: string; count: number }>
  byStatoPratica: Array<{ statoPratica: string; count: number }>
  byStato: Array<{ stato: string; count: number }>
  byStatoPraticaKeplero: Array<{ statoPraticaKeplero: string; count: number }>
  byEsito: Array<{ esito: string; count: number }>
}

export const fetchKepleroCompareData = async (request: PivotRequest): Promise<PivotResponse> => {
  const response = await apiClient.post<PivotResponse>('/keplero-compare', request)
  return response.data
}

export const fetchKepleroCompareStatistics = async (): Promise<KepleroCompareStatistics> => {
  const response = await apiClient.get<KepleroCompareStatistics>('/keplero-compare/statistics')
  return response.data
}

export default apiClient
