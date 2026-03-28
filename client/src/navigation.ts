import type { Component } from 'vue';
import {
  HomeIcon,
  ClipboardDocumentListIcon,
  BuildingStorefrontIcon,
  TruckIcon,
  ArrowPathIcon,
  ArchiveBoxIcon,
  CalculatorIcon,
  MapIcon,
  MapPinIcon,
  ArrowsRightLeftIcon,
  PuzzlePieceIcon,
  ShoppingCartIcon,
  InboxArrowDownIcon,
  BuildingOfficeIcon,
  ChartBarIcon,
  TableCellsIcon,
  UsersIcon,
  SignalIcon,
  DevicePhoneMobileIcon,
  ListBulletIcon,
  ShieldExclamationIcon,
  ArrowTrendingUpIcon,
} from '@heroicons/vue/24/outline';

export type UserRole = 'Admin' | 'Warehouse' | 'Accounting' | 'Dispatcher' | 'Manager';

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

export const NAV_ITEMS: NavGroup[] = [
    {
        items: [
            { label: 'Dashboard', to: '/', icon: HomeIcon },
        ],
    },
    {
        title: 'Sipariş & Sevkiyat',
        items: [
            { label: 'ISS Entegrasyon', to: '/orders/import', icon: SignalIcon,                 roles: ['Admin', 'Accounting', 'Manager'], badge: 'Beta' },
            { label: 'Sevkiyatlar',     to: '/shipments',      icon: ClipboardDocumentListIcon },
        ],
    },
    {
        title: 'Operasyon',
        items: [
            { label: 'Depo Hazırlık',    to: '/warehouse',        icon: BuildingStorefrontIcon, roles: ['Admin', 'Warehouse', 'Manager'] },
            { label: 'Şoför & Araç',     to: '/transport',        icon: TruckIcon,              roles: ['Admin', 'Manager', 'Dispatcher'] },
            { label: 'Şoför Paneli',     to: '/driver',           icon: DevicePhoneMobileIcon,  roles: ['Admin', 'Manager', 'Dispatcher'] },
            { label: 'Belirsiz İadeler', to: '/floating-returns', icon: ArrowPathIcon,          roles: ['Admin', 'Manager', 'Warehouse', 'Dispatcher'] },
            { label: 'Rota Optimizasyonu', to: '/route-optimization', icon: ArrowTrendingUpIcon, roles: ['Admin', 'Manager', 'Dispatcher'] },
        ],
    },
    {
        title: 'Stok',
        items: [
            { label: 'Stok Yönetimi', to: '/stocks',       icon: ArchiveBoxIcon, roles: ['Admin', 'Accounting', 'Manager'] },
            { label: 'Stok Sayımı',   to: '/stock-counts', icon: CalculatorIcon, roles: ['Admin', 'Manager', 'Warehouse'] },
        ],
    },
    {
        title: 'Depo & Yerleşim',
        items: [
            { label: 'Depo Adresleri',  to: '/warehouse/locations',       icon: MapPinIcon,          roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'], badge: 'Yeni' },
            { label: 'Stok Haritası',   to: '/warehouse/stock-locations', icon: ArrowsRightLeftIcon, roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'], badge: 'Yeni' },
            { label: 'Bölge Yönetimi', to: '/zones',                  icon: MapIcon,         roles: ['Admin', 'Manager'] },
            { label: 'Proje - Bölge',  to: '/projects/zone-mapping',  icon: PuzzlePieceIcon, roles: ['Admin', 'Manager'] },
            { label: 'Teslimat Sırası', to: '/zones/project-order',    icon: ListBulletIcon,  roles: ['Admin', 'Manager'] },
        ],
    },
    {
        title: 'Satınalma',
        items: [
            { label: 'Satınalma Siparişleri',  to: '/purchase-orders', icon: ShoppingCartIcon,   roles: ['Admin', 'Accounting', 'Manager'] },
            { label: 'Mal Kabul İrsaliyeleri', to: '/goods-receipts',  icon: InboxArrowDownIcon, roles: ['Admin', 'Warehouse', 'Manager'] },
            { label: 'Tedarikçiler',           to: '/suppliers',        icon: BuildingOfficeIcon, roles: ['Admin', 'Accounting', 'Manager'] },
        ],
    },
    {
        title: 'Raporlar',
        items: [
            { label: 'Raporlar',             to: '/reports',               icon: ChartBarIcon,  roles: ['Admin', 'Accounting', 'Warehouse', 'Manager'] },
            { label: 'Bölge Malzeme Raporu', to: '/reports/zone-material', icon: TableCellsIcon, roles: ['Admin', 'Warehouse', 'Dispatcher', 'Manager', 'Accounting'] },
        ],
    },
    {
        title: 'Sistem',
        items: [
            { label: 'Kullanıcı Yönetimi',   to: '/users',          icon: UsersIcon,             roles: ['Admin'] },
            { label: 'Mutabakat Kontrolleri', to: '/reconciliation', icon: ShieldExclamationIcon, roles: ['Admin', 'Manager'] },
        ],
    },
];
