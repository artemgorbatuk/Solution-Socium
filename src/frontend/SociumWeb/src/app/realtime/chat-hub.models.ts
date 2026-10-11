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
