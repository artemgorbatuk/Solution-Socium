/** Совпадают с ограничениями `UserCrudValidators` области Security на бекенде. */
export const userLoginMaxLength = 64;
export const userNameMaxLength = 128;
export const userEmailMaxLength = 256;

/** Регистрация: создать пользователя можно без входа; вход после подтверждения email. */
export interface UserCreateRequest {
  email: string;
  login: string;
  name: string;
  password: string;
}

export interface UserSearchModel {
  id: string;
  login: string;
  name: string;
}

export interface UserSearchResponse {
  rows: UserSearchModel[];
}

/** Строка списка `/users` (`UserRead`); `canGrant*` / `canRevoke*` — подсказка для переключателя, сервер проверяет сам. */
export interface UserAdminListModel {
  id: string;
  login: string;
  name: string;
  email: string;
  isEmailConfirmed: boolean;
  isDeleted: boolean;
  isAppAdmin: boolean;
  canGrantAppAdmin: boolean;
  canRevokeAppAdmin: boolean;
}

export interface UserAdminListPageResponse {
  rowExists: boolean;
  rowCount: number;
  rows: UserAdminListModel[];
}
