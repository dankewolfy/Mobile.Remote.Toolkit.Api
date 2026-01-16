<template>
  <div id="app">
    <header class="app-header">
      <h1>Mobile Remote Toolkit</h1>
      <nav>
        <router-link to="/">Home</router-link>
        <a
          :href="swaggerUrl"
          target="_blank"
          >API Swagger</a
        >
      </nav>
    </header>
    <main>
      <Home />
    </main>
  </div>
</template>

<script lang="ts">
  import { defineComponent, computed, onMounted } from "vue";
  import Home from "./views/Home.vue";
  import { startMonitoring, getMonitoringStatus } from "./api/monitoring";

  export default defineComponent({
    components: { Home },
    setup() {
      const base =
        (import.meta.env.VITE_API_BASE_URL as string) ||
        "https://localhost:5000";
      const swaggerUrl = computed(() => `${base.replace(/\/$/, "")}/swagger`);
      onMounted(async () => {
        try {
          console.log("App: checking monitoring status...");
          const st = await getMonitoringStatus();
          console.log("App: monitoring status", st);
          if (!st.isMonitoring) {
            console.log("App: starting monitoring by default");
            const r = await startMonitoring();
            console.log("App: startMonitoring result", r);
          }
        } catch (err) {
          console.warn("App: monitoring init failed", err);
        }
      });

      return { swaggerUrl };
    },
  });
</script>

<style scoped>
  .app-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 12px 18px;
    background: #0f172a;
    color: #fff;
  }
  nav > * {
    margin-left: 12px;
    color: #93c5fd;
  }
  main {
    padding: 16px;
  }
</style>
