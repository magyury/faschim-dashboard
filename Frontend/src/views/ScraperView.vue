<template>
  <div class="scraper-view">
    <div class="grid-container">
      <ScraperGrid />
    </div>
  </div>
</template>

<script setup lang="ts">
import ScraperGrid from '@/components/ScraperGrid.vue'
</script>

<style scoped>
.scraper-view {
  width: 100%;
}

.grid-container {
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  padding: 1rem;
  height: calc(100vh - 120px);
}
</style>
