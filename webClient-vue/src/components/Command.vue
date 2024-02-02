<template>
    <button type="button" @click="">
        move
    </button>
    <button type="button">
        load
    </button>
    <button type="button">
        unload
    </button>
</template>

<script>

const sendMessage = () => socket.value.send(button.value)


</script>

<style>

</style>