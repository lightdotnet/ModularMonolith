import { LoginForm } from "@/modules/identity/auth/components/login-form";

export async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ redirect?: string; error?: string }>;
}) {
  const { redirect, error } = await searchParams;

  return (
    <div className="flex min-h-full flex-1 items-center justify-center p-4">
      <LoginForm redirect={redirect} error={error} />
    </div>
  );
}
