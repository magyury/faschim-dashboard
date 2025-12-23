<template>
  <div class="keplero-compare">
    <div class="grid-container">
      <h2>Keplero Compare</h2>
      <KepleroCompareGrid />
    </div>
  </div>
</template>

<script setup lang="ts">
import KepleroCompareGrid from '@/components/KepleroCompareGrid.vue'
</script>

<style scoped>
.keplero-compare {
  width: 100%;
}

.grid-container {
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  padding: 1rem;
  height: calc(100vh - 120px);
}

.grid-container h2 {
  margin: 0 0 1rem 0;
  color: #2c3e50;
  font-size: 1.25rem;
}
</style>
