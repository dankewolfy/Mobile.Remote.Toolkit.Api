<template>
  <section>
    <h2>Dispositivos Android</h2>
    <div v-if="loading">Cargando...</div>
    <div v-else>
      <div class="card split">
        <div class="master">
          <h3>Dispositivos</h3>
          <div class="toolbar">
            <button
              class="btn"
              @click="loadAll"
              :disabled="loading"
            >
              Cargar todos
            </button>
            <button
              class="btn"
              @click="loadActive"
              :disabled="loading"
            >
              Solo activos
            </button>
            <div style="flex: 1"></div>
            <div class="muted">{{ devices.length }} dispositivos</div>
          </div>

          <div v-if="loading">Cargando...</div>
          <ul class="list">
            <DeviceItem
              v-for="d in devices"
              :key="d.serial"
              :device="d"
              @select="selectDevice"
            />
          </ul>
        </div>

        <div class="detail">
          <DeviceDetail :serial="selected" />
        </div>
      </div>
    </div>
  </section>
</template>

<script lang="ts">
  import { defineComponent, ref, onMounted } from "vue";
  import { getDevices, getActiveDevices } from "../api/android";
  import type { AndroidDevice } from "../api/types";
  import DeviceItem from "../components/DeviceItem.vue";
  import DeviceDetail from "./DeviceDetail.vue";

  export default defineComponent({
    name: "Devices",
    components: { DeviceItem, DeviceDetail },
    setup() {
      const devices = ref<AndroidDevice[]>([]);
      const loading = ref(true);

      async function loadAll() {
        loading.value = true;
        try {
          devices.value = await getDevices();
        } finally {
          loading.value = false;
        }
      }

      async function loadActive() {
        loading.value = true;
        try {
          devices.value = await getActiveDevices();
        } finally {
          loading.value = false;
        }
      }

      onMounted(loadAll);

      const selected = ref<string | null>(null);

      function selectDevice(serial: string) {
        selected.value = serial;
      }

      return { devices, loading, loadAll, loadActive, selected, selectDevice };
    },
  });
</script>

<style scoped>
  /* keep view-specific minimal styles here if necessary */
</style>
