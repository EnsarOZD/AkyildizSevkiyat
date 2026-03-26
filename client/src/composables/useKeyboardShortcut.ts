import { onMounted, onUnmounted } from 'vue';

interface ShortcutOptions {
  /** Aktif olmayacağı element tipleri (input, textarea, select içindeyken tetikleme) */
  ignoreInInputs?: boolean;
  /** Ctrl/Cmd tuşu ile birlikte mi? */
  ctrl?: boolean;
}

/**
 * Belirli bir tuş kombinasyonuna global keydown dinleyicisi ekler.
 * Component unmount edildiğinde otomatik temizlenir.
 */
export function useKeyboardShortcut(
  key: string,
  handler: (e: KeyboardEvent) => void,
  options: ShortcutOptions = {},
) {
  const { ignoreInInputs = true, ctrl = false } = options;

  const onKeyDown = (e: KeyboardEvent) => {
    if (ignoreInInputs) {
      const tag = (e.target as HTMLElement)?.tagName?.toLowerCase();
      if (tag === 'input' || tag === 'textarea' || tag === 'select') return;
      if ((e.target as HTMLElement)?.isContentEditable) return;
    }

    if (ctrl && !(e.ctrlKey || e.metaKey)) return;
    if (!ctrl && (e.ctrlKey || e.metaKey || e.altKey)) return;

    if (e.key.toLowerCase() === key.toLowerCase()) {
      handler(e);
    }
  };

  onMounted(() => window.addEventListener('keydown', onKeyDown));
  onUnmounted(() => window.removeEventListener('keydown', onKeyDown));
}
