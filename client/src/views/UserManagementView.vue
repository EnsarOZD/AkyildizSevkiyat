<template>
  <div class="space-y-6">
    <PageHeader title="Kullanıcı Yönetimi" subtitle="Sistem kullanıcılarını ve rollerini yönetin" color="blue">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
        </svg>
      </template>
      <template #actions>
        <BaseButton @click="openCreateModal" variant="primary">+ Yeni Kullanıcı Ekle</BaseButton>
      </template>
    </PageHeader>

    <!-- Kullanıcı Tablosu -->
    <div class="bg-white dark:bg-gray-900 shadow overflow-hidden rounded-lg">
      <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Ad Soyad</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden sm:table-cell">E-posta</th>
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
              <div class="text-xs text-gray-400 font-normal">{{ user.username }}</div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 hidden sm:table-cell">{{ user.email }}</td>
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
              <button @click="openEditModal(user)" class="text-blue-600 hover:text-blue-900">Düzenle</button>
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
    <BaseModal :show="showUserModal" :title="editingUser ? 'Kullanıcı Düzenle' : 'Yeni Kullanıcı Ekle'" maxWidth="md" @close="showUserModal = false">
      <div class="space-y-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <BaseInput v-model="form.firstName" label="Ad" />
          <BaseInput v-model="form.lastName" label="Soyad" />
        </div>
        <BaseInput v-model="form.email" type="email" label="E-posta" />
        <BaseInput v-model="form.username" label="Kullanıcı Adı" hint="Giriş ekranında kullanılır. Boş bırakılırsa e-posta adresi kullanılır." />
        <BaseInput v-if="!editingUser" v-model="form.password" type="password" label="Şifre" :error="formErrors.password" />
        <BaseInput v-if="!editingUser" v-model="form.confirmPassword" type="password" label="Şifre Tekrar" :error="formErrors.confirmPassword" />
        <BaseSelect v-model.number="form.role" label="Rol">
          <option v-for="r in roles" :key="r.value" :value="r.value">{{ r.label }}</option>
        </BaseSelect>
        <!-- Driver link — shown only when Driver role is selected -->
        <div v-if="form.role === 5">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Bağlı Şoför Kaydı <span class="text-gray-400 font-normal">(isteğe bağlı)</span>
          </label>
          <select
            v-model.number="form.linkedDriverId"
            class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option :value="null">— Bağlantı yok —</option>
            <option v-for="d in availableDriversForLink" :key="d.id" :value="d.id">{{ d.fullName }}</option>
          </select>
          <p class="mt-1 text-xs text-gray-400">Seçilen şoför kaydı bu kullanıcıya bağlanır ve QR sefer başlatma yapabilir.</p>
        </div>
      </div>
      <template #footer>
        <BaseButton @click="showUserModal = false" variant="secondary">İptal</BaseButton>
        <BaseButton @click="saveUser" :disabled="saving" :loading="saving" variant="primary">Kaydet</BaseButton>
      </template>
    </BaseModal>

    <!-- Şifre Sıfırla Modal -->
    <BaseModal :show="showResetModal" title="Şifre Sıfırla" maxWidth="sm" @close="showResetModal = false">
      <div class="space-y-4">
        <p class="text-sm text-gray-500 dark:text-gray-400">
          {{ resetTarget?.firstName }} {{ resetTarget?.lastName }} için yeni şifre belirleyin.
        </p>
        <BaseInput
          v-model="newPassword"
          type="password"
          label="Yeni Şifre"
          hint="En az 8 karakter, bir büyük harf, bir rakam ve bir özel karakter (!@#_...) içermelidir."
        />
      </div>
      <template #footer>
        <BaseButton @click="showResetModal = false" variant="secondary">İptal</BaseButton>
        <BaseButton @click="resetPassword" :disabled="saving" :loading="saving" variant="primary">Sıfırla</BaseButton>
      </template>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import userService, { type UserListItem } from '../services/userService';
import transportService, { type Driver } from '../services/transportService';
import { useNotificationStore } from '../stores/notification';
import BaseModal from '../components/BaseModal.vue';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/base/BaseInput.vue';
import BaseSelect from '../components/base/BaseSelect.vue';

const notificationStore = useNotificationStore();

const users = ref<UserListItem[]>([]);
const allDrivers = ref<Driver[]>([]);
const loading = ref(false);
const saving = ref(false);

const showUserModal = ref(false);
const editingUser = ref<UserListItem | null>(null);
const form = ref({ firstName: '', lastName: '', email: '', username: '', password: '', confirmPassword: '', role: 0, linkedDriverId: null as number | null });
const formErrors = ref({ password: '', confirmPassword: '' });

// Drivers available for linking: unlinked ones + the one currently linked to this user (edit mode)
const availableDriversForLink = computed(() => {
  const currentLinkedDriverId = editingUser.value
    ? allDrivers.value.find(d => d.userId === editingUser.value!.id)?.id ?? null
    : null;
  return allDrivers.value.filter(d => d.isActive && (!d.userId || d.id === currentLinkedDriverId));
});

