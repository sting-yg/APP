import './assets/main.css'

import { createApp } from 'vue'
import App from './App.vue'
import socket from 'vue3-websocket'

const app = createApp(App)

app.use(socket, 'ws://localhost:3000')
app.mount('#app')




