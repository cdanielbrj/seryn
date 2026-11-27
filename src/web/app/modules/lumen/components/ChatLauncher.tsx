import { MessageCircle } from 'lucide-react'
import { Link, useLocation } from 'react-router-dom'

export function ChatLauncher() {
  const inChat = useLocation().pathname === '/conversa'
  if (inChat) return null
  return <Link className="chat-launcher" to="/conversa" aria-label="Conversar com a Seryn">
    <MessageCircle aria-hidden="true" />
  </Link>
}
