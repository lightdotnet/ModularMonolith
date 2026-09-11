import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { UserStatusBadge } from "@/modules/identity/user-profile/components/user-status-badge";
import { getDisplayName, getInitials } from "@/lib/shared/user-display";
import type { SessionData } from "@/types/session";

interface ProfileSummaryCardProps {
  session: SessionData;
}

export function ProfileSummaryCard({ session }: ProfileSummaryCardProps) {
  const { profile, roles } = session;

  if (!profile) {
    return (
      <Alert variant="destructive">
        <AlertTitle>Unable to load profile</AlertTitle>
        <AlertDescription>Please try again.</AlertDescription>
      </Alert>
    );
  }

  return (
    <Card>
      <CardContent className="flex flex-wrap items-center gap-4">
        <Avatar size="lg">
          <AvatarFallback>{getInitials(profile)}</AvatarFallback>
        </Avatar>

        <div className="flex flex-1 flex-col gap-1">
          <div className="flex items-center gap-2">
            <span className="text-lg font-semibold text-foreground">
              {getDisplayName(profile)}
            </span>
            <UserStatusBadge status={profile.status} />
          </div>
          <span className="text-sm text-muted-foreground">@{profile.userName}</span>
          {profile.email && (
            <span className="text-sm text-muted-foreground">{profile.email}</span>
          )}
        </div>

        {roles.length > 0 && (
          <div className="flex flex-wrap gap-1.5">
            {roles.map((role) => (
              <Badge key={role} variant="outline">
                {role}
              </Badge>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
