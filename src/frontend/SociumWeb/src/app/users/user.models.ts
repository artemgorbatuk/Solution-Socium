/** Совпадают с ограничениями `UserCrudValidators` на бекенде. */
export const userLoginMaxLength = 64;
export const userNameMaxLength = 128;

export interface UserListModel {
  id: string;
  login: string;
  name: string;
}

export interface UserListPageResponse {
  rowExists: boolean;
  rowCount: number;
  rows: UserListModel[];
}

export interface UserCreateRequest {
  login: string;
  name: string;
}
