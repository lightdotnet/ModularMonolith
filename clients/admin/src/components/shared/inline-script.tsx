/**
 * Renders a `<script>` that the browser executes synchronously while parsing the server HTML,
 * before React hydrates. On the client it renders as `type="text/plain"` so it stays inert on
 * soft navigations (where the owning Client Component re-renders anyway).
 *
 * See node_modules/next/dist/docs/01-app/02-guides/preventing-flash-before-hydration.md.
 */
export function InlineScript({ html }: { html: string }) {
  return (
    <script
      type={typeof window === "undefined" ? "text/javascript" : "text/plain"}
      suppressHydrationWarning
      dangerouslySetInnerHTML={{ __html: html }}
    />
  );
}
