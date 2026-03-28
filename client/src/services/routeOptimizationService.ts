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

export interface RouteOptimizationRequestDto {
  projectCodes: string[];
  startAddress: string | null;
  vehicleType: string;
  forceBridgeCrossing: boolean;
}

export interface RouteStopDto {
  order: number;
  projectCode: string;
  projectName: string;
  address: string | null;
  estimatedDistanceFromPrevious: number | null;
  estimatedDurationFromPrevious: number | null;
}

export interface RouteOptimizationResultDto {
  optimizedStops: RouteStopDto[];
  totalDistance: number;
  totalDuration: number;
  excludedProjects: string[];
  bridgeNotice: string | null;
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
