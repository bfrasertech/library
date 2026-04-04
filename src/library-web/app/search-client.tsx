"use client";

import { FormEvent, useState, useTransition } from "react";

import { UrlCard } from "@/components/url-card";
import { searchUrls, type SearchResultItem } from "@/lib/api";

export function SearchClient() {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<SearchResultItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    startTransition(async () => {
      try {
        setResults(await searchUrls(query));
      } catch (searchError) {
        setError(searchError instanceof Error ? searchError.message : "Search failed.");
      }
    });
  }

  return (
    <div className="space-y-6">
      <form onSubmit={handleSubmit} className="rounded-[22px] border border-[var(--border)] bg-white/40 p-5">
        <label className="block">
          <span className="text-sm text-[var(--muted)]">Semantic query</span>
          <div className="mt-3 flex flex-col gap-3 md:flex-row">
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="distributed systems, vector search, browser runtimes..."
              className="min-w-0 flex-1 rounded-[18px] border border-[var(--border)] bg-white/70 px-4 py-3 text-sm outline-none transition focus:border-[var(--accent)]"
            />
            <button
              type="submit"
              disabled={isPending}
              className="rounded-full bg-[var(--accent)] px-5 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:opacity-60"
            >
              {isPending ? "Searching..." : "Search"}
            </button>
          </div>
        </label>
        <p className="mt-3 text-xs leading-6 text-[var(--muted)]">
          Results are semantic-only. The API embeds the query and asks Vectorize for nearby article vectors.
        </p>
        {error ? <p className="mt-3 text-sm text-rose-700">{error}</p> : null}
      </form>

      <div className="space-y-4">
        {results.map((result) => (
          <UrlCard
            key={result.id}
            record={result.record}
            actions={
              <div className="rounded-full border border-[var(--border)] px-3 py-2 text-xs text-[var(--muted)]">
                score {result.score.toFixed(3)}
              </div>
            }
          />
        ))}
        {!isPending && results.length === 0 && !error ? (
          <div className="rounded-[22px] border border-dashed border-[var(--border)] p-5 text-sm leading-7 text-[var(--muted)]">
            Run a query to see semantic matches from the saved corpus.
          </div>
        ) : null}
      </div>
    </div>
  );
}
