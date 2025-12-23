<template>
  <div class="home">
    <div class="grid-container">
      <SecondTableGrid />
    </div>
  </div>
</template>

<script setup lang="ts">
import SecondTableGrid from '@/components/SecondTableGrid.vue'
</script>

<style scoped>
.home {
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
