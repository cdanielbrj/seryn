import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, expect, it, vi } from 'vitest'
import { AuthScreen } from './AuthScreen'

afterEach(() => vi.unstubAllGlobals())

it('sem administrador, não permite criar conta remotamente e oferece nova verificação', async () => {
  const retry = vi.fn()
  render(<AuthScreen configured={false} onLogin={vi.fn()} onRetry={retry} />)
  expect(screen.getByRole('heading', { name: 'Aguardando configuração' })).toBeInTheDocument()
  expect(screen.queryByLabelText('Senha')).not.toBeInTheDocument()
  expect(screen.queryByText('Código de configuração')).not.toBeInTheDocument()
  await userEvent.click(screen.getByRole('button', { name: 'Verificar novamente' }))
  expect(retry).toHaveBeenCalledOnce()
})

it('entra com o administrador provisionado usando CSRF e limpa o formulário', async () => {
  const fetch = vi.fn().mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf-test' })))
    .mockResolvedValueOnce(new Response(JSON.stringify({ username: 'admin' })))
  vi.stubGlobal('fetch', fetch)
  const login = vi.fn()
  render(<AuthScreen configured onLogin={login} onRetry={vi.fn()} />)
  const user = userEvent.setup()
  await user.type(screen.getByLabelText('Usuário'), 'admin')
  await user.type(screen.getByLabelText('Senha'), 'Senha longa para testes!')
  await user.click(screen.getByRole('button', { name: 'Entrar' }))
  expect(fetch.mock.calls[0][0]).toBe('/api/v1/auth/csrf')
  expect(fetch.mock.calls[1][0]).toBe('/api/v1/auth/login')
  expect(fetch.mock.calls[1][1].headers['X-CSRF-TOKEN']).toBe('csrf-test')
  expect(login).toHaveBeenCalledWith('admin')
  expect(screen.getByLabelText('Senha')).toHaveValue('')
})
