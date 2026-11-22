import { Navigate, Route, Routes } from 'react-router-dom'
import { Home } from 'lucide-react'
import { UnavailableState } from '@/components/patterns/UnavailableState'
import { CategoriesRoute } from '@/modules/classifications/routes/CategoriesRoute'
import { ClassificationsRoute } from '@/modules/classifications/routes/ClassificationsRoute'
import { DashboardRoute } from '@/modules/dashboard/routes/DashboardRoute'
import { RecordFormRoute } from '@/modules/records/routes/RecordFormRoute'
import { RecordsRoute } from '@/modules/records/routes/RecordsRoute'
import { PreferencesRoute } from '@/modules/settings/routes/PreferencesRoute'
import { ApiTokensRoute } from '@/modules/settings/routes/ApiTokensRoute'
import { AnalysesRoute } from '@/modules/analyses/routes/AnalysesRoute'
import { ObjectiveFormRoute } from '@/modules/objectives/routes/ObjectiveFormRoute'
import { ObjectivesRoute } from '@/modules/objectives/routes/ObjectivesRoute'
import { ExportRoute } from '@/modules/portability/routes/ExportRoute'
import { ImportRoute } from '@/modules/portability/routes/ImportRoute'

export function AppRouter({ username, onLogout }: { username: string; onLogout: () => void }) {
  return <Routes>
    <Route path="/" element={<DashboardRoute />} />
    <Route path="/registros" element={<RecordsRoute />} />
    <Route path="/registros/novo" element={<RecordFormRoute />} />
    <Route path="/registros/:id/editar" element={<RecordFormRoute />} />
    <Route path="/analises" element={<AnalysesRoute />} />
    <Route path="/objetivos" element={<ObjectivesRoute />} />
    <Route path="/objetivos/novo" element={<ObjectiveFormRoute />} />
    <Route path="/objetivos/:id/editar" element={<ObjectiveFormRoute />} />
    <Route path="/pagamentos" element={<ClassificationsRoute key="payment-methods" kind="payment-methods" />} />
    <Route path="/categorias" element={<CategoriesRoute />} />
    <Route path="/tipos" element={<ClassificationsRoute key="types" kind="types" />} />
    <Route path="/exportar" element={<ExportRoute />} />
    <Route path="/importar" element={<ImportRoute />} />
    <Route path="/importar-exportar" element={<Navigate to="/exportar" replace />} />
    <Route path="/ajustes" element={<PreferencesRoute username={username} onLogout={onLogout} />} />
    <Route path="/ajustes/tokens" element={<ApiTokensRoute />} />
    <Route path="*" element={<UnavailableState title="Página não encontrada" icon={Home} description="Este endereço não existe na Seryn." detail="Use a navegação para voltar ao seu planejamento." />} />
  </Routes>
}
