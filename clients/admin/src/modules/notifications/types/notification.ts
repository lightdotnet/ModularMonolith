/** Mirrors `Notifications.Contracts/SystemNotifications/NotificationState.cs` — the backend serializes this enum by name, not by number. */
export enum NotificationStatus {
  None = "None",
  Read = "Read",
  Archived = "Archived",
}

export interface NotificationDto {
  id: string;
  toUserId: string;
  title: string;
  message?: string | null;
  url?: string | null;
  senderUserId?: string | null;
  senderName?: string | null;
  status: NotificationStatus;
  created: string;
}

export interface NotificationLookupParams {
  toUserId?: string;
  status?: NotificationStatus;
  pageNumber?: number;
  pageSize?: number;
}

export interface SendNotificationRequest {
  toUserId: string;
  title: string;
  message?: string;
  url?: string;
}
