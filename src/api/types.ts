export interface AndroidDevice {
  serial: string;
  name?: string;
  model?: string;
  productVersion?: string;
  productType?: string;
}

export interface ActionResponse {
  success: boolean;
  message?: string;
}

export type DeviceStatus = Record<string, any>;
