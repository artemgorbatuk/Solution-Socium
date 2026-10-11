export interface MessageAttachmentModel {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

export interface MessageListModel {
  id: string;
  text: string;
  /** UTC в формате ISO 8601. */
  createdAt: string;
  senderUserId: string;
  senderName: string;
  attachments?: MessageAttachmentModel[];
}

export interface MessageListPageResponse {
  rowExists: boolean;
  rowCount: number;
  hasOlder: boolean;
  rows: MessageListModel[];
}

export interface MessageCreateRequest {
  chatId: string;
  text: string;
  fileIds?: string[];
}

export interface MessageUpdateRequest {
  id: string;
  text: string;
}

export interface MessageDeletePageResponse {
  id: string;
  chatId: string;
  text: string;
  createdAt: string;
}

export interface MessageDeleteResponse {
  isDeleted: boolean;
}
