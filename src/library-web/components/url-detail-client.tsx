"use client";

import { useEffect, useState } from "react";

import { SiteShell } from "@/components/site-shell";
import { getUrl, type UrlRecord } from "@/lib/api";

export function UrlDetailClient({ id }: { id: string }) {
  const [record, setRecord] = useState<UrlRecord | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      try {
        setError(null);
        setRecord(await getUrl(id));
      } catch (loadError) {
        setError(loadError instanceof Error ? loadError.message : "Failed to load the URL detail.");
      }
    }

    void load();
  }, [id]);

  return (
    <SiteShell
      eyebrow="URL Detail"
      title={record?.title ?? "Loading article detail"}
      description="Detail view for the saved article, including the enrichment fields returned by the processing pipeline."
    >
      {error ? <p className="text-sm text-rose-700">{error}</p> : null}
      {!record && !error ? <p className="text-sm text-[var(--muted)]">Loading record...</p> : null}
      {record ? (
        <div className="space-y-6">
          <section className="grid gap-4 md:grid-cols-4">
            <div className="rounded-[22px] border border-[var(--border)] bg-white/40 p-4">
              <div className="text-xs uppercase tracking-[0.25em] text-[var(--muted)]">Status</div>
              <div className="mt-3 text-lg capitalize">{record.processingStatus}</div>
            </div>
            <div className="rounded-[22px] border border-[var(--border)] bg-white/40 p-4">
              <div className="text-xs uppercase tracking-[0.25em] text-[var(--muted)]">Score</div>
              <div className="mt-3 text-lg">{record.systemRating ? `${record.systemRating}/10` : "Pending"}</div>
            </div>
            <div className="rounded-[22px] border border-[var(--border)] bg-white/40 p-4 md:col-span-2">
              <div className="text-xs uppercase tracking-[0.25em] text-[var(--muted)]">Source URL</div>
              <div className="mt-3 break-all font-mono text-xs text-[var(--foreground)]">{record.url}</div>
            </div>
          </section>

          <section className="rounded-[22px] border border-[var(--border)] bg-white/40 p-5">
            <h2 className="text-lg">Summary</h2>
            <p className="mt-3 text-sm leading-7 text-[var(--muted)]">
              {record.aiSummary ?? "No AI summary has been saved for this record yet."}
            </p>
          </section>

          <section className="grid gap-4 md:grid-cols-2">
            <article className="rounded-[22px] border border-[var(--border)] bg-white/40 p-5">
              <h2 className="text-lg">Tags</h2>
              <p className="mt-3 text-sm leading-7 text-[var(--muted)]">
                {record.aiTags ?? "No tags have been generated yet."}
              </p>
            </article>
            <article className="rounded-[22px] border border-[var(--border)] bg-white/40 p-5">
              <h2 className="text-lg">Reasoning</h2>
              <p className="mt-3 text-sm leading-7 text-[var(--muted)]">
                {record.aiReasoning ?? "No assessment reasoning is available yet."}
              </p>
            </article>
          </section>

          <section className="rounded-[22px] border border-[var(--border)] bg-white/40 p-5">
            <h2 className="text-lg">Markdown content</h2>
            <pre className="mt-4 overflow-x-auto whitespace-pre-wrap text-sm leading-7 text-[var(--muted)]">
              {record.markdownContent ?? "No markdown content has been extracted yet."}
            </pre>
          </section>
        </div>
      ) : null}
    </SiteShell>
  );
}
