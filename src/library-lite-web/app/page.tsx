import { SiteShell } from "@/components/site-shell";

export default function HomePage() {
  return (
    <SiteShell
      eyebrow="Library Lite"
      title="A compact reading room for fast iteration."
      description="This scaffold establishes the Next.js app shell, route structure, and API configuration without committing to final product flows."
    >
      <div className="grid gap-4 md:grid-cols-3">
        <article className="rounded-[22px] border border-[var(--border)] bg-white/40 p-4">
          <h2 className="text-lg">Home</h2>
          <p className="mt-2 text-sm leading-6 text-[var(--muted)]">
            Entry point for featured collections, recent activity, or onboarding content.
          </p>
        </article>
        <article className="rounded-[22px] border border-[var(--border)] bg-white/40 p-4">
          <h2 className="text-lg">Search</h2>
          <p className="mt-2 text-sm leading-6 text-[var(--muted)]">
            Reserved for future discovery UI, filters, and result previews.
          </p>
        </article>
        <article className="rounded-[22px] border border-[var(--border)] bg-white/40 p-4">
          <h2 className="text-lg">Chat</h2>
          <p className="mt-2 text-sm leading-6 text-[var(--muted)]">
            Placeholder route for conversational lookup and guided assistance.
          </p>
        </article>
      </div>
    </SiteShell>
  );
}
