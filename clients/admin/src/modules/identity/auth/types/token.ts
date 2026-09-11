export interface GetTokenRequest {
  username: string;
  password: string;
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}

export interface ExchangeExternalLoginCodeRequest {
  code: string;
  codeVerifier: string;
}

export interface DeviceDto {
  id?: string | null;
  name?: string | null;
  ipAddress?: string | null;
}

export interface TokenDto {
  accessToken: string;
  expiresIn: number;
  refreshToken: string | null;
}
