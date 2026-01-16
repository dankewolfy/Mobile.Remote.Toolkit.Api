import { createRouter, createWebHistory } from "vue-router";
import Home from "../views/Home.vue";
import Devices from "../views/Devices.vue";
import DeviceDetail from "../views/DeviceDetail.vue";

const routes = [
  { path: "/", name: "Home", component: Home },
  { path: "/devices", name: "Devices", component: Devices },
  {
    path: "/devices/:serial",
    name: "DeviceDetail",
    component: DeviceDetail,
    props: true,
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
