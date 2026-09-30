import type { DeviceDto } from "@/modules/identity/auth";

export interface UserSessionDto {
  id: string;
  expiresAt: string | null;
  refreshTokenExpiresAt: string | null;
  device: DeviceDto | null;
}
