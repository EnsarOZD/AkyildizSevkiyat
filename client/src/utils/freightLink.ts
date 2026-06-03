// Nakliyeci teslim linki yardımcıları (public yükleme sayfası + WhatsApp gönderimi)

/** Public teslim yükleme sayfası URL'i (uygulamanın kendi origin'i). */
export function uploadUrl(token: string): string {
  return `${window.location.origin}/teslim/${token}`;
}

/** wa.me linki: telefonu uluslararası formata çevirir, mesaja yükleme linkini koyar. */
export function waHref(phone: string | null | undefined, projectName: string, token: string): string {
  const text = `${projectName} teslimatı için fotoğraf yükleme linki: ${uploadUrl(token)}`;
  const digits = (phone || '').replace(/\D/g, '');
  let intl = digits;
  if (digits.startsWith('0')) intl = '90' + digits.slice(1);
  else if (digits.length === 10) intl = '90' + digits;
  return intl
    ? `https://wa.me/${intl}?text=${encodeURIComponent(text)}`
    : `https://wa.me/?text=${encodeURIComponent(text)}`;
}
