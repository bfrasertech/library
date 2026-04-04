import { SiteShell } from "@/components/site-shell";

export default function ChatPage() {
  return (
    <SiteShell
      eyebrow="Route Placeholder"
      title="Chat"
      description="This page keeps the navigation and app structure in place without adding conversation logic yet."
    >
      <div className="rounded-[22px] border border-dashed border-[var(--border)] p-5 text-sm leading-7 text-[var(--muted)]">
        Chat orchestration, prompt handling, and response rendering are intentionally out of scope for this scaffold.
      </div>
    </SiteShell>
  );
}
