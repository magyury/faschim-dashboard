import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import KepleroCompareView from '../views/KepleroCompareView.vue'
import ScraperView from '../views/ScraperView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView
    },
    {
      path: '/keplero-compare',
      name: 'keplero-compare',
      component: KepleroCompareView
    },
    {
      path: '/scraper',
      name: 'scraper',
      component: ScraperView
    }
  ]
})

export default router
