import Link from "next/link";

import type { UrlRecord } from "@/lib/api";

type UrlCardProps = {
  record: UrlRecord;
  actions?: React.ReactNode;
};

const statusTone: Record<string, string> = {
  pending: "bg-amber-100 text-amber-800",
  processing: "bg-sky-100 text-sky-800",
  completed: "bg-emerald-100 text-emerald-800",
  failed: "bg-rose-100 text-rose-800"
};

export function UrlCard({ record, actions }: UrlCardProps) {
  const score = record.systemRating ? `${record.systemRating}/10` : "Pending";
  const summary = record.aiSummary ?? record.processingError ?? "No summary available yet.";
  const statusClassName = statusTone[record.processingStatus] ?? "bg-stone-200 text-stone-700";

  return (
    <article className="rounded-[24px] border border-[var(--border)] bg-white/45 p-5">
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <span className={`rounded-full px-3 py-1 text-xs font-medium ${statusClassName}`}>
              {record.processingStatus}
            </span>
            <span className="rounded-full border border-[var(--border)] px-3 py-1 text-xs text-[var(--muted)]">
              Score {score}
            </span>
          </div>
          <h2 className="mt-4 text-xl leading-tight">
            <Link href={`/urls/${record.id}`} className="transition hover:text-[var(--accent)]">
              {record.title ?? "Untitled saved URL"}
            </Link>
          </h2>
          <p className="mt-2 break-all font-mono text-xs text-[var(--muted)]">{record.url}</p>
          <p className="mt-4 text-sm leading-7 text-[var(--muted)]">{summary}</p>
        </div>
        {actions ? <div className="shrink-0">{actions}</div> : null}
      </div>
    </article>
  );
}
