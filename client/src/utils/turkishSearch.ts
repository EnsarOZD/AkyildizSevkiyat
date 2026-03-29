export function turkishLower(str: string): string {
  return (str ?? '')
    .replace(/İ/g, 'i')
    .replace(/I/g, 'ı')
    .replace(/Ğ/g, 'ğ')
    .replace(/Ü/g, 'ü')
    .replace(/Ş/g, 'ş')
    .replace(/Ö/g, 'ö')
    .replace(/Ç/g, 'ç')
    .toLowerCase()
}

export function turkishIncludes(text: string, query: string): boolean {
  if (!query) return true
  return turkishLower(text).includes(turkishLower(query))
}
