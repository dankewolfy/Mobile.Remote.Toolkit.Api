import axios from "axios";

function detectDefaultBase() {
  const envBase = (import.meta.env.VITE_API_BASE_URL as string) || "";
  if (envBase) return envBase;

  // When running inside Electron the app is served from file:// — prefer http
  try {
    if (
      typeof window !== "undefined" &&
      window.location &&
      window.location.protocol === "file:"
    ) {
      return "http://localhost:5000";
    }
  } catch (e) {
    // ignore
  }

  // default to https in browser dev
  return "https://localhost:5000";
}

export const apiClient = axios.create({
  baseURL: detectDefaultBase(),
  headers: { "Content-Type": "application/json" },
  timeout: 10000,
});

apiClient.interceptors.response.use(
  (res) => res,
  (err) => {
    // central place for error transformation / logging
    return Promise.reject(err);
  }
);
