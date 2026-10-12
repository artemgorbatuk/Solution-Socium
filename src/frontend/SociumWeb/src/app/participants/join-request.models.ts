export interface JoinRequestCreateResponse {
  id: string;
}

export interface JoinRequestAcceptResponse {
  id: string;
  participantId: string;
}

export interface JoinRequestRejectResponse {
  id: string;
}

export interface JoinRequestPendingListModel {
  id: string;
  userId: string;
  login: string;
  name: string;
}

export interface JoinRequestPendingListPageResponse {
  rowExists: boolean;
  rowCount: number;
  rows: JoinRequestPendingListModel[];
}
