# Library

Library is a local-first AI-assisted bookmark database built for a blog-post walkthrough. You save a URL, the backend extracts the article, generates an assessment, embeds the content, stores the record in Cloudflare D1, and makes it searchable through Cloudflare Vectorize.

## What is implemented

- ASP.NET Core API in `src/Library.Api`
- Next.js web UI in `src/library-web`
- Cloudflare D1 schema migration in `d1/migrations`
- Direct REST integrations for Cloudflare D1, Cloudflare Vectorize, and OpenAI
- Background enrichment pipeline for extraction, assessment, embedding, and vector indexing
- Semantic search and grounded chat
- Strict delete flow that blocks when required vector cleanup fails

## Prerequisites

- .NET 9 SDK or newer
- Node.js 22+
- pnpm 10+
- A Cloudflare account with:
  - one D1 database
  - one Vectorize index
- An OpenAI API key

## Cloudflare setup

1. Log in to Cloudflare:

```bash
npx wrangler login
```

2. Create the D1 database:

```bash
npx wrangler d1 create library-db
```

3. Apply the schema migration:

```bash
npx wrangler d1 migrations apply library-db --remote
```

4. Create the Vectorize index:

```bash
npx wrangler vectorize create library-vectors --dimensions 1536 --metric cosine
```

5. Record your Cloudflare account ID and D1 database ID from the Wrangler output.

## Configuration

Copy `.env.example` to `.env` and provide values for:

- `NEXT_PUBLIC_API_BASE_URL`
- `CLOUDFLARE_ACCOUNT_ID`
- `CLOUDFLARE_API_TOKEN`
- `CLOUDFLARE_D1_DATABASE_ID`
- `CLOUDFLARE_D1_DATABASE_NAME`
- `CLOUDFLARE_VECTORIZE_INDEX_NAME`
- `OPENAI_API_KEY`
- `OPENAI_EMBEDDING_MODEL`
- `OPENAI_ASSESSMENT_MODEL`
- `OPENAI_CHAT_MODEL`

The API also includes placeholder sections in `appsettings.json` and `appsettings.Development.json`, but environment variables are the intended local override path.

## Local run

Start the API:

```bash
dotnet run --project src/Library.Api/Library.Api.csproj --urls http://127.0.0.1:5099
```

Start the web UI in a second terminal:

```bash
cd src/library-web
pnpm install
pnpm dev
```

The web UI expects the API at `http://localhost:5099` unless `NEXT_PUBLIC_API_BASE_URL` is overridden.

## Build checks

Backend:

```bash
dotnet build src/Library.Api/Library.Api.csproj
```

Frontend:

```bash
cd src/library-web
pnpm build
```

## Architecture

- `src/Library.Api`
  - minimal API endpoints
  - D1 repository
  - background processing orchestration
  - extraction, assessment, embeddings, Vectorize, search, and chat services
- `src/library-web`
  - home/save/list flow
  - detail view
  - semantic search
  - grounded chat
  - strict delete UX
- `d1/migrations/0001_initial.sql`
  - single `urls` table with processing, markdown, and AI result fields

## Processing flow

1. `POST /api/urls` saves the URL in D1 with `pending` status.
2. A best-effort in-process background task updates the record to `processing`.
3. The backend fetches the page and extracts readable article HTML and markdown.
4. Markdown and fallback title are stored in D1.
5. The backend asks OpenAI for an assessment and stores score, summary, tags, and reasoning.
6. The backend generates an embedding and upserts it into Vectorize.
7. The record moves to `completed`.

If a stage fails, processing moves to `failed` and the error message is stored in `processing_error`.

## API reference

### `POST /api/urls`

Save a URL and queue background processing.

Request body:

```json
{
  "url": "https://example.com/article",
  "title": "Optional title"
}
```

Returns `202 Accepted` with the saved record payload.

### `GET /api/urls`

List saved URLs. Supports:

- `pageSize`
- `offset`

### `GET /api/urls/{id}`

Return one saved URL with markdown and enrichment fields.

### `GET /api/urls/search?q=...`

Run semantic-only retrieval against Vectorize and return matching records with similarity scores.

### `POST /api/chat`

Ask a grounded question using retrieved saved content.

Request body:

```json
{
  "question": "What have I saved about semantic search?"
}
```

### `DELETE /api/urls/{id}`

Delete the D1 row. If the record is `completed`, Vectorize cleanup is attempted first and the delete fails if that cleanup fails.

## Web UI

- `/`
  - save form
  - recent URL list
  - status, score, summary snippet
- `/urls/[id]`
  - detail page with summary, tags, reasoning, and markdown
- `/search`
  - semantic query UI
- `/chat`
  - grounded chat UI with returned sources

## Costs

Expected baseline cost is low, but not zero:

- Cloudflare D1 can stay within free-tier limits for small demos
- Cloudflare Vectorize typically requires a paid Cloudflare plan
- OpenAI usage is pay-per-request for assessment, embeddings, and chat

## Known limitations

- Background processing is fire-and-forget and not durable. There is no queue or retry mechanism.
- Duplicate save updates the existing record metadata. It is not treated as a retry or forced reprocess.
- Search is semantic-only. There is no FTS5 table and no hybrid ranking.
- Delete spans D1 and Vectorize but is not transactional across both systems.
- The implementation is optimized for clarity and blog teaching value, not production hardening.

## License

See `LICENSE`.
