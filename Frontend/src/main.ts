import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

// AG Grid Community styles
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

const app = createApp(App)
app.use(router)
app.mount('#app')
