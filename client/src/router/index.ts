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
                    meta: { title: 'Sevkiyatlar' }
                },
                {
                    path: 'shipments/:id',
                    name: 'ShipmentDetail',
                    component: () => import('../views/ShipmentDetailView.vue'),
                    meta: { title: 'Sevkiyat Detayı', breadcrumb: [{ label: 'Sevkiyatlar', to: '/shipments' }] }
                },
                {
                    path: 'zones',
                    name: 'ZoneList',
                    component: () => import('../views/ZoneListView.vue'),
                    meta: { title: 'Bölge Yönetimi', roles: ['Admin', 'Manager'] }
                },
                {
                    path: 'projects/zone-mapping',
                    name: 'ProjectMapping',
                    component: () => import('../views/ProjectMappingView.vue'),
                    meta: { title: 'Proje-Bölge Eşleşmesi', roles: ['Admin', 'Manager'] }
                },
                {
                    path: 'zones/project-order',
                    name: 'ZoneProjectOrder',
                    component: () => import('../views/ZoneProjectOrderView.vue'),
                    meta: { title: 'Teslimat Sırası', roles: ['Admin', 'Manager'] }
                },
                {
                    path: 'orders/import',
                    name: 'OrderImport',
                    component: () => import('../views/OrderImportView.vue'),
                    meta: { title: 'ISS Entegrasyon', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'stocks',
                    name: 'StockManagement',
                    component: () => import('../views/StockManagementView.vue'),
                    meta: { title: 'Stok Yönetimi', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'warehouse',
                    name: 'WarehouseDashboard',
                    component: () => import('../views/WarehouseDashboard.vue'),
                    meta: { title: 'Depo Hazırlık', roles: ['Admin', 'Warehouse', 'Manager'] }
                },
                {
                    path: 'transport',
                    name: 'TransportManagement',
                    component: () => import('../views/TransportManagementView.vue'),
                    meta: { title: 'Şoför & Araç Yönetimi', roles: ['Admin', 'Manager', 'Dispatcher'] }
                },
                {
                    path: 'purchase-orders',
                    name: 'PurchaseOrders',
                    component: () => import('../views/PurchaseOrdersView.vue'),
                    meta: { title: 'Satınalma Siparişleri', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'goods-receipts',
                    name: 'GoodsReceipts',
                    component: () => import('../views/GoodsReceiptsView.vue'),
                    meta: { title: 'Mal Kabul İrsaliyeleri', roles: ['Admin', 'Warehouse', 'Manager'] }
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
                    meta: { title: 'Sipariş Detayı', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'goods-receipts/:id',
                    name: 'GoodsReceiptDetail',
                    component: () => import('../views/GoodsReceiptDetailView.vue'),
                    meta: { title: 'İrsaliye Detayı', roles: ['Admin', 'Warehouse', 'Manager'] }
                },
                {
                    path: 'goods-receipts/:id/print',
                    name: 'GoodsReceiptPrint',
                    component: () => import('../views/GoodsReceiptPrintView.vue'),
                    meta: { title: 'İrsaliye Yazdır', roles: ['Admin', 'Warehouse', 'Manager'] }
                },
                {
                    path: 'suppliers',
                    name: 'SupplierManagement',
                    component: () => import('../views/SupplierManagementView.vue'),
                    meta: { title: 'Tedarikçi Yönetimi', roles: ['Admin', 'Accounting', 'Manager'] }
                },
                {
                    path: 'users',
                    name: 'UserManagement',
                    component: () => import('../views/UserManagementView.vue'),
                    meta: { title: 'Kullanıcı Yönetimi', roles: ['Admin'] }
                },
                {
                    path: 'reports/zone-material',
                    name: 'ZoneMaterialReport',
                    component: () => import('../views/ZoneMaterialReportView.vue'),
                    meta: { title: 'Bölge Malzeme Raporu', roles: ['Admin', 'Warehouse', 'Dispatcher', 'Manager', 'Accounting'], breadcrumb: [{ label: 'Raporlar', to: '/reports' }] }
                },
                {
                    path: 'reports',
                    name: 'ReportsDashboard',
                    component: () => import('../views/ReportsDashboardView.vue'),
                    meta: { title: 'Raporlar', roles: ['Admin', 'Accounting', 'Warehouse', 'Manager'] }
                },
                {
                    path: 'floating-returns',
                    name: 'FloatingReturns',
                    component: () => import('../views/FloatingReturnsView.vue'),
                    meta: { title: 'Belirsiz İadeler', roles: ['Admin', 'Manager', 'Warehouse', 'Dispatcher'] }
                },
                {
                    path: 'stock-counts',
                    name: 'StockCounts',
                    component: () => import('../views/StockCountView.vue'),
                    meta: { title: 'Stok Sayımı', roles: ['Admin', 'Manager', 'Warehouse'] }
                },
                {
                    path: 'warehouse/locations',
                    name: 'WarehouseLocations',
                    component: () => import('../views/WarehouseLocationsView.vue'),
                    meta: { title: 'Depo Adres Yönetimi', roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] }
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
                    meta: { title: 'Rota Optimizasyonu', roles: ['Admin', 'Manager', 'Dispatcher'] }
                },
                {
                    path: 'settings/depot',
                    name: 'DepotSettings',
                    component: () => import('../views/DepotSettingsView.vue'),
                    meta: { title: 'Depo Tanımları', roles: ['Admin', 'Manager'] }
                },
            ]
        },
        {
            path: '/driver',
            component: () => import('../layouts/DriverLayout.vue'),
            meta: { requiresAuth: true, roles: ['Admin', 'Manager', 'Dispatcher', 'Driver'] },
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
                    path: ':id',
                    name: 'DriverDelivery',
                    component: () => import('../views/DriverDeliveryView.vue'),
                    meta: { title: 'Teslimat' }
                },
                {
                    path: 'return/:id',
                    name: 'DriverReturn',
                    component: () => import('../views/DriverReturnView.vue'),
                    meta: { title: 'Araç İadesi' }
                },
            ]
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
