import '../../../setup/vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, expect, it, vi } from 'vitest'
import { ObjectiveFormRoute } from '@/modules/objectives/routes/ObjectiveFormRoute'
afterEach(() => vi.unstubAllGlobals())
it('creates an objective with interest and symmetric priority', async () => {
  const user = userEvent.setup(); const fetch = vi.fn().mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf' }))).mockResolvedValueOnce(new Response(JSON.stringify({ id: '1' }), { status: 201 })); vi.stubGlobal('fetch', fetch)
  render(<MemoryRouter initialEntries={['/objetivos/novo']}><Routes><Route path="/objetivos/novo" element={<ObjectiveFormRoute />} /><Route path="/objetivos" element={<div>Lista</div>} /></Routes></MemoryRouter>)
  expect(screen.getAllByText('opcional', { selector: '.optional' })).toHaveLength(3)
  screen.getAllByText('opcional', { selector: '.optional' }).forEach(label => expect(label.parentElement).toHaveClass('field-label'))
  await user.type(screen.getByLabelText('Nome'), 'Notebook')
  await user.type(screen.getByLabelText('Valor estimado'), '600000')
  await user.click(screen.getByRole('button', { name: 'Desejado' }))
  await user.click(screen.getByRole('button', { name: 'Prioridade 2: Alta' }))
  await user.click(screen.getByRole('button', { name: 'Salvar objetivo' }))
  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith('/api/v1/objectives', expect.objectContaining({ method: 'POST', body: expect.stringContaining('"prioridade":2') })))
})
