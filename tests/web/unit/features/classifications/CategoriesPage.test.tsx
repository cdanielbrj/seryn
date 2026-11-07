import '../../../setup/vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, expect, it, vi } from 'vitest'
import { CategoriesPage } from '@/features/classifications/CategoriesPage'

afterEach(() => vi.unstubAllGlobals())

it('organiza subcategorias, cria dentro do pai e impede excluir categoria com filhos', async () => {
  const software = { id: 'root-1', nome: 'Software', categoriaPaiId: null }
  const server = { id: 'child-1', nome: 'Servidor', categoriaPaiId: software.id }
  const fetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    if (url.endsWith('/categories') && !init?.method)
      return new Response(JSON.stringify([software, server]))
    if (url.endsWith('/auth/csrf'))
      return new Response(JSON.stringify({ token: 'csrf' }))
    if (url.endsWith('/categories') && init?.method === 'POST') {
      expect(JSON.parse(String(init.body))).toEqual({ nome: 'Streaming', categoriaPaiId: software.id })
      return new Response(JSON.stringify({ id: 'child-2', nome: 'Streaming', categoriaPaiId: software.id }), { status: 201 })
    }
    throw new Error(`Unexpected request: ${init?.method ?? 'GET'} ${url}`)
  })
  vi.stubGlobal('fetch', fetch)
  const user = userEvent.setup()
  render(<CategoriesPage />)

  expect(await screen.findByText('Servidor')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Software possui subcategorias e não pode ser excluída' })).toBeDisabled()
  await user.click(screen.getByRole('button', { name: 'Adicionar subcategoria em Software' }))
  expect(screen.getByLabelText('Subcategoria')).toHaveValue(software.id)
  await user.type(screen.getByLabelText('Nome'), 'Streaming')
  await user.click(screen.getByRole('button', { name: 'Adicionar' }))
  expect(await screen.findByText('Streaming')).toBeInTheDocument()
  expect(screen.getByText('2 subcategorias')).toBeInTheDocument()
})
