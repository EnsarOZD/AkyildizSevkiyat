let audioCtx: AudioContext | null = null;

function getCtx(): AudioContext {
    if (!audioCtx) audioCtx = new AudioContext();
    if (audioCtx.state === 'suspended') audioCtx.resume();
    return audioCtx;
}

function beep(frequency: number, durationMs: number, type: OscillatorType = 'sine', volume = 0.4) {
    try {
        const ctx = getCtx();
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.type = type;
        osc.frequency.value = frequency;
        gain.gain.setValueAtTime(volume, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + durationMs / 1000);
        osc.start(ctx.currentTime);
        osc.stop(ctx.currentTime + durationMs / 1000);
    } catch { /* AudioContext not available */ }
}

function vibrate(pattern: number | number[]) {
    navigator.vibrate?.(pattern);
}

export function useSoundFeedback() {
    /** Ürün başarıyla toplandı — kısa yüksek bip + kısa titreşim */
    function success() {
        beep(880, 120);
        vibrate(60);
    }

    /** Hata / eşleşme yok — çift alçak bip + uzun titreşim */
    function error() {
        beep(220, 160, 'square', 0.3);
        setTimeout(() => beep(220, 160, 'square', 0.3), 220);
        vibrate([120, 60, 120]);
    }

    /** Tüm kalemler tamamlandı — yükselen melodi + uzun titreşim */
    function complete() {
        beep(523, 110); // C5
        setTimeout(() => beep(659, 110), 130); // E5
        setTimeout(() => beep(784, 220), 260); // G5
        vibrate([60, 40, 60, 40, 200]);
    }

    /** Yeni sevkiyat atandı — iki tonlu uyarı + orta titreşim */
    function newAssignment() {
        beep(440, 180);
        setTimeout(() => beep(550, 180), 220);
        vibrate([100, 60, 100]);
    }

    return { success, error, complete, newAssignment };
}
