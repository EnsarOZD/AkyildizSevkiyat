import defaultTheme from 'tailwindcss/defaultTheme'

/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: [
    './index.html',
    './src/**/*.{vue,js,ts,jsx,tsx}',
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Plus Jakarta Sans', ...defaultTheme.fontFamily.sans],
      },
      screens: {
        // Sidebar ↔ alt navbar geçişi için özel kırılım.
        // < desktop  → mobil/tablet modu (gizli sidebar + alt navbar)
        // >= desktop → masaüstü sidebar
        // Tabletler (dikey ~800px, yatay ~1024–1194px) bu eşiğin altında kaldığından
        // her iki yönde de mobil nav kullanır; gerçek dizüstü/masaüstü sidebar görür.
        'desktop': '1280px',
      },
      colors: {
        // Marka ana paleti
        brand: {
          50:  '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',  // primary action
          600: '#2563eb',  // primary hover
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
          950: '#172554',
        },
        // Semantik renkler — form validation, toast, badge
        success: {
          50:  '#f0fdf4',
          100: '#dcfce7',
          200: '#bbf7d0',
          500: '#22c55e',
          600: '#16a34a',
          700: '#15803d',
        },
        warning: {
          50:  '#fffbeb',
          100: '#fef3c7',
          200: '#fde68a',
          500: '#f59e0b',
          600: '#d97706',
          700: '#b45309',
        },
        danger: {
          50:  '#fef2f2',
          100: '#fee2e2',
          200: '#fecaca',
          500: '#ef4444',
          600: '#dc2626',
          700: '#b91c1c',
        },
      },
      spacing: {
        'label-gap':   '0.375rem', // label–input arası (mb-1/mb-2 tutarsızlığını çözer)
        'form-gap':    '1.5rem',   // form elemanları arası
        'section-gap': '2rem',     // bölümler arası
      },
      borderRadius: {
        'input':  '0.5rem',
        'card':   '0.75rem',
        'modal':  '1rem',
        'button': '0.5rem',
      },
      boxShadow: {
        'card':  '0 1px 3px 0 rgb(0 0 0 / 0.1)',
        'modal': '0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)',
      },
    },
  },
  plugins: [],
}
