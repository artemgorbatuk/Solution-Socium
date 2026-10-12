export interface ParticipantListModel {
  id: string;
  userId: string;
  login: string;
  name: string;
  isAdmin: boolean;
}

export interface ParticipantListPageResponse {
  rowExists: boolean;
  rowCount: number;
  /** ChatAdminGrant — смена админ-роли. */
  canChangeAdmin: boolean;
  /** ChatParticipantGrant — добавление и Accept заявок. */
  canGrantParticipant: boolean;
  /** ChatParticipantRevoke — исключение другого участника. */
  canKick: boolean;
  rows: ParticipantListModel[];
}

export interface ParticipantCreateResponse {
  id: string;
}

export interface ParticipantUpdateRequest {
  id: string;
  isAdmin: boolean;
}

export interface ParticipantUpdateResponse {
  id: string;
  isAdmin: boolean;
}

export interface ParticipantDeletePageResponse {
  chatId: string;
  canLeave: boolean;
  /** Почему уйти нельзя; `null`, если можно. */
  reason: string | null;
}

export interface ParticipantDeleteResponse {
  isDeleted: boolean;
}
