import { Route, Routes } from 'react-router-dom'
import { ArrowLeftRight, Home } from 'lucide-react'
import { UnavailableState } from '@/components/patterns/UnavailableState'
import { CategoriesRoute } from '@/modules/classifications/routes/CategoriesRoute'
import { ClassificationsRoute } from '@/modules/classifications/routes/ClassificationsRoute'
import { DashboardRoute } from '@/modules/dashboard/routes/DashboardRoute'
import { RecordFormRoute } from '@/modules/records/routes/RecordFormRoute'
import { RecordsRoute } from '@/modules/records/routes/RecordsRoute'
import { PreferencesRoute } from '@/modules/settings/routes/PreferencesRoute'

export function AppRouter({ username, onLogout }: { username: string; onLogout: () => void }) {
  return <Routes>
    <Route path="/" element={<DashboardRoute />} />
    <Route path="/registros" element={<RecordsRoute />} />
    <Route path="/registros/novo" element={<RecordFormRoute />} />
    <Route path="/registros/:id/editar" element={<RecordFormRoute />} />
    <Route path="/pagamentos" element={<ClassificationsRoute key="payment-methods" kind="payment-methods" />} />
    <Route path="/categorias" element={<CategoriesRoute />} />
    <Route path="/tipos" element={<ClassificationsRoute key="types" kind="types" />} />
    <Route path="/importar-exportar" element={<UnavailableState title="Importar / Exportar" icon={ArrowLeftRight} description="Seus registros, com portabilidade." detail="A importação e exportação de registros originais em CSV e Excel serão disponibilizadas na etapa de portabilidade." />} />
    <Route path="/ajustes" element={<PreferencesRoute username={username} onLogout={onLogout} />} />
    <Route path="*" element={<UnavailableState title="Página não encontrada" icon={Home} description="Este endereço não existe na Seryn." detail="Use a navegação para voltar ao seu planejamento." />} />
  </Routes>
}
