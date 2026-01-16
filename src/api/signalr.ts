import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";
import { apiClient } from "./index";

type Callback = (...args: any[]) => void;

class SignalRService {
  private connection: HubConnection | null = null;

  private listeners = new Map<string, Set<Callback>>();

  private buildUrl() {
    const base = (apiClient.defaults.baseURL || "").replace(/\/$/, "");
    return `${base}/hubs/android`;
  }

  async start() {
    if (this.connection) return;
    const url = this.buildUrl();
    this.connection = new HubConnectionBuilder()
      .withUrl(url, { withCredentials: true })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build();

    // re-register listeners after reconnect
    this.connection.onreconnected(() => {
      this.listeners.forEach((set, name) => {
        set.forEach((cb) => this.connection?.on(name, cb));
      });
    });

    // register existing listeners
    this.listeners.forEach((set, name) => {
      set.forEach((cb) => this.connection?.on(name, cb));
    });

    await this.connection.start();
  }

  async stop() {
    if (!this.connection) return;
    await this.connection.stop();
    this.connection = null;
  }

  on(eventName: string, callback: Callback) {
    let set = this.listeners.get(eventName);
    if (!set) {
      set = new Set();
      this.listeners.set(eventName, set);
    }
    set.add(callback);
    if (this.connection) this.connection.on(eventName, callback);
  }

  off(eventName: string, callback?: Callback) {
    const set = this.listeners.get(eventName);
    if (!set) return;
    if (callback) {
      set.delete(callback);
      if (this.connection) this.connection.off(eventName, callback);
    } else {
      // remove all
      set.forEach((cb) => {
        if (this.connection) this.connection.off(eventName, cb);
      });
      this.listeners.delete(eventName);
    }
  }

  async joinDeviceGroup(serial: string) {
    if (!this.connection) return;
    try {
      await this.connection.invoke("JoinDeviceGroup", serial);
    } catch (e) {
      console.warn("joinDeviceGroup failed", e);
    }
  }

  async leaveDeviceGroup(serial: string) {
    if (!this.connection) return;
    try {
      await this.connection.invoke("LeaveDeviceGroup", serial);
    } catch (e) {
      console.warn("leaveDeviceGroup failed", e);
    }
  }

  async getDeviceStatus(serial: string) {
    if (!this.connection) return null;
    try {
      await this.connection.invoke("GetDeviceStatus", serial);
    } catch (e) {
      console.warn("getDeviceStatus failed", e);
    }
  }
}

export const signalRService = new SignalRService();
export default signalRService;
