/**
 * Centralized date/time formatting for the Akyildiz Sevkiyat frontend.
 *
 * Why this exists: previously ~84 call sites across 41 helpers
 * formatted dates inline with subtle variations and at least
 * 3 latent timezone bugs (audit/AUDIT_REPORT_2026-05-19.md).
 *
 * Migration of existing call sites is being done incrementally
 * in separate commits. Do not edit those sites unless you are
 * doing the migration deliberately.
 *
 * Backend convention:
 *   - Full ISO timestamps (with time + Z) → use formatDate / formatDateTime
 *   - Date-only strings (YYYY-MM-DD)      → use formatDateOnly
 *   - Mixing the two will cause previous-day shifts in tr-TR.
 */

const LOCALE = 'tr-TR';
const EMPTY = '-';

function toDate(input: string | Date): Date {
  return input instanceof Date ? input : new Date(input);
}

/**
 * Format a full timestamp as a short date in Turkish locale.
 * Use when input is a full ISO timestamp (with time component).
 * For pure YYYY-MM-DD strings, use formatDateOnly instead
 * to avoid timezone shifts.
 *
 * Examples:
 *   formatDate("2026-05-19T14:30:00Z") → "19.05.2026"
 *   formatDate(new Date()) → "19.05.2026"
 *   formatDate(null) → "-"
 *   formatDate("not a date") → "-" (and console.warn)
 */
export function formatDate(input: string | Date | null | undefined): string {
  if (input == null || input === '') return EMPTY;
  const date = toDate(input);
  if (isNaN(date.getTime())) {
    console.warn('[dateFormat] Invalid date input:', input);
    return EMPTY;
  }
  return date.toLocaleDateString(LOCALE);
}

/**
 * Format a date in long Turkish form with short month name.
 *
 * Examples:
 *   formatDateLong("2026-05-19T14:30:00Z") → "19 May 2026"
 *   formatDateLong(null) → "-"
 */
export function formatDateLong(input: string | Date | null | undefined): string {
  if (input == null || input === '') return EMPTY;
  const date = toDate(input);
  if (isNaN(date.getTime())) {
    console.warn('[dateFormat] Invalid date input:', input);
    return EMPTY;
  }
  return date.toLocaleDateString(LOCALE, { day: '2-digit', month: 'short', year: 'numeric' });
}

/**
 * Format a full timestamp as date + time in Turkish locale.
 *
 * Examples:
 *   formatDateTime("2026-05-19T14:30:00Z") → "19.05.2026 14:30"
 *     (assuming +03 timezone)
 *   formatDateTime(null) → "-"
 */
export function formatDateTime(input: string | Date | null | undefined): string {
  if (input == null || input === '') return EMPTY;
  const date = toDate(input);
  if (isNaN(date.getTime())) {
    console.warn('[dateFormat] Invalid date input:', input);
    return EMPTY;
  }
  return date.toLocaleString(LOCALE, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/**
 * Format date + time with short month name.
 *
 * Examples:
 *   formatDateTimeLong("2026-05-19T14:30:00Z") → "19 May 2026 14:30"
 */
export function formatDateTimeLong(input: string | Date | null | undefined): string {
  if (input == null || input === '') return EMPTY;
  const date = toDate(input);
  if (isNaN(date.getTime())) {
    console.warn('[dateFormat] Invalid date input:', input);
    return EMPTY;
  }
  return date.toLocaleString(LOCALE, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/**
 * Format time only (HH:mm).
 *
 * Examples:
 *   formatTime("2026-05-19T14:30:00Z") → "14:30"  (assuming +03)
 *   formatTime(null) → "-"
 */
export function formatTime(input: string | Date | null | undefined): string {
  if (input == null || input === '') return EMPTY;
  const date = toDate(input);
  if (isNaN(date.getTime())) {
    console.warn('[dateFormat] Invalid date input:', input);
    return EMPTY;
  }
  return date.toLocaleTimeString(LOCALE, { hour: '2-digit', minute: '2-digit' });
}

/**
 * Format a YYYY-MM-DD date-only string WITHOUT timezone conversion.
 * Use this for date-only backend fields (e.g., a delivery date
 * stored as "2026-05-19" without time). Avoids the silent
 * previous-day bug in tr-TR timezone.
 *
 * Accepts: "YYYY-MM-DD" string only. If a Date object or full
 * timestamp is passed, splits at 'T' first.
 *
 * Examples:
 *   formatDateOnly("2026-05-19") → "19.05.2026"
 *   formatDateOnly("2026-05-19T00:00:00Z") → "19.05.2026"
 *     (splits at T, takes date part literally)
 *   formatDateOnly(null) → "-"
 */
export function formatDateOnly(input: string | Date | null | undefined): string {
  if (input == null || input === '') return EMPTY;

  if (input instanceof Date) {
    if (isNaN(input.getTime())) {
      console.warn('[dateFormat] Invalid date input:', input);
      return EMPTY;
    }
    const y = input.getFullYear();
    const m = String(input.getMonth() + 1).padStart(2, '0');
    const d = String(input.getDate()).padStart(2, '0');
    return `${d}.${m}.${y}`;
  }

  const datePart = input.split('T')[0] ?? input;
  const parts = datePart.split('-');
  if (parts.length !== 3 || parts.some(p => !/^\d+$/.test(p))) {
    console.warn('[dateFormat] Invalid date input:', input);
    return EMPTY;
  }
  const [y, m, d] = parts;
  return `${d}.${m}.${y}`;
}

/**
 * Format date with long Turkish weekday and full month name.
 * Used by warehouse dashboard and food preparation modal.
 * Uses T12:00:00 timezone shim for YYYY-MM-DD inputs to
 * prevent previous-day shift.
 *
 * Examples:
 *   formatWeekdayLong("2026-05-19") → "Salı 19 Mayıs"
 *   formatWeekdayLong("2026-05-19T14:30:00Z") → "Salı 19 Mayıs"
 *     (uses the date part, not the time)
 */
export function formatWeekdayLong(input: string | Date | null | undefined): string {
  if (input == null || input === '') return EMPTY;

  let date: Date;
  if (input instanceof Date) {
    date = input;
  } else if (/^\d{4}-\d{2}-\d{2}$/.test(input)) {
    date = new Date(input + 'T12:00:00');
  } else {
    date = new Date(input);
  }

  if (isNaN(date.getTime())) {
    console.warn('[dateFormat] Invalid date input:', input);
    return EMPTY;
  }
  return date.toLocaleDateString(LOCALE, { weekday: 'long', day: 'numeric', month: 'long' });
}
