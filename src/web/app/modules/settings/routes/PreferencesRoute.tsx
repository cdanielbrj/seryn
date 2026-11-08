import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeftRight, ChevronRight, CreditCard, LogOut, Tag } from 'lucide-react'
import { ApiError, logout as endSession } from '@/modules/auth/api/authApi'
import { Button } from '@/components/primitives/Button'
import { ThemeControl } from '@/components/patterns/ThemeControl'

export function PreferencesRoute({ username, onLogout }: { username: string; onLogout: () => void }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  async function logout() {
    setBusy(true); setError('')
    try { await endSession(); onLogout() }
    catch (cause) {
      if (cause instanceof ApiError && cause.status === 401) onLogout()
      else setError('Não foi possível encerrar a sessão. Tente novamente.')
    } finally { setBusy(false) }
  }
  return <><header className="page-heading"><h1 tabIndex={-1}>Ajustes</h1><p>Seu espaço, do seu jeito.</p></header><div className="settings-grid">
    <section className="panel"><h2>Aparência</h2><p className="muted">Escolha como a Seryn aparece neste dispositivo.</p><ThemeControl expanded /></section>
    <section className="panel"><h2>Seu acesso</h2><p className="muted">Conectado como <strong>{username}</strong>.</p><Button variant="outline" onClick={() => { void logout() }} disabled={busy}><LogOut size={18} aria-hidden="true" />{busy ? 'Saindo…' : 'Sair da Seryn'}</Button>{error && <p className="form-error" role="alert">{error}</p>}</section>
    <section className="panel"><h2>Organização e portabilidade</h2><div className="settings-links"><Link to="/tipos"><Tag aria-hidden="true" />Tipos<ChevronRight aria-hidden="true" /></Link><Link to="/pagamentos"><CreditCard aria-hidden="true" />Pagamentos<ChevronRight aria-hidden="true" /></Link><Link to="/importar-exportar"><ArrowLeftRight aria-hidden="true" />Importar / Exportar<ChevronRight aria-hidden="true" /></Link></div></section>
    <section className="panel"><h2>Sobre esta versão</h2><p className="muted">Fundação da Seryn: acesso seguro, navegação e temas. O cadastro de dados e as projeções chegam na próxima etapa.</p><p className="muted">Todos os valores da Seryn são previsões de planejamento.</p></section>
  </div></>
}
