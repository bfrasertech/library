import Link from "next/link";
import { ReactNode } from "react";

import { getApiBaseUrl } from "@/lib/config";

const navItems = [
  { href: "/", label: "Home" },
  { href: "/search", label: "Search" },
  { href: "/chat", label: "Chat" }
];

type SiteShellProps = {
  eyebrow: string;
  title: string;
  description: string;
  children?: ReactNode;
};

export function SiteShell({ eyebrow, title, description, children }: SiteShellProps) {
  const apiBaseUrl = getApiBaseUrl();

  return (
    <main className="min-h-screen px-6 py-8 md:px-10">
      <div className="mx-auto flex w-full max-w-6xl flex-col gap-6">
        <header className="rounded-[28px] border border-[var(--border)] bg-[var(--surface)] p-4 shadow-[0_20px_60px_rgba(29,26,22,0.08)] backdrop-blur md:p-6">
          <div className="flex flex-col gap-6 md:flex-row md:items-end md:justify-between">
            <div className="max-w-2xl">
              <p className="text-xs uppercase tracking-[0.35em] text-[var(--muted)]">{eyebrow}</p>
              <h1 className="mt-3 text-4xl leading-none md:text-6xl">{title}</h1>
              <p className="mt-4 max-w-xl text-base leading-7 text-[var(--muted)]">{description}</p>
            </div>
            <div className="rounded-[20px] border border-[var(--border)] bg-[var(--surface-strong)] px-4 py-3 text-sm text-[var(--muted)]">
              <div className="uppercase tracking-[0.25em] text-[11px]">API Base URL</div>
              <div className="mt-2 font-mono text-xs text-[var(--foreground)]">{apiBaseUrl}</div>
            </div>
          </div>
          <nav className="mt-6 flex flex-wrap gap-2">
            {navItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className="rounded-full border border-[var(--border)] px-4 py-2 text-sm transition hover:border-[var(--accent)] hover:text-[var(--accent)]"
              >
                {item.label}
              </Link>
            ))}
          </nav>
        </header>

        <section className="rounded-[28px] border border-[var(--border)] bg-[var(--surface)] p-5 shadow-[0_16px_50px_rgba(29,26,22,0.06)] backdrop-blur md:p-7">
          {children}
        </section>
      </div>
    </main>
  );
}
