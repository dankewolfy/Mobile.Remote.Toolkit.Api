import { apiClient } from "./index";
import type { AndroidDevice, ActionResponse } from "./types";

export async function getDevices(): Promise<AndroidDevice[]> {
  const res = await apiClient.get<AndroidDevice[]>("/api/Android/devices");
  return res.data;
}

export async function getDevice(serial: string): Promise<AndroidDevice> {
  const res = await apiClient.get<AndroidDevice>(
    `/api/Android/devices/${encodeURIComponent(serial)}/info`
  );
  return res.data;
}

export async function getActiveDevices(): Promise<AndroidDevice[]> {
  const res = await apiClient.get<AndroidDevice[]>(
    "/api/Android/devices/active"
  );
  return res.data;
}

export async function getDeviceStatus(
  serial: string
): Promise<Record<string, any>> {
  const res = await apiClient.get<Record<string, any>>(
    `/api/Android/devices/${encodeURIComponent(serial)}/status`
  );
  return res.data;
}

export async function startMirror(
  serial: string,
  options: Record<string, any> = {}
): Promise<ActionResponse> {
  const res = await apiClient.post<ActionResponse>(
    `/api/Android/devices/${encodeURIComponent(serial)}/mirror/start`,
    { options }
  );
  return res.data;
}

export async function stopMirror(serial: string): Promise<ActionResponse> {
  const res = await apiClient.post<ActionResponse>(
    `/api/Android/devices/${encodeURIComponent(serial)}/mirror/stop`
  );
  return res.data;
}

export async function takeScreenshot(
  serial: string,
  filename?: string
): Promise<Blob> {
  const res = await apiClient.post(
    `/api/Android/devices/${encodeURIComponent(serial)}/screenshot`,
    { filename },
    { responseType: "blob" }
  );
  return res.data;
}

export async function executeAdb(
  serial: string,
  command: string
): Promise<ActionResponse> {
  const res = await apiClient.post<ActionResponse>(
    `/api/Android/devices/${encodeURIComponent(serial)}/adb`,
    { command }
  );
  return res.data;
}
