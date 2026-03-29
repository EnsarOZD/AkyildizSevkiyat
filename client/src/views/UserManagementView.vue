<template>
  <div class="space-y-6">
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Kullanıcı Yönetimi</h1>
      <button @click="openCreateModal" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
        + Yeni Kullanıcı Ekle
      </button>
    </div>

    <!-- Kullanıcı Tablosu -->
    <div class="bg-white dark:bg-gray-900 shadow overflow-hidden rounded-lg">
      <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Ad Soyad</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">E-posta</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Rol</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-if="loading">
            <td colspan="5" class="px-6 py-10 text-center text-gray-500 dark:text-gray-400">Yükleniyor...</td>
          </tr>
          <tr v-else-if="users.length === 0">
            <td colspan="5" class="px-6 py-10 text-center text-gray-500 dark:text-gray-400">Kullanıcı bulunamadı.</td>
          </tr>
          <tr v-for="user in users" :key="user.id" :class="!user.isActive ? 'bg-gray-50 dark:bg-gray-800 opacity-60' : ''">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
              {{ user.firstName }} {{ user.lastName }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ user.email }}</td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full" :class="roleBadgeClass(user.role)">
                {{ roleLabel(user.role) }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span
                class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full"
                :class="user.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'"
              >
                {{ user.isActive ? 'Aktif' : 'Pasif' }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-3">
              <button @click="openEditModal(user)" class="text-indigo-600 hover:text-indigo-900">Düzenle</button>
              <button @click="openResetPasswordModal(user)" class="text-yellow-600 hover:text-yellow-900">Şifre Sıfırla</button>
              <button
                @click="toggleActive(user)"
                :class="user.isActive ? 'text-red-600 hover:text-red-900' : 'text-green-600 hover:text-green-900'"
              >
                {{ user.isActive ? 'Pasife Al' : 'Aktif Et' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
      </div>
    </div>

    <!-- Kullanıcı Oluştur / Düzenle Modal -->
    <div v-if="showUserModal" class="fixed inset-0 bg-gray-500 bg-opacity-75 flex items-center justify-center z-50">
      <div class="bg-white dark:bg-gray-900 rounded-lg p-6 max-w-md w-full">
        <h3 class="text-lg font-medium mb-4 dark:text-gray-100">{{ editingUser ? 'Kullanıcı Düzenle' : 'Yeni Kullanıcı Ekle' }}</h3>
        <div class="space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Ad</label>
              <input v-model="form.firstName" type="text" class="mt-1 block w-full border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:text-gray-100" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Soyad</label>
              <input v-model="form.lastName" type="text" class="mt-1 block w-full border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:text-gray-100" />
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">E-posta</label>
            <input v-model="form.email" type="email" class="mt-1 block w-full border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:text-gray-100" />
          </div>
          <div v-if="!editingUser">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Şifre</label>
            <input
              v-model="form.password"
              type="password"
              class="mt-1 block w-full border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:text-gray-100"
              :class="{ 'border-red-500': formErrors.password }"
            />
            <p v-if="formErrors.password" class="mt-1 text-xs text-red-600">{{ formErrors.password }}</p>
          </div>
          <div v-if="!editingUser">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Şifre Tekrar</label>
            <input
              v-model="form.confirmPassword"
              type="password"
              class="mt-1 block w-full border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:text-gray-100"
              :class="{ 'border-red-500': formErrors.confirmPassword }"
            />
            <p v-if="formErrors.confirmPassword" class="mt-1 text-xs text-red-600">{{ formErrors.confirmPassword }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Rol</label>
            <select v-model="form.role" class="mt-1 block w-full border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:text-gray-100">
              <option v-for="r in roles" :key="r.value" :value="r.value">{{ r.label }}</option>
            </select>
          </div>
        </div>
        <div class="mt-5 flex justify-end gap-2">
          <button @click="showUserModal = false" class="px-4 py-2 border rounded text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800">İptal</button>
          <button @click="saveUser" :disabled="saving" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
            {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Şifre Sıfırla Modal -->
    <div v-if="showResetModal" class="fixed inset-0 bg-gray-500 bg-opacity-75 flex items-center justify-center z-50">
      <div class="bg-white dark:bg-gray-900 rounded-lg p-6 max-w-sm w-full">
        <h3 class="text-lg font-medium mb-1 dark:text-gray-100">Şifre Sıfırla</h3>
        <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">
          {{ resetTarget?.firstName }} {{ resetTarget?.lastName }} için yeni şifre belirleyin.
        </p>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Yeni Şifre</label>
          <input v-model="newPassword" type="password" class="mt-1 block w-full border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div class="mt-5 flex justify-end gap-2">
          <button @click="showResetModal = false" class="px-4 py-2 border rounded text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800">İptal</button>
          <button @click="resetPassword" :disabled="saving" class="px-4 py-2 bg-yellow-600 text-white rounded hover:bg-yellow-700 disabled:opacity-50">
            {{ saving ? 'Sıfırlanıyor...' : 'Sıfırla' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import userService, { type UserListItem } from '../services/userService';
import { useNotificationStore } from '../stores/notification';

const notificationStore = useNotificationStore();

const users = ref<UserListItem[]>([]);
const loading = ref(false);
const saving = ref(false);

const showUserModal = ref(false);
const editingUser = ref<UserListItem | null>(null);
const form = ref({ firstName: '', lastName: '', email: '', password: '', confirmPassword: '', role: 0 });
const formErrors = ref({ password: '', confirmPassword: '' });

const showResetModal = ref(false);
const resetTarget = ref<UserListItem | null>(null);
const newPassword = ref('');

const roles = [
  { value: 0, label: 'Admin' },
  { value: 1, label: 'Muhasebe (Accounting)' },
  { value: 2, label: 'Depo (Warehouse)' },
  { value: 3, label: 'Dağıtıcı (Dispatcher)' },
  { value: 4, label: 'Yönetici (Manager)' },
  { value: 5, label: 'Şoför (Driver)' },
];

const roleLabel = (role: string) => {
  const map: Record<string, string> = {
    Admin: 'Admin',
    Accounting: 'Muhasebe',
    Warehouse: 'Depo',
    Dispatcher: 'Dağıtıcı',
    Manager: 'Yönetici',
    Driver: 'Şoför',
  };
  return map[role] || role;
};

const roleBadgeClass = (role: string) => {
  const map: Record<string, string> = {
    Admin: 'bg-purple-100 text-purple-800',
    Accounting: 'bg-blue-100 text-blue-800',
    Warehouse: 'bg-yellow-100 text-yellow-800',
    Dispatcher: 'bg-orange-100 text-orange-800',
    Manager: 'bg-green-100 text-green-800',
    Driver: 'bg-teal-100 text-teal-800',
  };
  return map[role] || 'bg-gray-100 text-gray-800';
};

onMounted(fetchUsers);

async function fetchUsers() {
  loading.value = true;
  try {
    users.value = await userService.getAll();
  } catch {
    notificationStore.add('Kullanıcılar yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

function openCreateModal() {
  editingUser.value = null;
  form.value = { firstName: '', lastName: '', email: '', password: '', confirmPassword: '', role: 3 };
  formErrors.value = { password: '', confirmPassword: '' };
  showUserModal.value = true;
}

function openEditModal(user: UserListItem) {
  editingUser.value = user;
  const roleMap: Record<string, number> = { Admin: 0, Accounting: 1, Warehouse: 2, Dispatcher: 3, Manager: 4, Driver: 5 };
  form.value = {
    firstName: user.firstName,
    lastName: user.lastName,
    email: user.email,
    password: '',
    confirmPassword: '',
    role: roleMap[user.role] ?? 3,
  };
  formErrors.value = { password: '', confirmPassword: '' };
  showUserModal.value = true;
}

function validatePasswordForm(): boolean {
  formErrors.value = { password: '', confirmPassword: '' };
  let valid = true;
  if (!form.value.password) {
    formErrors.value.password = 'Şifre zorunludur.';
    valid = false;
  } else if (form.value.password.length < 6) {
    formErrors.value.password = 'Şifre en az 6 karakter olmalıdır.';
    valid = false;
  }
  if (!form.value.confirmPassword) {
    formErrors.value.confirmPassword = 'Şifre tekrarı zorunludur.';
    valid = false;
  } else if (form.value.password !== form.value.confirmPassword) {
    formErrors.value.confirmPassword = 'Şifreler eşleşmiyor.';
    valid = false;
  }
  return valid;
}

async function saveUser() {
  if (!editingUser.value && !validatePasswordForm()) return;
  saving.value = true;
  try {
    if (editingUser.value) {
      await userService.update({
        id: editingUser.value.id,
        email: form.value.email,
        firstName: form.value.firstName,
        lastName: form.value.lastName,
        role: form.value.role,
      });
      notificationStore.add('Kullanıcı güncellendi.', 'success');
    } else {
      await userService.create({ ...form.value });
      notificationStore.add('Kullanıcı oluşturuldu.', 'success');
    }
    showUserModal.value = false;
    await fetchUsers();
  } catch (e: any) {
    notificationStore.add(e?.message || 'Kaydetme başarısız.', 'error');
  } finally {
    saving.value = false;
  }
}

async function toggleActive(user: UserListItem) {
  const action = user.isActive ? 'pasife almak' : 'aktif etmek';
  const ok = await notificationStore.promptConfirm({ title: 'Kullanıcı Durumu', message: `${user.firstName} ${user.lastName} adlı kullanıcıyı ${action} istediğinize emin misiniz?`, confirmText: 'Evet', type: user.isActive ? 'warning' : 'info' });
  if (!ok) return;
  try {
    await userService.toggleActive(user.id, !user.isActive);
    notificationStore.add('Kullanıcı durumu güncellendi.', 'success');
    await fetchUsers();
  } catch {
    notificationStore.add('İşlem başarısız.', 'error');
  }
}

function openResetPasswordModal(user: UserListItem) {
  resetTarget.value = user;
  newPassword.value = '';
  showResetModal.value = true;
}

async function resetPassword() {
  if (!resetTarget.value || !newPassword.value) return;
  if (newPassword.value.length < 6) {
    notificationStore.add('Şifre en az 6 karakter olmalı.', 'warning');
    return;
  }
  saving.value = true;
  try {
    await userService.resetPassword(resetTarget.value.id, newPassword.value);
    notificationStore.add('Şifre sıfırlandı.', 'success');
    showResetModal.value = false;
  } catch {
    notificationStore.add('Şifre sıfırlama başarısız.', 'error');
  } finally {
    saving.value = false;
  }
}
</script>
