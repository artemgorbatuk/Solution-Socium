/** Совпадают с `RoleNames` области Security на бекенде. */
export const roleNames = {
  appAdmin: 'App.Admin',
  roomAdmin: 'Room.Admin',
  chatAdmin: 'Chat.Admin',
  chatParticipant: 'Chat.Participant',
} as const;

export type RoleAssignmentRoleName = (typeof roleNames)[keyof typeof roleNames];

export interface RoleAssignmentChatModel {
  chatId: string;
  chatName: string;
  isParticipant: boolean;
  isChatAdmin: boolean;
  isLastChatAdmin: boolean;
}

export interface RoleAssignmentRoomModel {
  roomId: string;
  roomName: string;
  isRoomAdmin: boolean;
  isLastRoomAdmin: boolean;
  chats: RoleAssignmentChatModel[];
}

/** Дерево «все комнаты → чаты» с ролями пользователя; `canManage` — `UserAccessManage`, не себе, не удалённому. */
export interface RoleAssignmentListPageResponse {
  userId: string;
  canManage: boolean;
  rooms: RoleAssignmentRoomModel[];
}

/** Роль комнаты — с `roomId`, роль чата — с `chatId`. */
export interface RoleAssignmentRequest {
  userId: string;
  roleName: RoleAssignmentRoleName;
  roomId?: string;
  chatId?: string;
}
