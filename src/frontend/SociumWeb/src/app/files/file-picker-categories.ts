/** Группа фильтра системного диалога (`showOpenFilePicker` / fallback `input.accept`). */
export interface OpenFilePickerType {
  description: string;
  /** MIME → расширения (с точкой), формат File System Access API. */
  accept: Record<string, string[]>;
}

type ShowOpenFilePickerFn = (options: {
  multiple?: boolean;
  excludeAcceptAllOption?: boolean;
  types?: OpenFilePickerType[];
}) => Promise<Array<{ getFile(): Promise<File> }>>;

const images: OpenFilePickerType = {
  description: 'Картинки',
  accept: {
    'image/jpeg': ['.jpg', '.jpeg'],
    'image/png': ['.png'],
    'image/webp': ['.webp'],
    'image/gif': ['.gif'],
  },
};

const office: OpenFilePickerType = {
  description: 'Office',
  accept: {
    'application/msword': ['.doc'],
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'],
    'application/vnd.ms-excel': ['.xls'],
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': ['.xlsx'],
    'application/vnd.ms-powerpoint': ['.ppt'],
    'application/vnd.openxmlformats-officedocument.presentationml.presentation': ['.pptx'],
    'application/vnd.oasis.opendocument.text': ['.odt'],
    'application/vnd.oasis.opendocument.spreadsheet': ['.ods'],
    'application/vnd.oasis.opendocument.presentation': ['.odp'],
    'application/rtf': ['.rtf'],
  },
};

const pdf: OpenFilePickerType = {
  description: 'PDF',
  accept: {
    'application/pdf': ['.pdf'],
  },
};

const code: OpenFilePickerType = {
  description: 'Код',
  accept: {
    'text/plain': ['.txt'],
    'text/html': ['.html', '.htm'],
    'text/css': ['.css'],
    'text/javascript': ['.js', '.mjs', '.cjs', '.jsx'],
    'application/javascript': ['.js'],
    'application/json': ['.json'],
    'text/xml': ['.xml'],
    'application/xml': ['.xml'],
    'text/markdown': ['.md'],
    'text/csv': ['.csv'],
    'text/x-csharp': ['.cs'],
    'text/x-python': ['.py'],
    'text/x-sql': ['.sql'],
    'text/yaml': ['.yml', '.yaml'],
    'application/x-yaml': ['.yml', '.yaml'],
    'text/x-sh': ['.sh'],
    'application/typescript': ['.ts', '.tsx'],
  },
};

const namedGroups = [images, office, pdf, code] as const;

function mergeAccept(groups: readonly OpenFilePickerType[]): Record<string, string[]> {
  const merged: Record<string, string[]> = {};
  for (const group of groups) {
    for (const [mime, extensions] of Object.entries(group.accept)) {
      const list = merged[mime] ?? (merged[mime] = []);
      for (const extension of extensions) {
        if (!list.includes(extension)) {
          list.push(extension);
        }
      }
    }
  }
  return merged;
}

const allAccept = mergeAccept(namedGroups);

/** Группы для выпадающего списка «Тип файла» в системном диалоге. */
export const openFilePickerTypes: OpenFilePickerType[] = [
  { description: 'Все', accept: allAccept },
  images,
  office,
  pdf,
  code,
];

/** Строка `accept` для `<input type="file">`, если `showOpenFilePicker` недоступен. */
export const fileInputAccept: string = [
  ...Object.keys(allAccept),
  ...Object.values(allAccept).flat(),
].join(',');

const allowedExtensions = new Set(Object.values(allAccept).flat());

export function fileExtension(fileName: string): string {
  const index = fileName.lastIndexOf('.');
  if (index < 0) {
    return '';
  }
  return fileName.slice(index).toLowerCase();
}

export function isAllowedAttachment(file: File): boolean {
  const extension = fileExtension(file.name);
  return extension.length > 0 && allowedExtensions.has(extension);
}

function showOpenFilePickerFn(): ShowOpenFilePickerFn | undefined {
  return (window as Window & { showOpenFilePicker?: ShowOpenFilePickerFn }).showOpenFilePicker;
}

export function supportsShowOpenFilePicker(): boolean {
  return typeof showOpenFilePickerFn() === 'function';
}

export function openAttachmentPicker(): ReturnType<ShowOpenFilePickerFn> {
  const picker = showOpenFilePickerFn();
  if (!picker) {
    return Promise.reject(new Error('showOpenFilePicker is not available'));
  }

  return picker({
    multiple: true,
    excludeAcceptAllOption: true,
    types: openFilePickerTypes,
  });
}
