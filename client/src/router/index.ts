import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { useNotificationStore } from '../stores/notification';
import LoginView from '../views/LoginView.vue';

const router = createRouter({
    history: createWebHistory(),
    routes: [
        {
            path: '/login',
            name: 'Login',
            component: LoginView,
            meta: { title: 'Giriş' }
        },
        {
            // Nakliyeci teslim fotoğrafı yükleme — login gerektirmeyen public sayfa
            path: '/teslim/:token',
            name: 'FreightDeliveryUpload',
            component: () => import('../views/FreightDeliveryUploadView.vue'),
            meta: { title: 'Teslim Fotoğrafı' }
        },
        {
            path: '/',
            component: () => import('../layouts/DefaultLayout.vue'),
            meta: { requiresAuth: true },
            children: [
                {
                    path: '',
                    name: 'Dashboard',
                    component: () => import('../views/HomeView.vue'),
                    meta: { title: 'Dashboard' }
                },
                {
                    path: 'shipments',
                    name: 'ShipmentList',
                    component: () => import('../views/ShipmentListView.vue'),
                    meta: { title: 'Sevkiyatlar', roles: ['Admin', 'Accounting', 'Manager', 'Driver', 'Warehouse'] }
                },
                {
                    path: 'notifications',
                    name: 'Notifications',
                    component: () => import('../views/NotificationsView.vue'),
                    meta: { title: 'Bildirimler' }
                },
                {
                    path: 'freight-deliveries',
                    name: 'FreightDeliveries',
                    component: () => import('../views/FreightDeliveriesView.vue'),
                    meta: { title: 'Nakliye Teslim Linkleri', roles: ['Admin', 'Manager', 'Accounting', 'Warehouse'] }
                },
                {
                    path: 'carriers',
                    name: 'Carriers',
                    component: () => import('../views/CarriersView.vue'),
                    meta: { title: 'Nakliyeciler', roles: ['Admin', 'Manager', 'Accounting'] }
                },
                {
                    path: 'shipments/:id',
                    name: 'ShipmentDetail',
                    component: () => import('../views/ShipmentDetailView.vue'),
                    meta: { title: 'Sevkiyat Detayı', roles: ['Admin', 'Accounting', 'Manager', 'Driver', 'Warehouse', 'Driver'], breadcrumb: [{ label: 'Sevkiyatlar', to: '/shipments' }] }
                },
                {
                    path: 'settings',
                    name: 'Settings',
                    component: () => import('../views/SettingsView.vue'),
                    meta: { title: 'Tanımlamalar', roles: ['Admin', 'Manager', 'Accounting'] }
                },
                {
                    path: 'clothing-keywords',
                    name: 'ClothingKeywords',
                    component: () => import('../views/ClothingKeywordsView.vue'),
                    meta: { title: 'Kıyafet Vurgu Kelimeleri', roles: ['Admin', 'Manager'] }
                },
                {
                    path: 'defined-reasons',
                    name: 'DefinedReasons',
                    component: () => import('../views/DefinedReasonsView.vue'),
                    meta: { title: 'Sebep Tanımları', roles: ['Admin', 'Manager'] }
                },
                {
                    path: 'clothing-prep',
                    name: 'ClothingPrep',
                    component: () => import('../views/ClothingPrepView.vue'),
                    meta: { title: 'Kıyafet Hazırlık', roles: ['Admin', 'Manager', 'Accounting', 'Warehouse'] }
                },
                {
                    path: 'clothing-picking/manager',
                    name: 'PickingManager',
                    component: () => import('../views/PickingManagerView.vue'),
                    meta: { title: 'Kıyafet Toplama Yönetimi', roles: ['Admin', 'Manager'] }
                },
                {
                    path: 'clothing-picking/pick',
                    name: 'PickerView',
                    component: () => import('../views/PickerView.vue'),
                    meta: { title: 'Toplama', roles: ['Admin', 'Manager', 'Accounting', 'Warehouse'] }
                },
                {
                    path: 'clothing-picking/closing',
                    name: 'ClosingView',
                    component: () => import('../views/ClosingView.vue'),
                    meta: { title: 'Kapama', roles: ['Admin', 'Manager', 'Accounting', 'Warehouse'] }
                },
                {
                    path: 'clothing-picking/shortages',
                    name: 'ShortageQueue',
                    component: () => import('../views/ShortageQueueView.vue'),
                    meta: { title: 'Eksik Ürün Kuyruğu', roles: ['Admin', 'Manager', 'Accounting', 'Warehouse'] }
                },
                {
                    path: 'zones',
                    redirect: '/settings',
                },
                {
                    path: 'projects/zone-mapping',
                    name: 'ProjectMapping',
                    component: () => import('../views/ProjectMappingView.vue'),
                    meta: { title: 'Proje-Bölge Eşleşmesi', roles: ['Admin', 'Manager', 'Accounting'] }
                },
                {
                    path: 'zones/project-order',
                    name: 'ZoneProjectOrder',
                    component: () => import('../views/ZoneProjectOrderView.vue'),
                    meta: { title: 'Teslimat Sırası', roles: ['Admin', 'Manager', 'Accounting'] }
                },
                {
                    path: 'projects/coordinates',
                    name: 'CoordinateValidation',
                    component: () => import('../views/CoordinateValidationView.vue'),
                    meta: { title: 'Koordinat Doğrulama', roles: ['Admin', 'Manager'] }
                },
                {
                    path: 'orders/import',
                    name: 'OrderImport',
                    component: () => import('../views/OrderImportView.vue'),
                    meta: { title: 'ISS Entegrasyon', roles: ['Admin', 'Accounting', 'Manager', 'Driver'] }
                },
                {
                    path: 'stocks',
                    name: 'StockManagement',
                    component: () => import('../views/StockManagementView.vue'),
                    meta: { title: 'Stok Yönetimi', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'stocks/mappings',
                    name: 'StockMappings',
                    component: () => import('../views/StockMappingView.vue'),
                    meta: { title: 'Stok Eşleştirme', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'warehouse',
                    name: 'WarehouseDashboard',
                    component: () => import('../views/WarehouseDashboard.vue'),
                    meta: { title: 'Depo Hazırlık', roles: ['Admin', 'Accounting', 'Warehouse', 'Manager', 'Driver'] }
                },
                {
                    path: 'transport',
                    redirect: '/settings',
                },
                {
                    path: 'purchase-orders',
                    name: 'PurchaseOrders',
                    component: () => import('../views/PurchaseOrdersView.vue'),
                    meta: { title: 'Satınalma Siparişleri', roles: ['Admin', 'Accounting', 'Manager', 'Warehouse'] }
                },
                {
                    path: 'goods-receipts',
                    name: 'GoodsReceipts',
                    component: () => import('../views/GoodsReceiptsView.vue'),
                    meta: { title: 'Mal Kabul İrsaliyeleri', roles: ['Admin', 'Warehouse', 'Manager', 'Accounting'] }
                },
                {
                    path: 'goods-receipts/intake',
                    name: 'MalKabulDashboard',
                    component: () => import('../views/MalKabulDashboardView.vue'),
                    meta: { title: 'Mal Kabul Merkezi', roles: ['Admin', 'Warehouse', 'Manager', 'Accounting'], breadcrumb: [{ label: 'Mal Kabul', to: '/goods-receipts' }] }
                },
                {
                    path: 'goods-receipts/select-po',
                    name: 'PurchaseOrderSelection',
                    component: () => import('../views/PurchaseOrderSelectionView.vue'),
                    meta: { title: 'Sipariş Seçimi', roles: ['Admin', 'Warehouse', 'Manager'], breadcrumb: [{ label: 'Mal Kabul', to: '/goods-receipts' }] }
                },
                {
                    path: 'purchase-orders/:id',
                    name: 'PurchaseOrderDetail',
                    component: () => import('../views/PurchaseOrderDetailView.vue'),
                    meta: { title: 'Sipariş Detayı', roles: ['Admin', 'Accounting', 'Manager', 'Warehouse'] }
                },
                {
                    path: 'goods-receipts/list',
                    redirect: '/goods-receipts',
                },
                {
                    path: 'goods-receipts/:id',
                    name: 'GoodsReceiptDetail',
                    component: () => import('../views/GoodsReceiptDetailView.vue'),
                    meta: { title: 'İrsaliye Detayı', roles: ['Admin', 'Warehouse', 'Manager', 'Accounting'] }
                },
                {
                    path: 'goods-receipts/:id/print',
                    name: 'GoodsReceiptPrint',
                    component: () => import('../views/GoodsReceiptPrintView.vue'),
                    meta: { title: 'İrsaliye Yazdır', roles: ['Admin', 'Warehouse', 'Manager', 'Accounting'] }
                },
                {
                    path: 'suppliers',
                    name: 'SupplierManagement',
                    component: () => import('../views/SupplierManagementView.vue'),
                    meta: { title: 'Tedarikçi Yönetimi', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'customers',
                    name: 'CustomerManagement',
                    component: () => import('../views/CustomersView.vue'),
                    meta: { title: 'Müşteriler', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'system/institution-cari-mappings',
                    name: 'InstitutionCariMappings',
                    component: () => import('../views/InstitutionCariMappingsView.vue'),
                    meta: { title: 'Netsis Cari Eşleşmeleri', roles: ['Admin', 'Manager', 'Accounting'] }
                },
                {
                    path: 'users',
                    name: 'UserManagement',
                    component: () => import('../views/UserManagementView.vue'),
                    meta: { title: 'Kullanıcı Yönetimi', roles: ['Admin'] }
                },
                {
                    path: 'reports/zone-material',
                    redirect: () => {
                        return { path: '/reports', query: { tab: 'zone-material' } };
                    }
                },
                {
                    path: 'reports',
                    name: 'ReportsDashboard',
                    component: () => import('../views/ReportsDashboardView.vue'),
                    meta: { title: 'Raporlar', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'reports/shipment-comparison',
                    name: 'ShipmentComparisonReport',
                    component: () => import('../views/ShipmentComparisonReportView.vue'),
                    meta: { title: 'Sevkiyat Karşılaştırma', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'cargo-tracking',
                    name: 'CargoTracking',
                    component: () => import('../views/CargoTrackingView.vue'),
                    meta: { title: 'Kargo Takip', roles: ['Admin', 'Manager', 'Accounting', 'Dispatcher'] }
                },
                {
                    path: 'admin/driver-sessions',
                    name: 'DriverSessionsAdmin',
                    component: () => import('../views/DriverSessionsView.vue'),
                    meta: { title: 'Şoför Puantajı', roles: ['Admin', 'Manager', 'Accounting'] }
                },
                {
                    path: 'admin/active-operations',
                    name: 'ActiveOperations',
                    component: () => import('../views/ActiveOperationsView.vue'),
                    meta: { title: 'Aktif Operasyonlar', roles: ['Admin', 'Manager', 'Accounting'] }
                },
                {
                    path: 'floating-returns',
                    name: 'FloatingReturns',
                    component: () => import('../views/FloatingReturnsView.vue'),
                    meta: { title: 'Belirsiz İadeler', roles: ['Admin', 'Manager', 'Warehouse', 'Driver'] }
                },
                {
                    path: 'vehicle-returns',
                    name: 'VehicleReturns',
                    component: () => import('../views/VehicleReturnsView.vue'),
                    meta: { title: 'Araç İade Takibi', roles: ['Admin', 'Manager', 'Accounting', 'Warehouse'] }
                },
                {
                    path: 'stock-counts',
                    name: 'StockCounts',
                    component: () => import('../views/StockCountView.vue'),
                    meta: { title: 'Stok Sayımı', roles: ['Admin', 'Manager', 'Warehouse'] }
                },
                {
                    path: 'stock-consumptions',
                    name: 'StockConsumptions',
                    component: () => import('../views/StockConsumptionView.vue'),
                    meta: { title: 'Stok Tüketim / Zai', roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] }
                },
                {
                    path: 'warehouse/locations',
                    name: 'WarehouseLocations',
                    component: () => import('../views/WarehouseLocationsView.vue'),
                    meta: { title: 'Depo Adres Yönetimi', roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] }
                },
                {
                    path: 'warehouse/map',
                    name: 'WarehouseMap',
                    component: () => import('../views/WarehouseMapView.vue'),
                    meta: { title: 'Depo Haritası', roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] }
                },
                {
                    path: 'warehouse/stock-locations',
                    name: 'StockLocations',
                    component: () => import('../views/StockLocationsView.vue'),
                    meta: { title: 'Stok Lokasyon Haritası', roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] }
                },
                {
                    path: 'reconciliation',
                    name: 'Reconciliation',
                    component: () => import('../views/ReconciliationView.vue'),
                    meta: { title: 'Mutabakat Kontrolleri', roles: ['Admin', 'Manager'] }
                },
                {
                    path: 'route-optimization',
                    name: 'RouteOptimization',
                    component: () => import('../views/RouteOptimizationView.vue'),
                    meta: { title: 'Rota Optimizasyonu', roles: ['Admin', 'Manager', 'Driver'] }
                },
                {
                    path: 'netsis/reconciliation',
                    name: 'NetsisReconciliation',
                    component: () => import('../views/NetsisReconciliationView.vue'),
                    meta: { title: 'Netsis Uzlaştırma', roles: ['Admin', 'Manager', 'Accounting'] }
                },
                {
                    path: 'settings/depot',
                    redirect: '/settings',
                },
                {
                    path: 'projects/contacts',
                    name: 'ProjectContacts',
                    component: () => import('../views/ProjectContactsView.vue'),
                    meta: { title: 'Proje İletişim Bilgileri', roles: ['Admin', 'Manager', 'Accounting'] }
                },
                {
                    path: 'external-email-contacts',
                    name: 'ExternalEmailContacts',
                    component: () => import('../views/ExternalEmailContactsView.vue'),
                    meta: { requiresAuth: true, roles: ['Admin', 'Manager', 'Accounting'] }
                },
            ]
        },
        {
            path: '/driver',
            component: () => import('../layouts/DriverLayout.vue'),
            meta: { requiresAuth: true, roles: ['Admin', 'Manager', 'Driver', 'Driver'] },
            children: [
                {
                    path: '',
                    name: 'DriverShipments',
                    component: () => import('../views/DriverShipmentListView.vue'),
                    meta: { title: 'Sevkiyatlarım' }
                },
                {
                    path: 'stop/:projectId',
                    name: 'DriverStop',
                    component: () => import('../views/DriverStopView.vue'),
                    meta: { title: 'Teslimat Noktası' }
                },
                {
                    path: 'settings',
                    name: 'DriverSettings',
                    component: () => import('../views/DriverSettingsView.vue'),
                    meta: { title: 'İzinler & Ayarlar' }
                },
                // İrsaliye-bazı teslim (DriverDelivery) ve iade route'ları kaldırıldı —
                // teslim ve iade artık durak (proje) bazlı DriverStopView içinde yapılıyor.
            ]
        },
        {
            path: '/driver/qr-scan',
            name: 'DriverQrScan',
            component: () => import('../views/DriverQrScanView.vue'),
            meta: { requiresAuth: true, roles: ['Driver'], title: 'QR Sefer' }
        },
        {
            path: '/shipments/:id/print',
            name: 'ShipmentOrderPrint',
            component: () => import('../views/ShipmentOrderPrintView.vue'),
            meta: { requiresAuth: true, title: 'Sipariş Formu' }
        },
        {
            path: '/shipments/:id/cargo-label',
            name: 'CargoLabel',
            component: () => import('../views/CargoLabelPrintView.vue'),
            meta: { requiresAuth: true, roles: ['Admin', 'Manager', 'Accounting', 'Dispatcher', 'Warehouse'], title: 'Kargo Etiketi' }
        },
        {
            path: '/print/container-labels',
            name: 'ContainerLabelPrint',
            component: () => import('../views/ContainerLabelPrintView.vue'),
            meta: { requiresAuth: true, roles: ['Admin', 'Manager', 'Warehouse'], title: 'Araba QR Etiketleri' }
        },
    ],
});

router.beforeEach((to, _from, next) => {
    const authStore = useAuthStore();
    const notificationStore = useNotificationStore();

    if (to.meta.requiresAuth && !authStore.isAuthenticated) {
        next({ name: 'Login' });
        return;
    }

    // Redirect Driver to /driver if they try to access root dashboard
    if (to.path === '/' && authStore.userRole === 'Driver') {
        next({ path: '/driver' });
        return;
    }

    // Role-based Access Control (RBAC)
    if (to.meta.roles) {
        const allowedRoles = to.meta.roles as string[];
        const userRole = authStore.userRole;

        if (!allowedRoles.includes(userRole)) {
            notificationStore.add('Bu sayfaya erişim yetkiniz yok.', 'warning');
            next({ name: 'Dashboard' });
            return;
        }
    }

    next();
});

router.afterEach((to) => {
    const title = to.meta.title as string | undefined;
    document.title = title ? `${title} — Akyıldız Sevkiyat` : 'Akyıldız Sevkiyat';
});

export default router;
