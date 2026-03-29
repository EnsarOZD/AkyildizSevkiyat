import { createWorker } from 'tesseract.js';

export interface OcrInvoiceLineResult {
  stockName: string;
  quantity: string;
  unit: string;
}

export interface OcrInvoiceResult {
  waybillNo: string;
  waybillDate: string;
  lines: OcrInvoiceLineResult[];
  rawText?: string;
  confidence?: number;
}

/**
 * Resizes an image if it exceeds max dimension.
 * Helps prevent memory errors on mobile devices by reducing pixel count.
 */
async function resizeImage(imageFile: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      const img = new Image();
      img.onload = () => {
        const maxDim = 1800; // Sufficient for OCR, lowers memory usage
        let width = img.width;
        let height = img.height;

        if (width > maxDim || height > maxDim) {
          if (width > height) {
            height = Math.round((height * maxDim) / width);
            width = maxDim;
          } else {
            width = Math.round((width * maxDim) / height);
            height = maxDim;
          }
          
          const canvas = document.createElement('canvas');
          canvas.width = width;
          canvas.height = height;
          const ctx = canvas.getContext('2d');
          if (!ctx) return resolve(e.target?.result as string);
          
          ctx.drawImage(img, 0, 0, width, height);
          resolve(canvas.toDataURL('image/jpeg', 0.85));
        } else {
          resolve(e.target?.result as string);
        }
      };
      img.onerror = () => reject(new Error('Image load error'));
      img.src = e.target?.result as string;
    };
    reader.onerror = () => reject(new Error('File read error'));
    reader.readAsDataURL(imageFile);
  });
}

/**
 * Perform OCR on an image file and extract structured invoice data.
 */
export async function scanInvoice(
  imageFile: File,
  onProgress?: (progress: number, status: string) => void
): Promise<OcrInvoiceResult> {
  
  onProgress?.(0, 'Resim hazırlanıyor...');
  const processedImage = await resizeImage(imageFile);
  
  const worker = await createWorker('tur', 1, {
    logger: (m) => {
      if (onProgress && m.status === 'recognizing text') {
        onProgress(Math.round(m.progress * 100), 'Karakterler tanınıyor...');
      } else if (onProgress && m.status.includes('loading')) {
        onProgress(10, 'Dil paketi yükleniyor...');
      }
    }
  });

  try {
    const response = await worker.recognize(processedImage);
    if (!response || !response.data) throw new Error('OCR recognition failed');
    const { data } = response;
    const result = parseInvoiceText(data.text);
    result.rawText = data.text;
    if (data.confidence !== undefined) {
      result.confidence = data.confidence;
    }
    return result;
  } finally {
    await worker.terminate();
  }
}

function parseInvoiceText(text: string): OcrInvoiceResult {
  const lines = text.split('\n');
  const result: OcrInvoiceResult = {
    waybillNo: '',
    waybillDate: '',
    lines: []
  };

  // === WAYBILL NO ===
  const waybillPatterns = [
    /[İIi](?:RS|rs)[Aa][Ll][İIi][Yy][Ee]\s*(?:NO|No|no)[\s[:\.\-]]+([\w\-\/]+)/i,
    /[İIi](?:RS|rs)\.?\s*(?:NO|No|no)[\s[:\.\-]]+([\w\-\/]+)/i,
    /Seri[\s\-]*S[ıi]ra\s*(?:No|NO)[\s[:\.\-]]+([\w\-\/]+)/i,
    /(?:Fatura|FATURA)\s*(?:NO|No|no)[\s[:\.\-]]+([\w\-\/]+)/i,
    /(?:Belge|BELGE)\s*(?:NO|No|no)[\s[:\.\-]]+([\w\-\/]+)/i,
    /ETTN[\s[:\.\-]]+([\w\-\/]+)/i
  ];

  for (const pattern of waybillPatterns) {
    const match = text.match(pattern);
    if (match && match[1]) {
      result.waybillNo = match[1].trim();
      break;
    }
  }

  // === DATE ===
  const datePatterns = [
    /[İIi](?:RS|rs)[Aa][Ll][İIi][Yy][Ee]\s*(?:TARİHİ|TARIHI|Tarihi|tarihi)[\s[:\.\-]]+(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{2,4})/i,
    /(?:Fatura|FATURA)\s*(?:TARİHİ|TARIHI|Tarihi|tarihi)[\s[:\.\-]]+(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{2,4})/i,
    /(?:Düzenleme|DUZENLEME)\s*(?:TARİHİ|TARIHI|Tarihi|tarihi)[\s[:\.\-]]+(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{2,4})/i,
    /(?:TARİH|TARIH|Tarih)[\s[:\.\-]]+(\d{1,2}[\.\/\-]\d{1,2}[\.\/\-]\d{2,4})/i
  ];

  for (const pattern of datePatterns) {
    const match = text.match(pattern);
    if (match && match[1]) {
      const parts = match[1].split(/[\.\/\-]/);
      if (parts.length === 3) {
        let day = parts[0]!;
        let month = parts[1]!;
        let year = parts[2]!;
        if (day.length === 1) day = '0' + day;
        if (month.length === 1) month = '0' + month;
        if (year.length === 2) year = '20' + year;
        if (year.length === 4) {
          result.waybillDate = `${year}-${month}-${day}`;
          break;
        }
      }
    }
  }

  // === MATERIAL LINES ===
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.length < 5) continue;

    if (/^\s*(S[ıi]ra|No|STOK|Malzeme|AÇIKLAMA|Miktar|Tutar|KDV|Toplam|Birim|Fiyat)/i.test(trimmed)) continue;
    if (/(?:Mah\.?|Sok\.?|Cad\.?|Bulv\.?|No\s*:\s*\d+|Katl?|Daire|Gebze|Kocaeli|İstanbul|Ankara|Sakarya|İzmir|Adapazarı|Vergi|Dairesi|VKN|A\.Ş\.|San\.|Tic\.)/i.test(trimmed)) {
       continue;
    }

    const qtyMatch = trimmed.match(/(\d+[\.,]?\d*)\s*(ADET|AD|KG|LT|MT|M2|M3|TON|PKT|PAKET|KUTU|RULO|TORBA|ÇUV)?/i);

    if (qtyMatch) {
      const quantityStr = qtyMatch[1]!.replace(',', '.');
      const quantity = parseFloat(quantityStr);
      if (!isNaN(quantity) && quantity > 0) {
        let stockName = trimmed.replace(qtyMatch[0], '').trim();
        stockName = stockName.replace(/^\d+[\s\.\)]+/, '').trim();
        
        if (stockName.length > 2) {
          result.lines.push({
            stockName,
            quantity: quantity.toString(),
            unit: qtyMatch[2]?.toUpperCase() || 'ADET'
          });
        }
      }
    }
  }

  return result;
}

export default { scanInvoice };
