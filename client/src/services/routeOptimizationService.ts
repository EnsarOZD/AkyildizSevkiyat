import apiClient from './apiClient';

export interface ProjectSyncComparisonDto {
  projectCode: string;
  projectName: string;
  currentName: string | null;
  issName: string | null;
  nameChanged: boolean;
  currentAddress: string | null;
  issAddress: string | null;
  addressChanged: boolean;
  hasDifference: boolean;
}

export interface SyncApprovalRequestDto {
  projectCode: string;
  approveNameUpdate: boolean;
  approveAddressUpdate: boolean;
}

export type StartLocationType = 0 | 1 | 2; // CurrentLocation=0, Depot=1, ManualAddress=2

export interface RouteOptimizationRequestDto {
  projectCodes: string[];
  startAddress: string | null;
  vehicleType: string;
  forceBridgeCrossing: boolean;
  startLocationType?: StartLocationType;
  startLatitude?: number | null;
  startLongitude?: number | null;
  returnToStart?: boolean;
  departureTime?: string | null; // "HH:mm"
}

export interface RouteStopDto {
  order: number;
  projectCode: string;
  projectName: string;
  address: string | null;
  estimatedDistanceFromPrevious: number | null;
  estimatedDurationFromPrevious: number | null;
}

export interface TimeWindowWarningDto {
  projectCode: string;
  projectName: string;
  windowStart: string; // "HH:mm"
  windowEnd: string;
  estimatedArrival: string;
  isLate: boolean;
}

export interface RouteOptimizationResultDto {
  optimizedStops: RouteStopDto[];
  totalDistance: number;
  totalDuration: number;
  excludedProjects: string[];
  bridgeNotice: string | null;
  timeWindowWarnings?: TimeWindowWarningDto[] | null;
}

const routeOptimizationService = {
  async compareWithIss(projectCodes: string[]): Promise<ProjectSyncComparisonDto[]> {
    const response = await apiClient.post<ProjectSyncComparisonDto[]>(
      '/route-optimization/compare',
      { projectCodes }
    );
    return response.data;
  },

  async syncApprovals(approvals: SyncApprovalRequestDto[]): Promise<{ updated: number }> {
    const response = await apiClient.post<{ updated: number }>(
      '/route-optimization/sync',
      approvals
    );
    return response.data;
  },

  async optimize(request: RouteOptimizationRequestDto): Promise<RouteOptimizationResultDto> {
    const response = await apiClient.post<RouteOptimizationResultDto>(
      '/route-optimization/optimize',
      request
    );
    return response.data;
  },
};

export default routeOptimizationService;
