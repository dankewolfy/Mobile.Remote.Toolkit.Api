<template>
  <section class="container">
    <div class="card split">
      <div class="master">
        <h2>Dispositivos</h2>

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

          <div class="monitoring">
            <label class="switch">
              <input
                type="checkbox"
                :checked="status?.isMonitoring"
                @change="toggleMonitoring"
                :disabled="monitoringLoading"
              />
              <span class="slider"></span>
            </label>
            <div class="monitoring-info">
              <div class="muted">Monitoring:</div>
              <div>{{ status?.isMonitoring ? "ON" : "OFF" }}</div>
            </div>
          </div>

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
  </section>
</template>

<script lang="ts">
  import { defineComponent, ref, onMounted, onBeforeUnmount } from "vue";
  import { getDevices, getActiveDevices } from "../api/android";
  import {
    getMonitoringStatus,
    startMonitoring,
    stopMonitoring,
  } from "../api/monitoring";
  import type { AndroidDevice } from "../api/types";
  import DeviceItem from "../components/DeviceItem.vue";
  import DeviceDetail from "./DeviceDetail.vue";
  import { signalRService } from "../api/signalr";

  export default defineComponent({
    name: "Home",
    components: { DeviceItem, DeviceDetail },
    setup() {
      const devices = ref<AndroidDevice[]>([]);
      const loading = ref(true);
      const error = ref<string | null>(null);
      const monitoringLoading = ref(false);
      const status = ref<{ isMonitoring: boolean; timestamp?: string } | null>(
        null
      );

      async function loadAll() {
        console.log("Home: loadAll() called");
        loading.value = true;
        error.value = null;
        try {
          devices.value = await getDevices();
          console.log("Home: received devices", devices.value);
        } catch (err) {
          console.error("Home: loadAll error", err);
          error.value = (err as any)?.message || String(err);
        } finally {
          loading.value = false;
        }
      }

      async function loadActive() {
        console.log("Home: loadActive() called");
        loading.value = true;
        error.value = null;
        try {
          devices.value = await getActiveDevices();
          console.log("Home: received active devices", devices.value);
        } catch (err) {
          console.error("Home: loadActive error", err);
          error.value = (err as any)?.message || String(err);
        } finally {
          loading.value = false;
        }
      }

      async function refreshMonitoringStatus() {
        try {
          const st = await getMonitoringStatus();
          status.value = st;
        } catch (err) {
          console.warn("Failed to fetch monitoring status", err);
        }
      }

      async function toggleMonitoring(e: Event) {
        const turnOn = (e.target as HTMLInputElement).checked;
        monitoringLoading.value = true;
        try {
          if (turnOn) {
            await startMonitoring();
          } else {
            await stopMonitoring();
          }
          await refreshMonitoringStatus();
        } catch (err) {
          console.error("toggleMonitoring error", err);
          await refreshMonitoringStatus();
        } finally {
          monitoringLoading.value = false;
        }
      }

      onMounted(() => {
        console.log("Home: mounted");
        loadAll();
        refreshMonitoringStatus();

        (async () => {
          try {
            await signalRService.start();

            signalRService.on("DeviceConnected", (device: any) => {
              console.info("SignalR DeviceConnected", device);
              const idx = devices.value.findIndex(
                (d) => d.serial === device.serial
              );
              if (idx === -1) {
                devices.value.unshift(device);
              } else {
                devices.value[idx] = { ...devices.value[idx], ...device };
              }
            });

            signalRService.on("DeviceDisconnected", (serial: string) => {
              console.info("SignalR DeviceDisconnected", serial);
              const idx = devices.value.findIndex((d) => d.serial === serial);
              if (idx !== -1) {
                devices.value.splice(idx, 1);
              }
            });

            signalRService.on("DeviceStatusChanged", (status: any) => {
              console.info("SignalR DeviceStatusChanged", status);
              const serial = status?.serial ?? status?.device?.serial;
              if (!serial) return;
              const idx = devices.value.findIndex((d) => d.serial === serial);
              if (idx !== -1) {
                devices.value[idx] = { ...devices.value[idx], ...status };
              }
            });
          } catch (e) {
            console.warn("SignalR start failed", e);
          }
        })();
      });

      onBeforeUnmount(() => {
        try {
          signalRService.off("DeviceConnected");
          signalRService.off("DeviceDisconnected");
          signalRService.off("DeviceStatusChanged");
        } catch (e) {
          // ignore
        }
      });

      const selected = ref<string | null>(null);
      function selectDevice(serial: string) {
        selected.value = serial;
      }

      return {
        devices,
        loading,
        loadAll,
        loadActive,
        selected,
        selectDevice,
        error,
        status,
        monitoringLoading,
        toggleMonitoring,
      };
    },
  });
</script>

<style scoped>
  .muted {
    color: var(--muted);
  }

  /* Toggle switch */
  .switch {
    position: relative;
    display: inline-block;
    width: 44px;
    height: 24px;
  }
  .switch input {
    display: none;
  }
  .slider {
    position: absolute;
    cursor: pointer;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: #ccc;
    transition: 0.2s;
    border-radius: 24px;
  }
  .slider:before {
    position: absolute;
    content: "";
    height: 18px;
    width: 18px;
    left: 3px;
    bottom: 3px;
    background-color: white;
    transition: 0.2s;
    border-radius: 50%;
  }
  .switch input:checked + .slider {
    background-color: #16a34a;
  }
  .switch input:checked + .slider:before {
    transform: translateX(20px);
  }

  .monitoring {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-left: 12px;
  }
  .monitoring-info {
    display: flex;
    flex-direction: column;
    font-size: 0.85rem;
    color: var(--muted);
  }
</style>
