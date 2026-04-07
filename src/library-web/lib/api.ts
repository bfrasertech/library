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

type UrlRecordApi = {
  id: string;
  url: string;
  original_url: string;
  title: string | null;
  saved_at: string;
  processing_status: string;
  processing_error: string | null;
  markdown_content: string | null;
  system_rating: number | null;
  ai_summary: string | null;
  ai_tags: string | null;
  ai_reasoning: string | null;
  source_application: string | null;
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

function toUrlRecord(record: UrlRecordApi): UrlRecord {
  return {
    id: record.id,
    url: record.url,
    originalUrl: record.original_url,
    title: record.title,
    savedAt: record.saved_at,
    processingStatus: record.processing_status,
    processingError: record.processing_error,
    markdownContent: record.markdown_content,
    systemRating: record.system_rating,
    aiSummary: record.ai_summary,
    aiTags: record.ai_tags,
    aiReasoning: record.ai_reasoning,
    sourceApplication: record.source_application,
    tags: record.tags
  };
}

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
  return (await parseJson<UrlRecordApi[]>(response)).map(toUrlRecord);
}

export async function getUrl(id: string): Promise<UrlRecord> {
  const response = await fetch(`${getApiBaseUrl()}/api/urls/${id}`);
  return toUrlRecord(await parseJson<UrlRecordApi>(response));
}

export async function saveUrl(input: { url: string; title?: string }): Promise<UrlRecord> {
  const response = await fetch(`${getApiBaseUrl()}/api/urls`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(input)
  });

  return toUrlRecord(await parseJson<UrlRecordApi>(response));
}

export async function searchUrls(query: string): Promise<SearchResultItem[]> {
  const response = await fetch(
    `${getApiBaseUrl()}/api/urls/search?q=${encodeURIComponent(query)}`
  );

  const items = await parseJson<Array<{ id: string; score: number; record: UrlRecordApi }>>(response);
  return items.map((item) => ({
    id: item.id,
    score: item.score,
    record: toUrlRecord(item.record)
  }));
}

export async function askChat(question: string): Promise<ChatResponse> {
  const response = await fetch(`${getApiBaseUrl()}/api/chat`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ question })
  });

  return parseJson<ChatResponse>(response);
}

export async function deleteUrl(id: string): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/api/urls/${id}`, {
    method: "DELETE"
  });

  if (response.ok) {
    return;
  }

  await parseJson<never>(response);
}
