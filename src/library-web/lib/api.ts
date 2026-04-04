"use client";

import { getApiBaseUrl } from "@/lib/config";

export type UrlRecord = {
  id: string;
  url: string;
  originalUrl: string;
  title: string | null;
  savedAt: string;
  processingStatus: string;
  processingError: string | null;
  markdownContent: string | null;
  systemRating: number | null;
  aiSummary: string | null;
  aiTags: string | null;
  aiReasoning: string | null;
  sourceApplication: string | null;
  tags: string | null;
};

export type SearchResultItem = {
  id: string;
  score: number;
  record: UrlRecord;
};

export type ChatSource = {
  id: string;
  title: string;
  url: string;
  score: number;
};

export type ChatResponse = {
  answer: string;
  sources: ChatSource[];
};

async function parseJson<T>(response: Response): Promise<T> {
  if (response.ok) {
    return (await response.json()) as T;
  }

  let message = `Request failed with status ${response.status}.`;

  try {
    const errorPayload = (await response.json()) as { error?: string; detail?: string; title?: string };
    message = errorPayload.error ?? errorPayload.detail ?? errorPayload.title ?? message;
  } catch {
    // ignore non-JSON error payloads
  }

  throw new Error(message);
}

export async function listUrls(): Promise<UrlRecord[]> {
  const response = await fetch(`${getApiBaseUrl()}/api/urls`);
  return parseJson<UrlRecord[]>(response);
}

export async function saveUrl(input: { url: string; title?: string }): Promise<UrlRecord> {
  const response = await fetch(`${getApiBaseUrl()}/api/urls`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(input)
  });

  return parseJson<UrlRecord>(response);
}
