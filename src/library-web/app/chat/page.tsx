import { ChatClient } from "@/app/chat-client";
import { SiteShell } from "@/components/site-shell";

export default function ChatPage() {
  return (
    <SiteShell
      eyebrow="Grounded Chat"
      title="Chat"
      description="Ask questions against the saved corpus. Answers are grounded in retrieved article context and should cite the matching saved items."
    >
      <ChatClient />
    </SiteShell>
  );
}
