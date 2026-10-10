/** Совпадают с ограничениями `UserCrudValidators` области Security на бекенде. */
export const passwordMinLength = 8;
export const passwordMaxLength = 128;

export interface AccountProfile {
  id: string;
  login: string;
  name: string;
}

export interface AccountLoginRequest {
  login: string;
  password: string;
  rememberMe: boolean;
}

/** Refresh-токена в теле нет: он приходит в HttpOnly-cookie. */
export interface AccountTokenResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: AccountProfile;
}

export interface AccountProfileUpdateRequest {
  name: string;
}
