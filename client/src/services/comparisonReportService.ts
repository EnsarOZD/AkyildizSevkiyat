import apiClient from './apiClient';

export interface LineComparisonDto {
  issStockCode: string;
  issStockName: string;
  issOrderedQty: number;
  actualStockCode: string | null;
  actualStockName: string | null;
  actualQty: number;
  /** "full_match" | "partial" | "substitution" | "partial_substitution" | "no_fulfillment" | "missing" | "extra" */
  status: string;
  differenceReason: string | null;
}

export interface ShipmentComparisonDto {
  shipmentId: number;
  orderNumber: string | null;
  talepNo: string | null;
  irsaliyeNo: string | null;
  yoneticiMail: string | null;
  missingItemsMailSentAt: string | null;
  projectCode: string;
  projectName: string;
  zoneName: string | null;
  deliveryDate: string;
  shipmentStatus: string;
  /** "full_match" | "has_substitutions" | "has_shortfalls" | "has_missing" | "critical" */
  overallStatus: string;
  lines: LineComparisonDto[];
}

export interface ShipmentComparisonReportDto {
  totalCount: number;
  pageIndex: number;
  totalPages: number;
  totalIssues: number;
  totalMissing: number;
  items: ShipmentComparisonDto[];
}

export interface ComparisonReportFilters {
  dateFrom?: string;
  dateTo?: string;
  projectId?: number;
  zoneId?: number;
  statusFilter?: 'all' | 'issues' | 'missing';
  mailSentFilter?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
}

const comparisonReportService = {
  async get(filters: ComparisonReportFilters): Promise<ShipmentComparisonReportDto> {
    const res = await apiClient.get<ShipmentComparisonReportDto>('/shipments/comparison-report', {
      params: filters,
    });
    return res.data;
  },
};

export default comparisonReportService;
