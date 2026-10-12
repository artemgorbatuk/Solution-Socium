/** Совпадает с `HasMaxLength(128)` у `Chat.Name` на бекенде. */
export const chatNameMaxLength = 128;

export interface ChatListModel {
  id: string;
  name: string;
  canUpdate: boolean;
  canDelete: boolean;
  unreadCount: number;
  lastMessageText?: string | null;
  /** UTC в формате ISO 8601. */
  lastMessageAt?: string | null;
  lastMessageSenderName?: string | null;
  lastMessageSenderUserId?: string | null;
  lastMessageHasAttachments?: boolean;
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
  isParticipant: boolean;
  isAdmin: boolean;
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

/** Как на бекенде: пустой чат сверху (MaxValue), иначе по lastMessageAt, затем Id. */
export function sortChatList(rows: ChatListModel[]): ChatListModel[] {
  return [...rows].sort((left, right) => {
    const leftAt = left.lastMessageAt ? Date.parse(left.lastMessageAt) : Number.POSITIVE_INFINITY;
    const rightAt = right.lastMessageAt ? Date.parse(right.lastMessageAt) : Number.POSITIVE_INFINITY;
    if (leftAt !== rightAt) {
      return rightAt - leftAt;
    }
    return right.id.localeCompare(left.id);
  });
}
