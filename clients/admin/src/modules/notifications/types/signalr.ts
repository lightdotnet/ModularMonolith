/**
 * Response of `POST auth/token/hub` on the Identity backend — a short-lived,
 * notification-hub-audience-only access token, minted fresh on every request
 * so it can be re-issued per (re)connect.
 */
export interface HubTokenDto {
  accessToken: string;
  /** Token lifetime in seconds (~120). */
  expiresIn: number;
}
