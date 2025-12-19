<template>
  <div id="app">
    <header>
      <h1>Faschim Pivot Dashboard</h1>
    </header>
    <main>
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
// No script needed for this component
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  background-color: #f5f5f5;
}

#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

header {
  background-color: #2c3e50;
  color: white;
  padding: 1rem 2rem;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

header h1 {
  font-size: 1.5rem;
  font-weight: 600;
}

main {
  flex: 1;
  padding: 2rem;
}
</style>
