const units = ['Б', 'КБ', 'МБ', 'ГБ', 'ТБ'] as const;

/**
 * Человекочитаемый размер файла: 512 → «512 Б», 17564 → «17,1 КБ», 108345 → «105,8 КБ».
 * База 1024; дробная часть — через `ru-RU`.
 */
export function formatFileSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) {
    return '0 Б';
  }

  if (bytes < 1024) {
    return `${Math.round(bytes)} Б`;
  }

  let value = bytes;
  let unitIndex = 0;
  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }

  const formatted = new Intl.NumberFormat('ru-RU', {
    maximumFractionDigits: unitIndex === 0 ? 0 : 1,
    minimumFractionDigits: 0,
  }).format(value);

  return `${formatted} ${units[unitIndex]}`;
}
