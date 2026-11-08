import { Brand } from '@/components/patterns/Brand'
import { Button } from '@/components/primitives/Button'

export function ConnectionScreen({ state, onRetry }: { state: 'loading' | 'error'; onRetry: () => void }) {
  return <main className="connection-screen"><Brand /><h1>{state === 'loading' ? 'Preparando seu espaço…' : 'Não foi possível conectar'}</h1><p role="status">{state === 'loading' ? 'Aguarde um instante.' : 'Verifique se a sua instalação está disponível e tente novamente.'}</p>{state === 'error' && <Button onClick={onRetry}>Tentar novamente</Button>}</main>
}
