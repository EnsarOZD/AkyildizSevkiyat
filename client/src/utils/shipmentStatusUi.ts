export const SHIPMENT_STATUS_LABELS: Record<string, string> = {
    Created:             'Taslak',
    AssignedToWarehouse: 'Depoda',
    Picking:             'Toplama',
    ReadyForDispatch:    'Hazır',
    AssignedToVehicle:   'Araçta',
    Dispatched:          'Yolda',
    Delivered:           'Teslim',
    ReturnedToWarehouse: 'İade',
    Cancelled:           'İptal',
    Passive:             'Pasif',
};

export const SHIPMENT_STATUS_BADGE: Record<string, string> = {
    Created:             'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-300',
    AssignedToWarehouse: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
    Picking:             'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-300',
    ReadyForDispatch:    'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300',
    AssignedToVehicle:   'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300',
    Dispatched:          'bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300',
    Delivered:           'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
    ReturnedToWarehouse: 'bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300',
    Cancelled:           'bg-red-100 text-red-600 dark:bg-red-900/40 dark:text-red-300',
    Passive:             'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400',
};

export const ZONE_PREP_STATUS_LABELS: Record<string, string> = {
    Draft:              'Taslak',
    MicroPicking:       'Mikro Toplama',
    MicroReady:         'Mikro Hazır',
    MacroPicking:       'Makro Toplama',
    GidaHazirlik:       'Gıda Hazırlık',
    ReadyForDriverInfo: 'Şoför Bekliyor',
    ReadyForTransfer:   'Yüklemeye Hazır',
    Dispatched:         'Yolda',
};

export function statusLabel(s: string): string {
    return SHIPMENT_STATUS_LABELS[s] ?? s;
}

export function statusBadge(s: string): string {
    return SHIPMENT_STATUS_BADGE[s] ?? 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-300';
}

export function zonePrepStatusLabel(s: string): string {
    return ZONE_PREP_STATUS_LABELS[s] ?? s;
}

export function formatShortDate(iso: string): string {
    return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' });
}

export function formatDateTime(iso: string): string {
    return new Date(iso).toLocaleString('tr-TR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
}
