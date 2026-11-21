import { Download, FileSpreadsheet } from 'lucide-react'
import { Button } from '@/components/primitives/Button'
import { exportWorkbook } from '../api/portabilityApi'

export function ExportRoute() {
  return <>
    <header className="page-heading"><h1 tabIndex={-1}>Exportar dados</h1><p>Crie uma cópia portátil das informações inseridas na Seryn.</p></header>
    <section className="panel portability-panel">
      <div className="panel-heading"><div className="square-icon"><FileSpreadsheet aria-hidden="true" /></div><div><h2>Dados do usuário</h2><p>Planilha XLSX com registros, objetivos e classificações em abas separadas.</p></div></div>
      <Button className="portability-file-action" onClick={() => void exportWorkbook()}><Download aria-hidden="true" />Exportar dados</Button>
    </section>
  </>
}
