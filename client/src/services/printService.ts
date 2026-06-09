import apiClient from './apiClient';

export interface PrintAgent {
  id: number;
  agentKey: string;
  machineName: string;
  displayName: string;
  lastSeenAt: string;
}

export interface PrintJobItem {
  id: number;
  labelType: number;
  status: number;       // 0 Pending,1 Printing,2 Done,3 Failed
  statusName?: string;
  printerName?: string;
  errorMessage?: string | null;
  createdAt: string;
  completedAt?: string | null;
}

const printService = {
  agents: () => apiClient.get('/print/agents').then(r => r.data as PrintAgent[]),
  jobs: (page = 1, pageSize = 20) =>
    apiClient.get('/print/jobs', { params: { page, pageSize } }).then(r => r.data),
  // Koli etiketi baskı işi
  createBoxJob: (shipmentId: number) =>
    apiClient.post('/print/jobs/box', { shipmentId }).then(r => r.data as { jobId: number; message: string }),
};

export default printService;
