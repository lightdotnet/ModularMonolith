import { redirect } from "next/navigation";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { resolveSession } from "@/modules/identity/user-profile";
import { getMyNotificationsAction } from "@/modules/notifications/api/get-my-notifications-action";
import { NotificationInbox } from "@/modules/notifications/components/notification-inbox";
import { ProfileSummaryCard } from "@/features/home/components/profile-summary-card";

export async function HomePage() {
  const session = await resolveSession();

  if (!session) {
    redirect("/login");
  }

  const notificationsState = await getMyNotificationsAction();

  return (
    <div className="flex flex-col gap-6">
      <ProfileSummaryCard session={session} />

      <Card>
        <CardHeader>
          <CardTitle>Inbox</CardTitle>
          <CardDescription>Notifications sent to you.</CardDescription>
        </CardHeader>
        <CardContent>
          <NotificationInbox
            initialNotifications={notificationsState.data?.records ?? []}
            initialTotalPages={notificationsState.data?.totalPages ?? 1}
          />
        </CardContent>
      </Card>
    </div>
  );
}