const showResetModal = ref(false);
const resetTarget = ref<UserListItem | null>(null);
const newPassword = ref('');

const roles = [
  { value: 0, label: 'Admin' },
  { value: 1, label: 'Muhasebe (Accounting)' },
  { value: 2, label: 'Depo (Warehouse)' },
  { value: 4, label: 'Yönetici (Manager)' },
  { value: 5, label: 'Şoför (Driver)' },
];

const roleLabel = (role: string) => {
  const map: Record<string, string> = {
    Admin: 'Admin',
    Accounting: 'Muhasebe',
    Warehouse: 'Depo',
    Manager: 'Yönetici',
    Driver: 'Şoför',
  };
  return map[role] || role;
};

const roleBadgeClass = (role: string) => {
  const map: Record<string, string> = {
    Admin: 'bg-violet-100 text-violet-800',
    Accounting: 'bg-blue-100 text-blue-800',
    Warehouse: 'bg-yellow-100 text-yellow-800',
    Manager: 'bg-green-100 text-green-800',
    Driver: 'bg-blue-100 text-blue-800',
  };
  return map[role] || 'bg-gray-100 text-gray-800';
};

onMounted(async () => {
  await Promise.all([fetchUsers(), fetchDrivers()]);
});

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

async function fetchDrivers() {
  try {
    allDrivers.value = await transportService.getDrivers();
  } catch {
    // non-critical — silently ignore
  }
}

function openCreateModal() {
  editingUser.value = null;
  form.value = { firstName: '', lastName: '', email: '', username: '', password: '', confirmPassword: '', role: 5, linkedDriverId: null };
  formErrors.value = { password: '', confirmPassword: '' };
  showUserModal.value = true;
}

function openEditModal(user: UserListItem) {
  editingUser.value = user;
  const roleMap: Record<string, number> = { Admin: 0, Accounting: 1, Warehouse: 2, Manager: 4, Driver: 5 };
  const linkedDriver = allDrivers.value.find(d => d.userId === user.id);
  form.value = {
    firstName: user.firstName,
    lastName: user.lastName,
    email: user.email,
    username: user.username || '',
    password: '',
    confirmPassword: '',
    role: roleMap[user.role] ?? 3,
    linkedDriverId: linkedDriver?.id ?? null,
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
  } else if (form.value.password.length < 8) {
    formErrors.value.password = 'Şifre en az 8 karakter olmalıdır.';
    valid = false;
  } else if (!/[A-Z]/.test(form.value.password)) {
    formErrors.value.password = 'Şifre en az bir büyük harf içermelidir.';
    valid = false;
  } else if (!/[0-9]/.test(form.value.password)) {
    formErrors.value.password = 'Şifre en az bir rakam içermelidir.';
    valid = false;
  } else if (!/[^a-zA-Z0-9]/.test(form.value.password)) {
    formErrors.value.password = 'Şifre en az bir özel karakter içermelidir (örn. !, @, #, _).';
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
    let userId: number;
    if (editingUser.value) {
      userId = editingUser.value.id;
      await userService.update({
        id: userId,
        email: form.value.email,
        username: form.value.username || undefined,
        firstName: form.value.firstName,
        lastName: form.value.lastName,
        role: form.value.role,
      });
    } else {
      userId = await userService.create({
        email: form.value.email,
        username: form.value.username || undefined,
        firstName: form.value.firstName,
        lastName: form.value.lastName,
        password: form.value.password,
        role: form.value.role,
      });
    }

    // Handle driver linking when role is Driver (5)
    if (form.value.role === 5 && form.value.linkedDriverId) {
      const driver = allDrivers.value.find(d => d.id === form.value.linkedDriverId);
      if (driver) {
        await transportService.updateDriver({ ...driver, userId });
      }
    } else if (editingUser.value) {
      // If role changed away from Driver, unlink any previously linked driver
      const prevLinked = allDrivers.value.find(d => d.userId === userId);
      if (prevLinked && form.value.role !== 5) {
        await transportService.updateDriver({ ...prevLinked, userId: null });
      }
    }

    notificationStore.add(editingUser.value ? 'Kullanıcı güncellendi.' : 'Kullanıcı oluşturuldu.', 'success');
    showUserModal.value = false;
    await Promise.all([fetchUsers(), fetchDrivers()]);
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
  if (newPassword.value.length < 8) {
    notificationStore.add('Şifre en az 8 karakter olmalıdır.', 'warning');
    return;
  }
  if (!/[A-Z]/.test(newPassword.value)) {
    notificationStore.add('Şifre en az bir büyük harf içermelidir.', 'warning');
    return;
  }
  if (!/[0-9]/.test(newPassword.value)) {
    notificationStore.add('Şifre en az bir rakam içermelidir.', 'warning');
    return;
  }
  if (!/[^a-zA-Z0-9]/.test(newPassword.value)) {
    notificationStore.add('Şifre en az bir özel karakter içermelidir (örn. !, @, #, _).', 'warning');
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
