import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Send, Square } from 'lucide-react'
import { Button } from '@/components/primitives/Button'
import { ApiError } from '@/infrastructure/http/client'
import { getChatConnection, sendChat, type ChatConnection, type ChatMessage, type ChatResponse } from '../api/lumenApi'

type Entry = ChatMessage & { response?: ChatResponse }

export function ChatRoute() {
  const [connection, setConnection] = useState<ChatConnection | null>(null)
  const [entries, setEntries] = useState<Entry[]>([])
  const [draft, setDraft] = useState('')
  const [model, setModel] = useState('')
  const [modelDraft, setModelDraft] = useState('')
  const [sessionId, setSessionId] = useState(() => crypto.randomUUID())
  const [pending, setPending] = useState('')
  const [error, setError] = useState('')
  const controller = useRef<AbortController | null>(null)
  const latest = useRef<HTMLDivElement>(null)
  const modelPicker = useRef<HTMLDetailsElement>(null)

  useEffect(() => {
    const request = new AbortController()
    getChatConnection(request.signal).then(value => {
      setConnection(value)
      setModel(value.modelo ?? '')
      setModelDraft(value.modelo ?? '')
    }).catch(() => {
      if (!request.signal.aborted) setError('Não foi possível verificar a conexão. Reabra a conversa para tentar novamente.')
    })
    return () => { request.abort(); controller.current?.abort() }
  }, [])

  useEffect(() => { latest.current?.scrollIntoView({ block: 'nearest' }) }, [entries, pending])

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (controller.current || !draft.trim() || !connection?.disponivel || !model.trim()) return
    const question = draft.trim()
    const history: ChatMessage[] = [...entries.map(({ papel, conteudo }) => ({ papel, conteudo })), { papel: 'usuario', conteudo: question }]
    while (history.length > 11 || history.reduce((total, entry) => total + entry.conteudo.length, 0) > 24000) history.splice(0, 2)
    const request = new AbortController()
    controller.current = request
    setPending(question); setError('')
    try {
      const now = new Date()
      const reference = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
      const response = await sendChat(history, reference, model.trim(), sessionId, request.signal)
      setEntries(current => [...current.slice(-38), { papel: 'usuario', conteudo: question }, { papel: 'seryn', conteudo: response.resposta, response }])
      setDraft('')
    } catch (cause) {
      if (!request.signal.aborted) setError(cause instanceof ApiError ? Object.values(cause.errors).flat()[0] ?? cause.message : 'Não foi possível obter a resposta. Tente novamente.')
      else setError('Resposta interrompida. O provedor ainda pode contabilizar o processamento iniciado.')
    } finally { controller.current = null; setPending('') }
  }

  function changeModel(event: FormEvent) {
    event.preventDefault()
    const next = modelDraft.trim()
    if (!next || next.length > 160) return
    if (next !== model && entries.length > 0 && !window.confirm('Trocar o modelo iniciará uma nova conversa. Deseja continuar?')) {
      setModelDraft(model)
      return
    }
    if (next !== model) {
      setModel(next)
      setSessionId(crypto.randomUUID())
      setEntries([])
      setError('')
    }
    if (modelPicker.current) modelPicker.current.open = false
  }

  function newConversation() {
    const defaultModel = connection?.modelo ?? ''
    setEntries([])
    setDraft('')
    setError('')
    setSessionId(crypto.randomUUID())
    setModel(defaultModel)
    setModelDraft(defaultModel)
  }

  const suggestions = ['Quais são minhas maiores saídas?', 'O que mais pesa nas minhas assinaturas?', 'Como está minha projeção para este ano?']

  return <section className="chat-page" aria-label="Conversa com a Seryn">
      <header className="chat-header"><div><h1 tabIndex={-1}>Seryn</h1><p className="muted">{connection?.disponivel ? connection.provedor : connection ? 'Provedor não configurado' : 'Verificando conexão…'}</p></div>
        <div className="chat-header-actions"><details className="chat-model-picker" ref={modelPicker}>
          <summary aria-label={`Modelo da conversa: ${model || 'não definido'}`}>{model || 'Modelo'}</summary>
          <form onSubmit={changeModel}><label htmlFor="chat-model">Modelo da conversa</label><input id="chat-model" value={modelDraft} onChange={event => setModelDraft(event.target.value)} maxLength={160} required disabled={Boolean(pending)} autoComplete="off" />
            <p className="muted">O modelo padrão desta conexão é {connection?.modelo ?? 'não definido'}.</p><Button type="submit" disabled={!modelDraft.trim() || Boolean(pending)}>Usar modelo</Button></form>
        </details>
        <Button variant="ghost" disabled={Boolean(pending) || (entries.length === 0 && model === (connection?.modelo ?? ''))} onClick={newConversation}><Plus size={18} aria-hidden="true" />Nova conversa</Button></div>
      </header>
      <div className="chat-messages" role="log" aria-label="Mensagens" aria-live="polite" aria-busy={Boolean(pending)}>
        {entries.length === 0 && !pending && <div className="chat-empty"><h2>Como posso ajudar?</h2><p>Converse naturalmente sobre seus registros e valores previstos.</p><div className="chat-suggestions">{suggestions.map(suggestion => <button type="button" key={suggestion} disabled={!connection?.disponivel} onClick={() => setDraft(suggestion)}>{suggestion}</button>)}</div></div>}
        {entries.map((entry, index) => <article key={index} className={`chat-message chat-message-${entry.papel}`}>
          <strong>{entry.papel === 'usuario' ? 'Você' : 'Seryn'}</strong><div className="chat-text">{entry.conteudo}</div>
          {entry.response && <>
            {entry.response.consultas.length > 0 && <details className="chat-queries"><summary>Consultas usadas ({entry.response.consultas.length})</summary>
              {entry.response.consultas.map((query, queryIndex) => <div key={queryIndex}><strong>{query.ferramenta}</strong>
                <pre>{JSON.stringify({ argumentos: query.argumentos, resultado: query.resultado }, null, 2)}</pre>
                <ul>{query.fontes.map(source => <li key={source.caminho}><Link to={source.caminho} target="_blank" rel="noopener noreferrer">{source.nome} <span className="sr-only">(abre em outra aba)</span></Link></li>)}</ul>
              </div>)}
            </details>}
            <p className="chat-usage muted">{entry.response.modelo} · {entry.response.uso.entradaTokens} tokens de entrada · {entry.response.uso.saidaTokens} de saída
              {entry.response.uso.custoUsd !== null && ` · ${new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'USD', maximumFractionDigits: 6 }).format(entry.response.uso.custoUsd)}`}</p>
          </>}
        </article>)}
        {pending && <><article className="chat-message chat-message-usuario"><strong>Você</strong><div className="chat-text">{pending}</div></article><p role="status" className="muted">A Seryn está consultando e preparando a resposta…</p></>}
        <div ref={latest} />
      </div>
      {error && <p role="alert" className="form-error">{error}</p>}
      <form className="chat-composer" onSubmit={event => { void submit(event) }}>
        <label className="sr-only" htmlFor="chat-question">Pergunte à Seryn</label><textarea id="chat-question" value={draft} onChange={event => setDraft(event.target.value)} maxLength={4000} rows={1} required disabled={Boolean(pending) || !connection?.disponivel} placeholder="Pergunte à Seryn…" />
        {pending ? <Button type="button" variant="outline" aria-label="Interromper resposta" onClick={() => controller.current?.abort()}><Square size={16} aria-hidden="true" /></Button>
          : <Button type="submit" aria-label="Enviar pergunta" disabled={!connection?.disponivel || !model.trim() || !draft.trim()}><Send size={18} aria-hidden="true" /></Button>}
      </form>
      <p className="chat-footnote muted">A Seryn consulta dados previstos em modo somente leitura. As mensagens e os dados necessários são enviados ao provedor configurado; esta conversa não é salva.</p>
    </section>
}
