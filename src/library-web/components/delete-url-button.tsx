"use client";

import { useState, useTransition } from "react";

import { deleteUrl } from "@/lib/api";

type DeleteUrlButtonProps = {
  id: string;
  onDeleted?: () => void;
};

export function DeleteUrlButton({ id, onDeleted }: DeleteUrlButtonProps) {
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  return (
    <div className="space-y-2">
      <button
        type="button"
        disabled={isPending}
        onClick={() =>
          startTransition(async () => {
            try {
              setError(null);
              await deleteUrl(id);
              onDeleted?.();
            } catch (deleteError) {
              setError(deleteError instanceof Error ? deleteError.message : "Delete failed.");
            }
          })
        }
        className="rounded-full border border-rose-300 px-4 py-2 text-sm text-rose-700 transition hover:bg-rose-50 disabled:opacity-60"
      >
        {isPending ? "Deleting..." : "Delete"}
      </button>
      {error ? <p className="max-w-xs text-xs leading-5 text-rose-700">{error}</p> : null}
    </div>
  );
}
