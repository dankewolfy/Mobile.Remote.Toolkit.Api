import { apiClient } from "./index";
import type { ActionResponse } from "./types";

export async function getMonitoringStatus(): Promise<{
  isMonitoring: boolean;
  timestamp: string;
}> {
  const res = await apiClient.get("/api/monitoring/status");
  return res.data;
}

export async function startMonitoring(): Promise<ActionResponse> {
  const res = await apiClient.post<ActionResponse>("/api/monitoring/start");
  return res.data;
}

export async function stopMonitoring(): Promise<ActionResponse> {
  const res = await apiClient.post<ActionResponse>("/api/monitoring/stop");
  return res.data;
}

export default { getMonitoringStatus, startMonitoring, stopMonitoring };
