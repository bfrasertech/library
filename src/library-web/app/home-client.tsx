"use client";

import { FormEvent, useEffect, useState, useTransition } from "react";

import { DeleteUrlButton } from "@/components/delete-url-button";
import { UrlCard } from "@/components/url-card";
import { listUrls, saveUrl, type UrlRecord } from "@/lib/api";

export function HomeClient() {
  const [records, setRecords] = useState<UrlRecord[]>([]);
  const [url, setUrl] = useState("");
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [isPending, startTransition] = useTransition();

  async function refresh() {
    try {
      setLoading(true);
      setError(null);
      setRecords(await listUrls());
    } catch (refreshError) {
      setError(refreshError instanceof Error ? refreshError.message : "Failed to load saved URLs.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void refresh();
  }, []);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setNotice(null);

    startTransition(async () => {
      try {
        const saved = await saveUrl({
          url,
          title: title || undefined
        });

        setUrl("");
        setTitle("");
        setNotice(`Saved ${saved.url} and queued background processing.`);
        await refresh();
      } catch (submitError) {
        setError(submitError instanceof Error ? submitError.message : "Failed to save the URL.");
      }
    });
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,22rem)_minmax(0,1fr)]">
      <section className="rounded-[24px] border border-[var(--border)] bg-[var(--surface-strong)] p-5">
        <p className="text-xs uppercase tracking-[0.25em] text-[var(--muted)]">Save URL</p>
        <form className="mt-4 space-y-4" onSubmit={handleSubmit}>
          <label className="block">
            <span className="text-sm text-[var(--muted)]">URL</span>
            <input
              required
              type="url"
              value={url}
              onChange={(event) => setUrl(event.target.value)}
              placeholder="https://example.com/article"
              className="mt-2 w-full rounded-[18px] border border-[var(--border)] bg-white/70 px-4 py-3 text-sm outline-none transition focus:border-[var(--accent)]"
            />
          </label>
          <label className="block">
            <span className="text-sm text-[var(--muted)]">Optional title</span>
            <input
              type="text"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              placeholder="Useful label for the article"
              className="mt-2 w-full rounded-[18px] border border-[var(--border)] bg-white/70 px-4 py-3 text-sm outline-none transition focus:border-[var(--accent)]"
            />
          </label>
          <button
            type="submit"
            disabled={isPending}
            className="w-full rounded-full bg-[var(--accent)] px-4 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isPending ? "Saving..." : "Save URL"}
          </button>
        </form>
        {notice ? <p className="mt-4 text-sm text-emerald-700">{notice}</p> : null}
        {error ? <p className="mt-4 text-sm text-rose-700">{error}</p> : null}
      </section>

      <section>
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-xs uppercase tracking-[0.25em] text-[var(--muted)]">Saved library</p>
            <h2 className="mt-2 text-2xl">Recent URLs</h2>
          </div>
          <button
            type="button"
            onClick={() => void refresh()}
            className="rounded-full border border-[var(--border)] px-4 py-2 text-sm transition hover:border-[var(--accent)] hover:text-[var(--accent)]"
          >
            Refresh
          </button>
        </div>

        <div className="mt-5 space-y-4">
          {loading ? <p className="text-sm text-[var(--muted)]">Loading saved URLs...</p> : null}
          {!loading && records.length === 0 ? (
            <div className="rounded-[24px] border border-dashed border-[var(--border)] p-6 text-sm leading-7 text-[var(--muted)]">
              No URLs saved yet. Add one from the form to start the extraction and enrichment pipeline.
            </div>
          ) : null}
          {records.map((record) => (
            <UrlCard
              key={record.id}
              record={record}
              actions={
                <DeleteUrlButton
                  id={record.id}
                  onDeleted={() => setRecords((current) => current.filter((item) => item.id !== record.id))}
                />
              }
            />
          ))}
        </div>
      </section>
    </div>
  );
}
