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
} from '@heroicons/vue/24/outline';

export type UserRole = 'Admin' | 'Warehouse' | 'Accounting' | 'Manager' | 'Driver';

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
            { label: 'ISS Entegrasyon', to: '/orders/import', icon: SignalIcon, roles: ['Admin', 'Accounting', 'Manager', 'Driver'] },
            { label: 'Sevkiyatlar',     to: '/shipments',      icon: ClipboardDocumentListIcon },
        ],
    },
    {
        title: 'Operasyon',
        items: [
            { label: 'Depo Hazırlık',    to: '/warehouse',        icon: BuildingStorefrontIcon, roles: ['Admin', 'Warehouse', 'Manager', 'Driver'] },
            { label: 'Belirsiz İadeler', to: '/floating-returns', icon: ArrowPathIcon,          roles: ['Admin', 'Manager', 'Warehouse', 'Driver'] },
        ],
    },
    {
        title: 'Stok',
        items: [
            { label: 'Stok Yönetimi', to: '/stocks', icon: ArchiveBoxIcon, roles: ['Admin', 'Accounting', 'Manager'] },
        ],
    },
    {
        title: 'Depo & Yerleşim',
        items: [
            { label: 'Proje - Bölge',   to: '/projects/zone-mapping', icon: PuzzlePieceIcon, roles: ['Admin', 'Manager', 'Accounting'] },
            { label: 'Teslimat Sırası', to: '/zones/project-order',   icon: ListBulletIcon,  roles: ['Admin', 'Manager', 'Accounting'] },
        ],
    },
    {
        title: 'Satınalma',
        items: [
            { label: 'Satınalma Siparişleri', to: '/purchase-orders',        icon: ShoppingCartIcon,   roles: ['Admin', 'Accounting', 'Manager'] },
            { label: 'Mal Kabul Merkezi',     to: '/goods-receipts/intake',  icon: InboxArrowDownIcon, roles: ['Admin', 'Warehouse', 'Manager', 'Accounting'] },
            { label: 'İrsaliye Geçmişi',      to: '/goods-receipts/list',    icon: ListBulletIcon,     roles: ['Admin', 'Warehouse', 'Manager', 'Accounting'] },
            { label: 'Tedarikçiler',          to: '/suppliers',              icon: BuildingOfficeIcon, roles: ['Admin', 'Accounting', 'Manager'] },
        ],
    },
    {
        title: 'Raporlar',
        items: [
            { label: 'Raporlar', to: '/reports', icon: ChartBarIcon, roles: ['Admin', 'Accounting', 'Warehouse', 'Manager'] },
        ],
    },
    {
        title: 'Sistem',
        items: [
            { label: 'Tanımlamalar',          to: '/settings',              icon: Cog6ToothIcon,         roles: ['Admin', 'Manager', 'Accounting'] },
            { label: 'Kullanıcı Yönetimi',    to: '/users',                 icon: UsersIcon,             roles: ['Admin'] },
            { label: 'Mutabakat Kontrolleri', to: '/reconciliation',        icon: ShieldExclamationIcon, roles: ['Admin', 'Manager'] },
            { label: 'Netsis Uzlaştırma',     to: '/netsis/reconciliation', icon: DocumentCheckIcon,     roles: ['Admin', 'Manager', 'Accounting'] },
        ],
    },
    {
        title: 'Planlanan',
        items: [
            { label: 'Şoför Paneli',       to: '/driver',                     icon: DevicePhoneMobileIcon, roles: ['Admin', 'Manager', 'Driver'] },
            { label: 'Stok Sayımı',        to: '/stock-counts',               icon: CalculatorIcon,        roles: ['Admin', 'Manager', 'Warehouse'] },
            { label: 'Depo Adresleri',     to: '/warehouse/locations',        icon: MapPinIcon,            roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] },
            { label: 'Stok Haritası',      to: '/warehouse/stock-locations',  icon: ArrowsRightLeftIcon,   roles: ['Admin', 'Manager', 'Warehouse', 'Accounting'] },
            { label: 'Koordinat Doğrulama', to: '/projects/coordinates',      icon: MapPinIcon,            roles: ['Admin', 'Manager'] },
            { label: 'Rota Optimizasyonu', to: '/route-optimization',         icon: ArrowTrendingUpIcon,   roles: ['Admin', 'Manager', 'Driver'] },
        ],
    },
];
