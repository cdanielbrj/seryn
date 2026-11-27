import '../../../setup/vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, expect, it, vi } from 'vitest'
import { ChatRoute } from '@/modules/lumen/chat/ChatRoute'

afterEach(() => vi.unstubAllGlobals())

it('starts with the connection default and sends a conversation model override', async () => {
  Element.prototype.scrollIntoView = vi.fn()
  const fetch = vi.fn()
    .mockResolvedValueOnce(new Response(JSON.stringify({ disponivel: true, provedor: 'OpenRouter', modelo: 'openrouter/free' })))
    .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf' })))
    .mockResolvedValueOnce(new Response(JSON.stringify({
      resposta: 'OK', modelo: 'google/gemma-test:free', consultas: [],
      uso: { entradaTokens: 10, saidaTokens: 2, custoUsd: 0 },
    })))
  vi.stubGlobal('fetch', fetch)
  render(<ChatRoute />)

  await userEvent.click(await screen.findByLabelText('Modelo da conversa: openrouter/free'))
  const model = screen.getByLabelText('Modelo da conversa')
  await userEvent.clear(model)
  await userEvent.type(model, 'google/gemma-test:free')
  await userEvent.click(screen.getByRole('button', { name: 'Usar modelo' }))
  await userEvent.type(screen.getByLabelText('Pergunte à Seryn'), 'Responda OK')
  await userEvent.click(screen.getByRole('button', { name: 'Enviar pergunta' }))

  expect(await screen.findByText('OK')).toBeInTheDocument()
  await waitFor(() => expect(fetch).toHaveBeenCalledTimes(3))
  const body = JSON.parse(fetch.mock.calls[2][1].body)
  expect(body.modelo).toBe('google/gemma-test:free')
  expect(body.sessao).toMatch(/^[0-9a-f-]{36}$/)
  expect(screen.getByText(/google\/gemma-test:free · 10 tokens de entrada/)).toBeInTheDocument()
})
