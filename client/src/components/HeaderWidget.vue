<template>
  <div class="hidden lg:flex items-center gap-4 text-gray-500 dark:text-gray-400 mr-2">
    <!-- Weather -->
    <div class="flex items-center gap-1.5 bg-gray-50 dark:bg-gray-800/50 px-2 py-1 rounded-lg border border-gray-100 dark:border-gray-800" v-if="weather" :title="weather.description">
      <span class="text-[16px] leading-none">{{ weather.icon }}</span>
      <span class="text-[11px] font-bold text-gray-700 dark:text-gray-300">{{ weather.temperature }}°C</span>
    </div>
    <div v-else class="flex items-center gap-1.5 bg-gray-50 dark:bg-gray-800/50 px-2 py-1 rounded-lg border border-gray-100 dark:border-gray-800 animate-pulse">
      <span class="text-[16px] leading-none opacity-50">☁️</span>
      <span class="text-[11px] font-bold opacity-50">--°C</span>
    </div>

    <!-- Clock -->
    <div class="flex flex-col items-end justify-center h-full">
      <span class="text-sm font-black text-blue-900 dark:text-blue-100 leading-none tracking-tight" style="font-variant-numeric: tabular-nums;">
        {{ formattedTime }}
      </span>
      <span class="text-[9px] font-bold text-gray-400 uppercase tracking-widest leading-none mt-1">
        {{ formattedDate }}
      </span>
    </div>

    <div class="h-5 w-px bg-gray-200 dark:bg-gray-700 ml-1"></div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';

const formattedTime = ref('');
const formattedDate = ref('');
const weather = ref<{ temperature: number, icon: string, description: string } | null>(null);

let timer: number;

const updateTime = () => {
  const now = new Date();
  formattedTime.value = now.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  formattedDate.value = now.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' });
};

const fetchWeather = async () => {
  try {
    // Kocaeli/Gebze Koordinatları
    const res = await fetch('https://api.open-meteo.com/v1/forecast?latitude=40.8028&longitude=29.4307&current_weather=true&timezone=Europe%2FIstanbul');
    const data = await res.json();
    const cw = data.current_weather;
    
    // WMO Hava Durumu Kodları 
    let icon = '☀️';
    let desc = 'Açık';
    if (cw.weathercode === 0) { icon = cw.is_day ? '☀️' : '🌙'; desc = 'Açık'; }
    else if ([1,2,3].includes(cw.weathercode)) { icon = cw.is_day ? '⛅' : '☁️'; desc = 'Parçalı Bulutlu'; }
    else if ([45,48].includes(cw.weathercode)) { icon = '🌫️'; desc = 'Sisli'; }
    else if ([51,53,55,56,57].includes(cw.weathercode)) { icon = '🌦️'; desc = 'Çisenti'; }
    else if ([61,63,65,66,67].includes(cw.weathercode)) { icon = '🌧️'; desc = 'Yağmurlu'; }
    else if ([71,73,75,77].includes(cw.weathercode)) { icon = '❄️'; desc = 'Karlı'; }
    else if ([80,81,82,85,86].includes(cw.weathercode)) { icon = '🌧️'; desc = 'Sağanak Yağışlı'; }
    else if ([95,96,99].includes(cw.weathercode)) { icon = '⛈️'; desc = 'Fırtınalı'; }

    weather.value = {
      temperature: Math.round(cw.temperature),
      icon,
      description: desc + ' (Gebze)'
    };
  } catch (e) {
    console.error('Hava durumu alınamadı:', e);
  }
};

onMounted(() => {
  updateTime();
  timer = window.setInterval(updateTime, 1000);
  fetchWeather();
  // Saatte bir güncelle
  window.setInterval(fetchWeather, 3600000);
});

onUnmounted(() => {
  if (timer) clearInterval(timer);
});
</script>
