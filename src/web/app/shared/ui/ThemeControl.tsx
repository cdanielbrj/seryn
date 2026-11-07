import { useEffect, useId, useState } from 'react'
import { Moon, Sun } from 'lucide-react'

type Preference = 'light' | 'dark' | 'system'
export function ThemeControl({ expanded = false }: { expanded?: boolean }) {
  const id = useId()
  const [theme, setTheme] = useState(document.documentElement.dataset.theme ?? 'light')
  const [preference, setPreference] = useState<Preference>(() => {
    try { const value = localStorage.getItem('seryn-theme'); return value === 'light' || value === 'dark' ? value : 'system' }
    catch { return 'system' }
  })
  useEffect(() => {
    const query = matchMedia('(prefers-color-scheme: dark)')
    const sync = () => {
      const value = preference === 'system' ? query.matches ? 'dark' : 'light' : preference
      document.documentElement.dataset.theme = value
      document.querySelector('meta[name="theme-color"]')?.setAttribute('content', value === 'dark' ? '#111719' : '#F2F4F5')
      setTheme(value)
    }
    sync()
    query.addEventListener('change', sync)
    return () => query.removeEventListener('change', sync)
  }, [preference])
  // Multiple controls (sidebar/mobile/settings) share the preference in this tab.
  useEffect(() => {
    const sync = () => {
      try {
        const value = localStorage.getItem('seryn-theme')
        setPreference(value === 'light' || value === 'dark' ? value : 'system')
      } catch { /* Keep a working in-memory preference when storage is unavailable. */ }
    }
    window.addEventListener('seryn-theme', sync)
    window.addEventListener('storage', sync)
    return () => { window.removeEventListener('seryn-theme', sync); window.removeEventListener('storage', sync) }
  }, [])
  function choose(value: Preference) {
    setPreference(value)
    try { localStorage.setItem('seryn-theme', value) } catch { /* Preference is optional. */ }
    window.dispatchEvent(new Event('seryn-theme'))
  }
  if (expanded) return <div className="theme-select"><label htmlFor={id}>Aparência</label><select id={id} value={preference} onChange={event => choose(event.target.value as Preference)}>
    <option value="system">Acompanhar o sistema</option><option value="light">Tema claro</option><option value="dark">Tema escuro</option>
  </select></div>
  return <button className="theme-toggle" onClick={() => choose(theme === 'dark' ? 'light' : 'dark')} aria-label={theme === 'dark' ? 'Ativar tema claro' : 'Ativar tema escuro'}>
    {theme === 'dark' ? <Sun aria-hidden="true" /> : <Moon aria-hidden="true" />}<span>{theme === 'dark' ? 'Tema claro' : 'Tema escuro'}</span>
  </button>
}
