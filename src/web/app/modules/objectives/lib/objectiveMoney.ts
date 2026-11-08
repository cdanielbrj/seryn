export function parseObjectiveMoney(value: string) {
  let normalized = value.trim().replace(/\s/g, '')
  if (normalized.includes(',')) normalized = normalized.replace(/\./g, '').replace(',', '.')
  if (!/^\d+(\.\d{1,2})?$/.test(normalized)) return null
  const cents = Math.round(Number(normalized) * 100)
  return Number.isSafeInteger(cents) && cents > 0 ? cents : null
}
export function maskObjectiveMoney(value: string) {
  const digits = value.replace(/\D/g, '').replace(/^0+(?=\d)/, '')
  if (!digits) return ''
  const padded = digits.padStart(3, '0')
  return `${padded.slice(0, -2).replace(/\B(?=(\d{3})+(?!\d))/g, '.')},${padded.slice(-2)}`
}
