import { HomeClient } from "@/app/home-client";
import { SiteShell } from "@/components/site-shell";

export default function HomePage() {
  return (
    <SiteShell
      eyebrow="Library"
      title="Capture URLs. Let the pipeline read the rest."
      description="Save an article, watch its processing status update, and browse the enriched results as the background pipeline fills in summaries and scores."
    >
      <HomeClient />
    </SiteShell>
  );
}
