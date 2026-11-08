import '../../../setup/vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, expect, it, vi } from 'vitest'
import { ClassificationsRoute } from '@/modules/classifications/routes/ClassificationsRoute'

afterEach(() => vi.unstubAllGlobals())

it.each([
  ['types', 'tipo', 'Renda', 'Receita'],
  ['payment-methods', 'pagamento', 'Pix', 'Pix pessoal'],
] as const)('cria, edita e confirma antes de excluir um %s', async (kind, singular, initialName, updatedName) => {
  const fetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    if (url.endsWith(`/${kind}`) && !init?.method)
      return new Response(JSON.stringify([]))
    if (url.endsWith('/auth/csrf'))
      return new Response(JSON.stringify({ token: 'csrf' }))
    if (url.endsWith(`/${kind}`) && init?.method === 'POST')
      return new Response(JSON.stringify({ id: 'classification-1', nome: initialName }), { status: 201 })
    if (url.endsWith(`/${kind}/classification-1`) && init?.method === 'PATCH')
      return new Response(JSON.stringify({ id: 'classification-1', nome: updatedName }))
    if (url.endsWith(`/${kind}/classification-1`) && init?.method === 'DELETE')
      return new Response(null, { status: 204 })
    throw new Error(`Unexpected request: ${init?.method ?? 'GET'} ${url}`)
  })
  vi.stubGlobal('fetch', fetch)
  const user = userEvent.setup()
  render(<ClassificationsRoute kind={kind} />)

  expect(await screen.findByText(`Nenhum ${singular} cadastrado`)).toBeInTheDocument()
  await user.type(screen.getByLabelText('Nome'), initialName)
  await user.click(screen.getByRole('button', { name: 'Adicionar' }))
  expect(await screen.findByText(initialName)).toBeInTheDocument()

  await user.click(screen.getByRole('button', { name: `Editar ${initialName}` }))
  const edit = screen.getByLabelText(`Nome do ${singular}`)
  await user.clear(edit)
  await user.type(edit, updatedName)
  await user.click(screen.getByRole('button', { name: `Salvar ${initialName}` }))
  expect(await screen.findByText(updatedName)).toBeInTheDocument()

  await user.click(screen.getByRole('button', { name: `Excluir ${updatedName}` }))
  expect(screen.getByRole('alertdialog', { name: `Excluir ${singular}?` })).toBeInTheDocument()
  await user.click(screen.getByRole('button', { name: 'Cancelar' }))
  expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
  expect(screen.getByText(updatedName)).toBeInTheDocument()

  await user.click(screen.getByRole('button', { name: `Excluir ${updatedName}` }))
  await user.click(screen.getByRole('button', { name: `Excluir ${singular}` }))
  await waitFor(() => expect(screen.getByText(`Nenhum ${singular} cadastrado`)).toBeInTheDocument())
})
