import { Card, CardBody } from '../ui/Card'

/** Placeholder pras telas que ainda não foram construídas — a API já existe (ver docs/tasks), só falta o frontend. */
export function ComingSoonPage({ title }: { title: string }) {
  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold text-ink">{title}</h1>
      <Card>
        <CardBody>
          <p className="text-sm text-ink-muted">Tela em construção — a API deste módulo já está pronta no backend.</p>
        </CardBody>
      </Card>
    </div>
  )
}
