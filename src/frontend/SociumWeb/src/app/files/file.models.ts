export interface FileUploadResponse {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

export function isImageContentType(contentType: string): boolean {
  return contentType.startsWith('image/');
}
