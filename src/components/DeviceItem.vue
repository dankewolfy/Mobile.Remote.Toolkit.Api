<template>
  <li
    class="device-item"
    @click="$emit('select', device.serial)"
  >
    <div class="device-line">
      <div class="device-title">
        {{ device.name || device.model || "Unknown" }}
      </div>
      <div class="device-serial">{{ device.serial }}</div>
    </div>
  </li>
</template>

<script lang="ts">
  import { defineComponent } from "vue";
  import type { AndroidDevice } from "../api/types";

  export default defineComponent({
    name: "DeviceItem",
    emits: ["select"],
    props: {
      device: { type: Object as () => AndroidDevice, required: true },
    },
  });
</script>

<style scoped>
  .device-item {
    list-style: none;
    padding: 10px 12px;
    border-radius: 6px;
    cursor: pointer;
  }
  .device-item + .device-item {
    margin-top: 8px;
  }
  .device-line {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
  .device-title {
    font-weight: 600;
    color: #f1f5f9;
  }
  .device-serial {
    color: var(--muted);
    font-size: 0.9rem;
  }
  .device-item:hover {
    background: rgba(255, 255, 255, 0.02);
  }
</style>
