import type { Component } from 'vue';
import {
  HomeIcon,
  ClipboardDocumentListIcon,
  BuildingStorefrontIcon,
  ArrowPathIcon,
  ArchiveBoxIcon,
  CalculatorIcon,
  MapPinIcon,
  ArrowsRightLeftIcon,
  PuzzlePieceIcon,
  ShoppingCartIcon,
  InboxArrowDownIcon,
  BuildingOfficeIcon,
  ChartBarIcon,
  UsersIcon,
  SignalIcon,
  DevicePhoneMobileIcon,
  ListBulletIcon,
  ShieldExclamationIcon,
  ArrowTrendingUpIcon,
  Cog6ToothIcon,
  DocumentCheckIcon,
  TruckIcon,
  PhoneIcon,
  EnvelopeIcon,
  ArrowUturnLeftIcon,
} from '@heroicons/vue/24/outline';

export type UserRole = 'Admin' | 'Warehouse' | 'Accounting' | 'Manager' | 'Driver' | 'Dispatcher';

export interface NavItem {
    label: string;
    to: string | { name: string };
    icon?: Component;
    roles?: UserRole[];
    badge?: 'Beta' | 'Yeni';
}

export interface NavGroup {
    title?: string;
    items: NavItem[];
}

export type MobileQuickItem = Required<Pick<NavItem, 'label' | 'icon'>> & Pick<NavItem, 'to'>;

export const MOBILE_NAV_BY_ROLE: Partial<Record<string, MobileQuickItem[]>> = {
    Admin: [
        { label: 'Dashboard',   to: '/',          icon: HomeIcon },
        { label: 'Sevkiyatlar', to: '/shipments', icon: ClipboardDocumentListIcon },
        { label: 'Depo',        to: '/warehouse', icon: BuildingStorefrontIcon },
        { label: 'Raporlar',    to: '/reports',   icon: ChartBarIcon },
    ],
    Manager: [
        { label: 'Dashboard',   to: '/',          icon: HomeIcon },
        { label: 'Sevkiyatlar', to: '/shipments', icon: ClipboardDocumentListIcon },
        { label: 'Depo',        to: '/warehouse', icon: BuildingStorefrontIcon },
        { label: 'Raporlar',    to: '/reports',   icon: ChartBarIcon },
    ],
    Warehouse: [
        { label: 'Depo Hazırlık', to: '/warehouse',             icon: BuildingStorefrontIcon },
        { label: 'İadeler',       to: '/floating-returns',      icon: ArrowPathIcon },
        { label: 'Satınalma',     to: '/purchase-orders',       icon: ShoppingCartIcon },
        { label: 'Mal Kabul',     to: '/goods-receipts/intake', icon: InboxArrowDownIcon },
    ],
    Accounting: [
        { label: 'Dashboard',   to: '/',                icon: HomeIcon },
        { label: 'Sevkiyatlar', to: '/shipments',       icon: ClipboardDocumentListIcon },
        { label: 'Satınalma',   to: '/purchase-orders', icon: ShoppingCartIcon },
        { label: 'Raporlar',    to: '/reports',         icon: ChartBarIcon },
    ],
    Dispatcher: [
        { label: 'Dashboard',   to: '/',               icon: HomeIcon },
        { label: 'Sevkiyatlar', to: '/shipments',      icon: ClipboardDocumentListIcon },
        { label: 'Depo',        to: '/warehouse',      icon: BuildingStorefrontIcon },
        { label: 'Kargo',       to: '/cargo-tracking', icon: TruckIcon },
    ],
};

