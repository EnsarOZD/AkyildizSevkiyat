// Photos are served by the API server, not the frontend server.
// In production, frontend and API are on different subdomains.
const API_ORIGIN = (() => {
  const base = import.meta.env.VITE_API_BASE_URL as string | undefined;
  if (!base) return '';
  // Strip trailing /api or /api/ to get the origin
  return base.replace(/\/api\/?$/, '');
})();

/**
 * Build an absolute URL for a photo stored by the API.
 * Handles both relative paths (from disk storage) and inline base64 data URIs.
 */
export function getPhotoUrl(
  photoPath: string | null | undefined,
  photoBase64: string | null | undefined,
): string | null {
  if (photoPath) return `${API_ORIGIN}/photos/${photoPath}`;
  if (photoBase64) return `data:image/jpeg;base64,${photoBase64}`;
  return null;
}

/**
 * Make an already-/photos/-prefixed URL (e.g. from the API's GetUrl) absolute
 * against the API origin. Pass-through for data URIs / absolute URLs.
 */
export function absolutePhotoUrl(url: string | null | undefined): string | null {
  if (!url) return null;
  if (url.startsWith('http') || url.startsWith('data:')) return url;
  return `${API_ORIGIN}${url.startsWith('/') ? '' : '/'}${url}`;
}
