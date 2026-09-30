import Link from "next/link";

export function Brand() {
  return (
    <Link
      href="/"
      className="flex shrink-0 items-center gap-2 font-semibold tracking-tight"
    >
      <span className="flex size-7 shrink-0 items-center justify-center rounded-lg bg-primary text-sm text-primary-foreground">
        A
      </span>
      <span className="truncate">Admin</span>
    </Link>
  );
}
