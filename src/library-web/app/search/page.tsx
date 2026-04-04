import { SearchClient } from "@/app/search-client";
import { SiteShell } from "@/components/site-shell";

export default function SearchPage() {
  return (
    <SiteShell
      eyebrow="Semantic Search"
      title="Search"
      description="Query the saved corpus by meaning instead of keywords. The backend embeds the query and asks Vectorize for the nearest article matches."
    >
      <SearchClient />
    </SiteShell>
  );
}
