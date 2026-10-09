/** Совпадает с `HasMaxLength(128)` у `Room.Name` на бекенде. */
export const roomNameMaxLength = 128;

export interface RoomListModel {
  id: string;
  name: string;
}

export interface RoomListPageResponse {
  rowExists: boolean;
  rowCount: number;
  rows: RoomListModel[];
}

export interface RoomCreateRequest {
  name: string;
}

export interface RoomUpdateRequest {
  id: string;
  name: string;
}

export interface RoomDeletePageResponse {
  id: string;
  name: string;
  chatCount: number;
}

export interface RoomDeleteResponse {
  isDeleted: boolean;
}
