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

export const fetchCodaValues = async (): Promise<string[]> => {
  try {
    const response = await apiClient.get<string[]>('/keplero-compare/coda-values')
    return response.data
  } catch (error) {
    console.error('Error fetching Coda values:', error)
    throw error
  }
}

export const fetchStatoValues = async (): Promise<string[]> => {
  try {
    const response = await apiClient.get<string[]>('/keplero-compare/stato-values')
    return response.data
  } catch (error) {
    console.error('Error fetching Stato values:', error)
    throw error
  }
}

export const fetchEsitoValues = async (): Promise<string[]> => {
  try {
    const response = await apiClient.get<string[]>('/keplero-compare/esito-values')
    return response.data
  } catch (error) {
    console.error('Error fetching Esito values:', error)
    throw error
  }
}

export const fetchStatoKepleroValues = async (): Promise<string[]> => {
  try {
    const response = await apiClient.get<string[]>('/keplero-compare/stato-keplero-values')
    return response.data
  } catch (error) {
    console.error('Error fetching StatoPratica_Keplero values:', error)
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

// Scraper APIs
export interface ScraperData {
  id: number
  url?: string
  dataSource?: string
  fetchDate?: string
  status?: string
  content?: string
  errorMessage?: string
  createdAt?: string
}

export interface ScraperFetchResponse {
  success: boolean
  searchId?: number
  searchName?: string
  totalRecords: number
  recordsFetched: number
  totalPages: number
  faschimSuccess?: boolean
  faschimCodErrore?: number
  faschimMessage?: string
}

export const fetchDataFromFaschim = async (filter?: string, searchName?: string): Promise<ScraperFetchResponse> => {
  try {
    const params = new URLSearchParams()
    if (filter) {
      params.append('filter', filter)
    }
    if (searchName) {
      params.append('searchName', searchName)
    }
    
    const url = `/scraper/fetch-from-faschim${params.toString() ? '?' + params.toString() : ''}`
    const response = await apiClient.get<ScraperFetchResponse>(url)
    return response.data
  } catch (error) {
    console.error('Error fetching data from Faschim:', error)
    throw error
  }
}

export const fetchScraperData = async (): Promise<ScraperData[]> => {
  try {
    const response = await apiClient.get<ScraperData[]>('/scraper/data')
    return response.data
  } catch (error) {
    console.error('Error fetching scraper data:', error)
    throw error
  }
}

export const deleteScraperData = async (recordIds: number[]): Promise<{ success: boolean; deletedCount: number }> => {
  try {
    const response = await apiClient.post('/scraper/data/delete', recordIds)
    return response.data
  } catch (error) {
    console.error('Error deleting scraper data:', error)
    throw error
  }
}

// Keplero Compare Mismatch
export interface MismatchResponse {
  success: boolean
  protocollos: string[]
  count: number
}

export const fetchMismatchData = async (): Promise<MismatchResponse> => {
  try {
    const response = await apiClient.get<MismatchResponse>('/keplero-compare/mismatch')
    return response.data
  } catch (error) {
    console.error('Error fetching mismatch data:', error)
    throw error
  }
}

// Keplero Compare Create
export interface CreateCompareResponse {
  success: boolean
  message: string
  timestamp?: string
  cancelled?: boolean
}

export const createCompareData = async (signal?: AbortSignal): Promise<CreateCompareResponse> => {
  try {
    const response = await apiClient.post<CreateCompareResponse>(
      '/keplero-compare/create-compare',
      {},
      { 
        signal,
        timeout: 600000 // 10 minutes timeout
      }
    )
    return response.data
  } catch (error: any) {
    if (error.name === 'CanceledError' || error.code === 'ERR_CANCELED') {
      return { success: false, message: 'Operation was cancelled by user', cancelled: true }
    }
    console.error('Error creating compare data:', error)
    throw error
  }
}

// Delta Faschim APIs
export const fetchDeltaFaschimData = async (request: PivotRequest): Promise<PivotResponse> => {
  const response = await apiClient.post<PivotResponse>('/delta-faschim', request)
  return response.data
}

export default apiClient
