// Runs before paint; this preference contains no account or financial data.
try {
  const saved = localStorage.getItem('seryn-theme')
  document.documentElement.dataset.theme = saved === 'light' || saved === 'dark'
    ? saved : matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
} catch { document.documentElement.dataset.theme = 'light' }
