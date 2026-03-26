<template>
  <router-view></router-view>
  <NotificationContainer />
  <ConfirmDialog />
</template>

<script setup lang="ts">
// Global layout logic if needed
import NotificationContainer from './components/NotificationContainer.vue';
import ConfirmDialog from './components/ConfirmDialog.vue';
</script>

<style>
/* Global styles */
</style>
