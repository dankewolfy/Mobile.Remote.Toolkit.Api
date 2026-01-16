<template>
  <section>
    <h2>Dispositivo: {{ serial }}</h2>

    <div v-if="loading">Cargando...</div>

    <div v-else>
      <div v-if="device">
        <p><strong>Nombre:</strong> {{ device.name || device.model }}</p>
        <p><strong>Serial:</strong> {{ device.serial }}</p>
        <p v-if="device.productType">
          <strong>Tipo:</strong> {{ device.productType }}
        </p>
        <p v-if="device.productVersion">
          <strong>Versión:</strong> {{ device.productVersion }}
        </p>
        <div
          style="margin-top: 12px; display: flex; gap: 8px; align-items: center"
        >
          <button
            class="btn"
            @click="handleStartMirror"
            :disabled="actionLoading || isMirroring"
          >
            Iniciar Mirror
          </button>
          <button
            class="btn"
            @click="handleStopMirror"
            :disabled="actionLoading || !isMirroring"
          >
            Detener Mirror
          </button>
          <div
            v-if="actionLoading"
            style="margin-left: 8px; color: var(--muted)"
          >
            Procesando...
          </div>
        </div>
      </div>

      <h3>Estado</h3>
      <pre>{{ statusPretty }}</pre>

      <router-link to="/devices">Volver</router-link>
    </div>
  </section>
</template>

<script lang="ts">
  import {
    defineComponent,
    ref,
    onMounted,
    onBeforeUnmount,
    computed,
    watch,
  } from "vue";
  import {
    getDevice,
    getDeviceStatus,
    startMirror,
    stopMirror,
  } from "../api/android";
  import type { AndroidDevice } from "../api/types";
  import { signalRService } from "../api/signalr";

  export default defineComponent({
    name: "DeviceDetail",
    props: { serial: { type: String, required: false } },
    setup(props: any) {
      const serial = ref<string | null>(props.serial || null);

      const device = ref<AndroidDevice | null>(null);
      const status = ref<Record<string, any> | null>(null);
      const loading = ref(false);
      const actionLoading = ref(false);

      async function load(s: string | null) {
        if (!s) return;
        loading.value = true;
        try {
          device.value = await getDevice(s);
          status.value = await getDeviceStatus(s);
        } finally {
          loading.value = false;
        }
      }

      const isMirroring = computed(
        () => !!status.value && !!status.value["active"]
      );

      async function handleStartMirror() {
        if (!serial.value) return;
        actionLoading.value = true;
        try {
          const res = await startMirror(serial.value, {});
          if (!res.success) throw new Error(res.message || "Start failed");
          // refresh status (server will also push MirrorStarted via SignalR)
          status.value = await getDeviceStatus(serial.value);
          // join device-specific SignalR group to receive per-device events
          await signalRService.joinDeviceGroup(serial.value);
        } catch (err) {
          console.error("Start mirror failed", err);
          window.alert(
            "No se pudo iniciar el mirror: " +
              ((err as any)?.message || String(err))
          );
          return;
        } finally {
          actionLoading.value = false;
        }
      }

      async function handleStopMirror() {
        if (!serial.value) return;
        if (!confirm("Detener mirror para este dispositivo?")) return;
        actionLoading.value = true;
        try {
          const res = await stopMirror(serial.value);
          if (!res.success) throw new Error(res.message || "Stop failed");
          status.value = await getDeviceStatus(serial.value);
          await signalRService.leaveDeviceGroup(serial.value);
        } catch (err) {
          console.error("Stop mirror failed", err);
          window.alert(
            "No se pudo detener el mirror: " +
              ((err as any)?.message || String(err))
          );
          return;
        } finally {
          actionLoading.value = false;
        }
      }

      onMounted(async () => {
        await load(serial.value);
        // join device group to receive per-device events when detail is open
        if (serial.value) {
          try {
            await signalRService.start();
            await signalRService.joinDeviceGroup(serial.value);
            signalRService.on("MirrorStarted", (s: string) => {
              if (s === serial.value)
                status.value = {
                  ...(status.value || {}),
                  active: true,
                  serial: s,
                };
            });
            signalRService.on("MirrorStopped", (s: string) => {
              if (s === serial.value)
                status.value = {
                  ...(status.value || {}),
                  active: false,
                  serial: s,
                };
            });
          } catch (e) {
            console.warn("SignalR device group join failed", e);
          }
        }
      });

      watch(
        () => props.serial,
        (v) => {
          serial.value = v;
          load(v);
        }
      );

      onBeforeUnmount(() => {
        try {
          if (serial.value) signalRService.leaveDeviceGroup(serial.value);
          signalRService.off("MirrorStarted");
          signalRService.off("MirrorStopped");
        } catch (e) {
          // ignore
        }
      });

      const statusPretty = computed(() =>
        JSON.stringify(status.value, null, 2)
      );

      return {
        serial,
        device,
        status,
        loading,
        statusPretty,
        isMirroring,
        actionLoading,
        handleStartMirror,
        handleStopMirror,
      };
    },
  });
</script>

<style scoped>
  pre {
    background: #071025;
    padding: 12px;
    border-radius: 6px;
    color: #e6eef8;
  }
</style>
