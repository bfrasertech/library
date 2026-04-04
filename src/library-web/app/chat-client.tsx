"use client";

import { FormEvent, useState, useTransition } from "react";

import { askChat, type ChatResponse } from "@/lib/api";

export function ChatClient() {
  const [question, setQuestion] = useState("");
  const [response, setResponse] = useState<ChatResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    startTransition(async () => {
      try {
        setResponse(await askChat(question));
      } catch (chatError) {
        setError(chatError instanceof Error ? chatError.message : "Chat request failed.");
      }
    });
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,22rem)_minmax(0,1fr)]">
      <form onSubmit={handleSubmit} className="rounded-[22px] border border-[var(--border)] bg-white/40 p-5">
        <label className="block">
          <span className="text-sm text-[var(--muted)]">Question</span>
          <textarea
            value={question}
            onChange={(event) => setQuestion(event.target.value)}
            rows={8}
            placeholder="What have I saved about semantic retrieval?"
            className="mt-3 w-full rounded-[18px] border border-[var(--border)] bg-white/70 px-4 py-3 text-sm outline-none transition focus:border-[var(--accent)]"
          />
        </label>
        <button
          type="submit"
          disabled={isPending}
          className="mt-4 w-full rounded-full bg-[var(--accent)] px-5 py-3 text-sm font-medium text-white transition hover:opacity-90 disabled:opacity-60"
        >
          {isPending ? "Asking..." : "Ask grounded chat"}
        </button>
        {error ? <p className="mt-3 text-sm text-rose-700">{error}</p> : null}
      </form>

      <section className="rounded-[22px] border border-[var(--border)] bg-white/40 p-5">
        <h2 className="text-lg">Answer</h2>
        <p className="mt-4 whitespace-pre-wrap text-sm leading-7 text-[var(--muted)]">
          {response?.answer ?? "Submit a question to generate a grounded answer from your saved articles."}
        </p>

        <div className="mt-6">
          <h3 className="text-sm uppercase tracking-[0.25em] text-[var(--muted)]">Sources</h3>
          <div className="mt-3 space-y-3">
            {response?.sources.map((source) => (
              <article key={source.id} className="rounded-[18px] border border-[var(--border)] px-4 py-3">
                <div className="text-sm">{source.title}</div>
                <div className="mt-1 break-all font-mono text-xs text-[var(--muted)]">{source.url}</div>
              </article>
            ))}
            {response && response.sources.length === 0 ? (
              <p className="text-sm text-[var(--muted)]">No source citations were returned for this answer.</p>
            ) : null}
          </div>
        </div>
      </section>
    </div>
  );
}
