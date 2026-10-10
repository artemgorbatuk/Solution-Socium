/** Совпадают с ограничениями `UserCrudValidators` области Security на бекенде. */
export const userLoginMaxLength = 64;
export const userNameMaxLength = 128;

/** Регистрация: создать пользователя можно без входа. */
export interface UserCreateRequest {
  login: string;
  name: string;
  password: string;
}
