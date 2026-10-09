/** Совпадает с `HasMaxLength(128)` у `Chat.Name` на бекенде. */
export const chatNameMaxLength = 128;

export interface ChatListModel {
  id: string;
  name: string;
}

export interface ChatListPageResponse {
  rowExists: boolean;
  rowCount: number;
  rows: ChatListModel[];
}

export interface ChatInfoPageResponse {
  id: string;
  roomId: string;
  name: string;
}

export interface ChatCreateRequest {
  roomId: string;
  name: string;
}

export interface ChatUpdateRequest {
  id: string;
  name: string;
}

export interface ChatDeletePageResponse {
  id: string;
  roomId: string;
  name: string;
}

export interface ChatDeleteResponse {
  isDeleted: boolean;
}
