import { redirect } from "next/navigation";
import QRCode from "qrcode";
import { KeyRound, Phone, ShieldCheck, ShieldOff, UserRound } from "lucide-react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { resolveSession } from "@/modules/identity/user-profile/api/resolve-session";
import { UserStatusBadge } from "@/modules/identity/user-profile/components/user-status-badge";
import { SessionInfoCard } from "@/modules/identity/user-profile/components/session-info-card";
import { SessionTokensCard } from "@/modules/identity/user-profile/components/session-tokens-card";
import { getDisplayName } from "@/lib/shared/user-display";
import { isSuperAdminUser } from "@/lib/server/authorization";

function InfoRow({
  icon: Icon,
  label,
  value,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-2xl border border-border bg-muted/30 p-4">
      <div className="flex items-center gap-3">
        <Icon className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
        <span className="text-sm font-medium text-muted-foreground">{label}</span>
      </div>
      {typeof value === "string" ? (
        <span className="text-sm font-semibold text-foreground">{value}</span>
      ) : (
        value
      )}
    </div>
  );
}

export async function UserProfilePage() {
  const session = await resolveSession();

  if (!session) {
    redirect("/login");
  }

  const {
    profile,
    claims,
    roles,
    permissions,
    expiresAt,
    sessionExpiresAt,
    accessToken,
    refreshToken,
  } = session;

  if (!profile) {
    return (
      <Alert variant="destructive">
        <AlertTitle>Unable to load profile</AlertTitle>
        <AlertDescription>Please try again.</AlertDescription>
      </Alert>
    );
  }

  const qrCode = await QRCode.toDataURL(profile.id, { width: 200, margin: 1 });
  const displayName = getDisplayName(profile) || "Authenticated user";
  const isSuperAdmin = isSuperAdminUser(profile.userName);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">My profile</h1>
        <p className="text-sm text-muted-foreground">
          Account details for the currently signed-in user.
        </p>
      </div>

      <div className="flex flex-col items-center gap-3 text-center">
        <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-primary text-primary-foreground">
          <UserRound className="h-7 w-7" aria-hidden="true" />
        </div>
        <div>
          <p className="text-lg font-semibold text-foreground">{displayName}</p>
          <p className="text-sm text-muted-foreground">@{profile.userName}</p>
          {profile.email && (
            <p className="text-sm text-muted-foreground">{profile.email}</p>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Profile</CardTitle>
          <CardDescription>Profile information from Identity.Api.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-6 md:grid-cols-2">
          <div className="flex flex-col gap-3">
            <InfoRow icon={Phone} label="Phone Number" value={profile.phoneNumber ?? "—"} />
            <InfoRow
              icon={ShieldCheck}
              label="Status"
              value={<UserStatusBadge status={profile.status} />}
            />
            <InfoRow
              icon={KeyRound}
              label="Auth Provider"
              value={profile.authProvider ?? "—"}
            />
            {profile.isDeleted && (
              <Alert variant="destructive">
                <ShieldOff />
                <AlertTitle>Account deactivated</AlertTitle>
              </Alert>
            )}
          </div>

          <div className="flex flex-col items-center gap-3">
            {/* eslint-disable-next-line @next/next/no-img-element -- static data: URI, no next/image optimization needed */}
            <img
              src={qrCode}
              alt={`QR code for user ID ${profile.id}`}
              width={200}
              height={200}
              className="rounded-lg border border-border p-2"
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Roles, Claims & Permissions</CardTitle>
          <CardDescription>Normalized authorization data from the access token.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-6 lg:grid-cols-2">
          <div className="overflow-hidden rounded-2xl border border-border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-40">Claim</TableHead>
                  <TableHead>Value</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {claims.length > 0 ? (
                  claims.map((claim) => (
                    <TableRow key={`${claim.type}:${claim.value}`}>
                      <TableCell className="align-top font-semibold text-foreground">
                        {claim.type}
                      </TableCell>
                      <TableCell className="align-top font-mono text-xs whitespace-pre-wrap break-words">
                        {claim.value}
                      </TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={2} className="text-center text-muted-foreground">
                      No claims.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>

          <div className="flex flex-col gap-4">
            <div className="rounded-2xl border border-border bg-muted/30 p-4">
              <span className="text-sm font-semibold text-foreground">
                {roles.length} roles
              </span>
              {roles.length > 0 && (
                <div className="mt-3 flex flex-wrap gap-1.5">
                  {roles.map((role) => (
                    <Badge key={role}>{role}</Badge>
                  ))}
                </div>
              )}
            </div>

            <div className="rounded-2xl border border-border bg-muted/30 p-4">
              <span className="text-sm font-semibold text-foreground">
                {claims.length} claims available
              </span>
            </div>

            {permissions.length > 0 && (
              <div className="rounded-2xl border border-border bg-muted/30 p-4">
                <span className="text-sm font-semibold text-foreground">
                  {permissions.length} permissions
                </span>
                <div className="mt-3 flex flex-wrap gap-1.5">
                  {permissions.map((permission) => (
                    <Badge key={permission} variant="outline">
                      {permission}
                    </Badge>
                  ))}
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      <SessionInfoCard
        expiresAt={expiresAt}
        sessionExpiresAt={sessionExpiresAt}
        canManuallyRefresh={isSuperAdmin}
      />
      {isSuperAdmin && (
        <SessionTokensCard accessToken={accessToken} refreshToken={refreshToken} />
      )}
    </div>
  );
}
