CREATE TABLE urls (
  id TEXT PRIMARY KEY,
  url TEXT NOT NULL,
  original_url TEXT NOT NULL,
  title TEXT,
  saved_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  processing_status TEXT NOT NULL DEFAULT 'pending',
  processing_error TEXT,
  markdown_content TEXT,
  system_rating INTEGER CHECK (system_rating IS NULL OR system_rating BETWEEN 1 AND 10),
  ai_summary TEXT,
  ai_tags TEXT,
  ai_reasoning TEXT,
  source_application TEXT,
  tags TEXT
);

CREATE INDEX idx_urls_saved_at ON urls(saved_at DESC);
CREATE INDEX idx_urls_processing_status ON urls(processing_status);
CREATE UNIQUE INDEX idx_urls_url ON urls(url);
