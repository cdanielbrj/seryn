import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { expect, it } from 'vitest'
import { Dashboard } from './Dashboard'

it('preserva seções do v2 sem inventar valores e permite trocar período', async () => {
  const user = userEvent.setup()
  render(<MemoryRouter><Dashboard /></MemoryRouter>)
  expect(screen.getByRole('heading', { name: 'Projeção do período' })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Por categoria' })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Registros previstos' })).toBeInTheDocument()
  expect(screen.getAllByLabelText('Sem dados')).toHaveLength(3)
  expect(screen.queryByText(/R\$/)).not.toBeInTheDocument()
  await user.click(screen.getByRole('button', { name: 'Semestre' }))
  expect(screen.getByRole('button', { name: 'Semestre' })).toHaveAttribute('aria-pressed', 'true')
  const before = document.querySelector('.date-select span')!.textContent
  await user.click(screen.getByRole('button', { name: 'Próximo período' }))
  expect(document.querySelector('.date-select span')!.textContent).not.toBe(before)
  expect(screen.getByRole('link', { name: 'Ver todos' })).toHaveAttribute('href', '/registros')
})
