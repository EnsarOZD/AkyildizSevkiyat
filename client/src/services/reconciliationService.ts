import apiClient from './apiClient';

export type ReconciliationCheckType =
  | 'IssQtyMismatch'
  | 'PickingIncomplete'
  | 'NetsisTransferMissing'
  | 'IrsaliyeMissing'
  | 'IssCoverageGap';

export type ReconciliationSeverity = 'Warning' | 'Error';
export type ReconciliationStatus = 'Open' | 'Acknowledged' | 'AutoResolved';

export interface ReconciliationIssueDto {
  id: number;
  issueKey: string;
  checkType: ReconciliationCheckType;
  severity: ReconciliationSeverity;
  status: ReconciliationStatus;
  shipmentId: number | null;
  shipmentLineId: number | null;
  issOrderLineId: number | null;
  description: string;
  expectedValue: string | null;
  actualValue: string | null;
  detectedAt: string;
  acknowledgedAt: string | null;
  acknowledgementNote: string | null;
}

export interface ReconciliationIssuesResult {
  items: ReconciliationIssueDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  openSummary: Record<ReconciliationCheckType, number>;
}

export interface RunReconciliationResult {
  newIssues: number;
  autoResolved: number;
  totalChecked: number;
  durationMs: number;
}

export interface ReconciliationIssuesFilter {
  checkType?: ReconciliationCheckType;
  status?: ReconciliationStatus;
  severity?: ReconciliationSeverity;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

const reconciliationService = {
  async getIssues(filter: ReconciliationIssuesFilter): Promise<ReconciliationIssuesResult> {
    const response = await apiClient.get('/reconciliation/issues', { params: filter });
    return response.data;
  },

  async runChecks(fromDate?: string, toDate?: string): Promise<RunReconciliationResult> {
    const response = await apiClient.post('/reconciliation/run', null, {
      params: { fromDate, toDate }
    });
    return response.data;
  },

  async acknowledge(id: number, note: string): Promise<void> {
    await apiClient.post(`/reconciliation/issues/${id}/acknowledge`, { note });
  },
};

export default reconciliationService;
