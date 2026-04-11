import apiClient from './apiClient';

export interface StartSessionRequest {
  qrCode: string;
  latitude: number;
  longitude: number;
  deviceFingerprint?: string;
}

export interface StartSessionResult {
  sessionId: string;
  vehiclePlateNumber: string;
  startTime: string;
}

export interface EndSessionResult {
  sessionId: string;
  totalDurationMinutes: number;
}

export interface ActiveSessionDto {
  sessionId: string;
  vehicleId: number;
  plateNumber: string;
  startTime: string;
  startLatitude: number;
  startLongitude: number;
  elapsedMinutes: number;
}

const driverSessionService = {
  async startSession(data: StartSessionRequest): Promise<StartSessionResult> {
    const res = await apiClient.post<StartSessionResult>('/driver/sessions/start', data);
    return res.data;
  },

  async endSession(qrCode: string, latitude: number, longitude: number): Promise<EndSessionResult> {
    const res = await apiClient.post<EndSessionResult>('/driver/sessions/end', {
      qrCode,
      latitude,
      longitude,
    });
    return res.data;
  },

  async getActiveSession(): Promise<ActiveSessionDto | null> {
    const res = await apiClient.get<ActiveSessionDto | null>('/driver/sessions/active');
    return res.data;
  },
};

export default driverSessionService;
