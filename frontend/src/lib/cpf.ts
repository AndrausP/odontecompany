/** Só os dígitos — mesmo formato que o backend persiste (Patients.Domain.ValueObjects.Cpf). */
export function onlyDigits(value: string): string {
  return value.replace(/\D/g, '')
}

/**
 * Validação mod-11 espelhando Patients.Domain.ValueObjects.Cpf — client-side é só UX (feedback
 * rápido antes de bater na API), a validação que importa de verdade continua no backend.
 */
export function isValidCpf(value: string): boolean {
  const digits = onlyDigits(value)
  if (digits.length !== 11) return false
  if (new Set(digits).size === 1) return false // todos os dígitos iguais (ex: 11111111111)

  const calcDigit = (base: string, weightStart: number) => {
    let sum = 0
    for (let i = 0; i < base.length; i++) {
      sum += Number(base[i]) * (weightStart - i)
    }
    const remainder = (sum * 10) % 11
    return remainder === 10 ? 0 : remainder
  }

  const dv1 = calcDigit(digits.slice(0, 9), 10)
  const dv2 = calcDigit(digits.slice(0, 10), 11)

  return dv1 === Number(digits[9]) && dv2 === Number(digits[10])
}

export function formatCpf(value: string): string {
  const digits = onlyDigits(value).slice(0, 11)
  return digits
    .replace(/(\d{3})(\d)/, '$1.$2')
    .replace(/(\d{3})(\d)/, '$1.$2')
    .replace(/(\d{3})(\d{1,2})$/, '$1-$2')
}
