import { MessageAttachmentModel } from '../messages/message.models';

export interface ChatMessageNotify {
  chatId: string;
  id: string;
  text: string;
  createdAt: string;
  senderUserId: string;
  senderName: string;
  attachments?: MessageAttachmentModel[];
}

export interface ChatMessageDeletedNotify {
  chatId: string;
  id: string;
}

export interface ChatParticipantNotify {
  chatId: string;
  id: string;
  userId: string;
  login: string;
  name: string;
  isAdmin: boolean;
}

export interface ChatParticipantLeftNotify {
  chatId: string;
  id: string;
  userId: string;
}

export interface ChatListNotify {
  chatId: string;
  roomId: string;
  targetUserId: string;
  unreadCount: number;
  lastMessageText?: string | null;
  lastMessageAt?: string | null;
  lastMessageSenderName?: string | null;
  lastMessageSenderUserId?: string | null;
  lastMessageHasAttachments?: boolean;
  lastMessageCleared?: boolean;
}

export interface UserChangedNotify {
  userId: string;
  name: string;
}

export type RoomChangedKind = 'created' | 'updated' | 'deleted';

export interface RoomChangedNotify {
  kind: RoomChangedKind;
  roomId: string;
  name?: string | null;
}

export type ChatChangedKind = 'created' | 'updated' | 'deleted';

export interface ChatChangedNotify {
  kind: ChatChangedKind;
  roomId: string;
  chatId: string;
  name?: string | null;
}

export interface ChatTypingStartedNotify {
  chatId: string;
  userId: string;
  name: string;
}

export interface ChatTypingStoppedNotify {
  chatId: string;
  userId: string;
}

export type JoinRequestChangedKind = 'created' | 'accepted' | 'rejected';

export interface JoinRequestChangedNotify {
  kind: JoinRequestChangedKind;
  chatId: string;
  joinRequestId: string;
  userId: string;
  pendingCount: number;
}
