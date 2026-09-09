import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import { loadConfig } from './config'

// The api urls are only known after the infrastructure is deployed, so they are loaded first.
await loadConfig()

createApp(App).mount('#app')
