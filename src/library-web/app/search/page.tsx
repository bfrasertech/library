import { SiteShell } from "@/components/site-shell";

export default function SearchPage() {
  return (
    <SiteShell
      eyebrow="Route Placeholder"
      title="Search"
      description="This page reserves the search surface while the indexing and query experience are still being defined."
    >
      <div className="rounded-[22px] border border-dashed border-[var(--border)] p-5 text-sm leading-7 text-[var(--muted)]">
        Search results, query controls, and metadata panels will be added in a later task.
      </div>
    </SiteShell>
  );
}
