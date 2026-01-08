<template>
  <div id="app" :class="{ 'dark-mode': isDarkMode }">
    <header>
      <div class="header-content">
        <h1>Faschim Pivot Dashboard</h1>
        <button @click="toggleDarkMode" class="theme-toggle" :title="isDarkMode ? 'Switch to Light Mode' : 'Switch to Dark Mode'">
          {{ isDarkMode ? '☀️' : '🌙' }}
        </button>
      </div>
      <nav>
        <router-link to="/">Keplero Full</router-link>
        <router-link to="/keplero-compare">Keplero Compare</router-link>
        <router-link to="/scraper">Scraper</router-link>
      </nav>
    </header>
    <main>
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { useDarkMode } from './composables/useDarkMode'

const { isDarkMode, toggleDarkMode } = useDarkMode()
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
  transition: background-color 0.3s ease;
}

#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  transition: background-color 0.3s ease, color 0.3s ease;
}

/* Dark Mode Styles */
#app.dark-mode {
  background-color: #1a1a1a;
  color: #e0e0e0;
}

#app.dark-mode body {
  background-color: #1a1a1a;
}

#app.dark-mode header {
  background-color: #0d0d0d;
  border-bottom: 1px solid #333;
}

#app.dark-mode main {
  background-color: #1a1a1a;
}

header {
  background-color: #2c3e50;
  color: white;
  padding: 1rem 2rem;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

header h1 {
  font-size: 1.5rem;
  font-weight: 600;
}

.theme-toggle {
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  color: white;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 1.2rem;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.theme-toggle:hover {
  background: rgba(255, 255, 255, 0.2);
  transform: scale(1.05);
}

.theme-toggle:active {
  transform: scale(0.98);
}

nav {
  display: flex;
  gap: 1rem;
}

nav a {
  color: white;
  text-decoration: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  transition: background-color 0.2s;
}

nav a:hover {
  background-color: rgba(255, 255, 255, 0.1);
}

nav a.router-link-active {
  background-color: rgba(255, 255, 255, 0.2);
  font-weight: 600;
}

main {
  flex: 1;
  padding: 2rem;
}

/* AG Grid Dark Theme (Custom) */
:global(.ag-theme-alpine-dark) {
  --ag-background-color: #1f1f1f;
  --ag-header-background-color: #2a2a2a;
  --ag-odd-row-background-color: #242424;
  --ag-header-foreground-color: #e0e0e0;
  --ag-foreground-color: #e0e0e0;
  --ag-border-color: #404040;
  --ag-secondary-border-color: #333;
  --ag-row-hover-color: #2d2d3f;
  --ag-selected-row-background-color: #3a3a4f;
  --ag-range-selection-background-color: rgba(102, 126, 234, 0.2);
  --ag-input-focus-border-color: #667eea;
}

:global(.ag-theme-alpine-dark) .ag-root-wrapper {
  background-color: #1f1f1f;
  color: #e0e0e0;
}

:global(.ag-theme-alpine-dark) .ag-header {
  background-color: #2a2a2a;
  border-bottom-color: #404040;
}

:global(.ag-theme-alpine-dark) .ag-cell {
  border-color: #333;
}

:global(.ag-theme-alpine-dark) .ag-row-odd {
  background-color: #242424;
}

:global(.ag-theme-alpine-dark) .ag-row-hover {
  background-color: #2d2d3f;
}

:global(.ag-theme-alpine-dark) input,
:global(.ag-theme-alpine-dark) select,
:global(.ag-theme-alpine-dark) textarea {
  background-color: #2a2a2a;
  color: #e0e0e0;
  border-color: #404040;
}
</style>
