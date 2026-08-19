import { clsx, type ClassValue } from 'clsx'

/** Junta classes condicionalmente — mesmo utilitário `cn` de qualquer projeto shadcn/ui, sem o pacote tailwind-merge (não precisamos de dedupe de classe conflitante aqui). */
export function cn(...inputs: ClassValue[]) {
  return clsx(inputs)
}
