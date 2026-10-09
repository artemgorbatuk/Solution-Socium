export interface MessageListModel {
  id: string;
  text: string;
  /** UTC в формате ISO 8601. */
  createdAt: string;
}

export interface MessageListPageResponse {
  rowExists: boolean;
  rowCount: number;
  rows: MessageListModel[];
}

export interface MessageCreateRequest {
  chatId: string;
  text: string;
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
