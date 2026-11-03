import { request, type FullConfig } from '@playwright/test'

export default async function setup(config: FullConfig) {
  if (process.env.SERYN_E2E !== '1') throw new Error('Use SERYN_E2E=1 apenas contra uma instalação descartável de teste.')
  const api = await request.newContext({ baseURL: config.projects[0].use.baseURL })
  const status = await (await api.get('/api/v1/auth/status')).json() as { configured: boolean }
  if (!status.configured) {
    await api.dispose()
    throw new Error('Provisione a instalação descartável com SERYN_ADMIN_USER=e2e e a senha de teste documentada antes de executar E2E.')
  }
  await api.dispose()
}
