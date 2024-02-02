<template>
  <tr>
    <td>
      <input v-model="sendCommand" />
      <button @click="sendMessage">Send Command</button> 
    </td>
  </tr>
  <tr>
    <td>
      <button @click="sendMove">Send move</button>
      <button @click="sendLoad">Send load</button>
      <button @click="sendUnload">Send unload</button>
    </td>    
  </tr>
  <tr>
      <td>
        <p>Received Message: {{ receiveMessage }}</p>
      </td>
    </tr>
</template>

<script setup>
import { ref, inject } from 'vue'
import { onMessage, onOpen, onClose, onError } from 'vue3-websocket'

const sendCommand = ref('')
const receiveMessage = ref('')

const socket = inject('socket')

const sendMessage = () => socket.value.send(sendCommand.value)
const sendMove = () => socket.value.send("move")
const sendLoad = () => socket.value.send("load")
const sendUnload = () => socket.value.send("unload")


onOpen(() => {
  console.log('WS connection is stable! ~uWu~')
})

onMessage(message => {
  console.log('Got a message from the WS: ', message)
  receiveMessage.value = message.data
  console.log(receiveMessage.value)
  
})

onClose(() => {
  console.log('No way, connection has been closed 😥')
})

onError(error => {
  console.error('Error: ', error)
})
</script>