export const NAV_ITEMS: NavGroup[] = [
    {
        items: [
            { label: 'Dashboard', to: '/', icon: HomeIcon },
        ],
    },
    {
        title: 'Sipariş & Sevkiyat',
        items: [
            { label: 'ISS Entegrasyon', to: '/orders/import',    icon: SignalIcon,                roles: ['Admin', 'Accounting', 'Manager', 'Driver'] },
            { label: 'Müşteriler',      to: '/customers',         icon: BuildingOfficeIcon,        roles: ['Admin', 'Accounting', 'Manager'] },
            { label: 'Sevkiyatlar',     to: '/shipments',         icon: ClipboardDocumentListIcon },
            { label: 'Kargo Takip',     to: '/cargo-tracking',    icon: TruckIcon },
            { label: 'Nakliye Linkleri', to: '/freight-deliveries', icon: PhoneIcon,               roles: ['Admin', 'Manager', 'Accounting', 'Warehouse'] },
            { label: 'Nakliyeciler',     to: '/carriers',           icon: TruckIcon,               roles: ['Admin', 'Manager', 'Accounting'] },
        ],
    },
    {
        title: 'Operasyon',
        items: [
            { label: 'Depo Hazırlık',    to: '/warehouse',          icon: BuildingStorefrontIcon, roles: ['Admin', 'Accounting', 'Warehouse', 'Manager', 'Driver'] },
            { label: 'Kıyafet Hazırlık', to: '/clothing-prep',      icon: BuildingStorefrontIcon, roles: ['Admin', 'Accounting', 'Warehouse', 'Manager'] },
            { label: 'Belirsiz İadeler', to: '/floating-returns',   icon: ArrowPathIcon,          roles: ['Admin', 'Manager', 'Warehouse', 'Driver'] },
            { label: 'Araç İade Takibi', to: '/vehicle-returns',   icon: ArrowUturnLeftIcon,      roles: ['Admin', 'Manager', 'Accounting', 'Warehouse'] },
            { label: 'Şoför Paneli',     to: '/driver',             icon: DevicePhoneMobileIcon,  roles: ['Admin', 'Manager', 'Driver'] },
            { label: 'Aktif Operasyonlar', to: '/admin/active-operations', icon: SignalIcon,          roles: ['Admin', 'Manager', 'Accounting'] },
            { label: 'Rota Optimizasyonu', to: '/route-optimization', icon: ArrowTrendingUpIcon,  roles: ['Admin', 'Manager'] },
        ],
    },
    {
        title: 'Stok',
        items: [
            { label: 'Stok Yönetimi',    to: '/stocks',           icon: ArchiveBoxIcon,     roles: ['Admin', 'Accounting', 'Manager'] },
            { label: 'Stok Eşleştirme', to: '/stocks/mappings',  icon: ArrowsRightLeftIcon, roles: ['Admin', 'Accounting', 'Manager'] },
            { label: 'Stok Sayımı',      to: '/stock-counts',     icon: CalculatorIcon,     roles: ['Admin', 'Manager', 'Warehouse'] },
            { label: 'Stok Tüketim / Zai', to: '/stock-consumptions', icon: ArchiveBoxIcon, roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] },
        ],
    },
    {
        title: 'Depo & Yerleşim',
        items: [
            { label: 'Proje - Bölge',      to: '/projects/zone-mapping',      icon: PuzzlePieceIcon,    roles: ['Admin', 'Manager', 'Accounting'] },
            { label: 'Teslimat Sırası',    to: '/zones/project-order',        icon: ListBulletIcon,     roles: ['Admin', 'Manager', 'Accounting'] },
            { label: 'Depo Adresleri',     to: '/warehouse/locations',        icon: MapPinIcon,         roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] },
            { label: 'Depo Haritası',      to: '/warehouse/map',              icon: BuildingOfficeIcon, roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] },
            { label: 'Stok Haritası',      to: '/warehouse/stock-locations',  icon: ArrowsRightLeftIcon, roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] },
            { label: 'Koordinat Doğrulama', to: '/projects/coordinates',      icon: MapPinIcon,         roles: ['Admin', 'Manager'] },
            { label: 'Proje İletişim',     to: '/projects/contacts',          icon: PhoneIcon,          roles: ['Admin', 'Manager', 'Accounting'] },
        ],
    },
    {
        title: 'Satınalma',
        items: [
            { label: 'Satınalma Siparişleri', to: '/purchase-orders',        icon: ShoppingCartIcon,   roles: ['Admin', 'Accounting', 'Manager', 'Warehouse'] },
            { label: 'Mal Kabul Merkezi',     to: '/goods-receipts/intake',  icon: InboxArrowDownIcon, roles: ['Admin', 'Warehouse', 'Manager', 'Accounting'] },
            { label: 'İrsaliye Geçmişi',      to: '/goods-receipts/list',    icon: ListBulletIcon,     roles: ['Admin', 'Warehouse', 'Manager', 'Accounting'] },
            { label: 'Tedarikçiler',          to: '/suppliers',              icon: BuildingOfficeIcon, roles: ['Admin', 'Accounting', 'Manager'] },
        ],
    },
    {
        title: 'Raporlar',
        items: [
            { label: 'Raporlar', to: '/reports', icon: ChartBarIcon, roles: ['Admin', 'Accounting', 'Warehouse', 'Manager'] },
            { label: 'Sevkiyat Karşılaştırma', to: '/reports/shipment-comparison', icon: DocumentCheckIcon, roles: ['Admin', 'Accounting', 'Manager'] },
        ],
    },
    {
        title: 'Sistem',
        items: [
            { label: 'Tanımlamalar',          to: '/settings',                   icon: Cog6ToothIcon,         roles: ['Admin', 'Manager', 'Accounting'] },
            { label: 'Netsis Cari Eşleşmeleri', to: '/system/institution-cari-mappings', icon: ArrowsRightLeftIcon, roles: ['Admin', 'Manager', 'Accounting'] },
            { label: 'Harici Mail Adresleri', to: '/external-email-contacts',    icon: EnvelopeIcon,          roles: ['Admin', 'Manager', 'Accounting'] },
            { label: 'Kıyafet Vurgu Kelimeleri', to: '/clothing-keywords',       icon: Cog6ToothIcon,         roles: ['Admin', 'Manager'] },
            { label: 'Kullanıcı Yönetimi',    to: '/users',                      icon: UsersIcon,             roles: ['Admin'] },
            { label: 'Şoför Puantajı',        to: '/admin/driver-sessions',      icon: DevicePhoneMobileIcon, roles: ['Admin', 'Manager', 'Accounting'] },
            { label: 'Mutabakat Kontrolleri', to: '/reconciliation',             icon: ShieldExclamationIcon, roles: ['Admin', 'Manager'] },
            { label: 'Netsis Uzlaştırma',     to: '/netsis/reconciliation',      icon: DocumentCheckIcon,     roles: ['Admin', 'Manager', 'Accounting'] },
        ],
    },
];
