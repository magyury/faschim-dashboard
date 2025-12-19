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

export const checkHealth = async () => {
  try {
    const response = await apiClient.get('/health')
    return response.data
  } catch (error) {
    console.error('Health check failed:', error)
    throw error
  }
}

export default apiClient
