# Library

Lightweight AI-assisted bookmark database for local development and blog-post walkthroughs.

## Status

This repository is in active scaffold/build-out. Core save, processing, search, and chat flows are added incrementally through the task sequence in `fts-research`.

## Projects

- `src/Library.Api` — ASP.NET Core minimal API
- `src/library-web` — Next.js frontend
- `d1/migrations` — Cloudflare D1 schema migrations

## Local Setup

1. Copy `.env.example` to `.env` and fill in the required values.
2. Create the Cloudflare D1 database and Vectorize index.
3. Apply D1 migrations with Wrangler.
4. Build and run the API and frontend locally.

## Cloudflare

The repo includes `wrangler.jsonc` for D1 and Vectorize metadata only. It is intended to support local setup commands and future blog documentation, not a deployed Worker runtime.

## Documentation

This README is intentionally skeletal for now. Full setup, architecture, and blog-oriented documentation lands in a later task.
