import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Building2, ClipboardList, CreditCard, MapPin, Pencil, Stethoscope, DoorOpen } from 'lucide-react'
import { getOrganization, listBranches, updateBranch, updateOrganization } from './api'
import { listPlans, selectPlan } from '../subscriptions/api'
import { PlanCards } from '../subscriptions/PlanCards'
import {
  createProcedimento,
  createProfissional,
  createSala,
  listProcedimentos,
  listProfissionais,
  listSalas,
  updateProcedimento,
  updateProfissional,
  updateSala,
} from '../scheduling/api'
import { useMe } from '../auth/useMe'
import { getApiErrorMessage } from '../../lib/query-client'
import { toneClasses } from '../../lib/status-tone'
import { cn } from '../../lib/cn'
import { Card, CardBody, CardHeader, CardTitle } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'
import { Select } from '../../components/ui/Select'
import { Modal } from '../../components/ui/Modal'
import type { BranchDetails } from '../../types/organizations'
import type { PlanTier } from '../../types/subscriptions'
import type { Procedimento, Profissional, Sala, TipoContrato } from '../../types/scheduling'

const TIPO_CONTRATO_LABELS: Record<TipoContrato, string> = { Clt: 'CLT', Pj: 'PJ', Autonomo: 'Autônomo' }

const orgSchema = z.object({
  nome: z.string().min(1, 'Nome é obrigatório').max(150, 'Nome não pode ter mais de 150 caracteres'),
  cnpj: z.string().optional(),
  telefone: z.string().optional(),
  endereco: z.string().optional(),
})
type OrgFormValues = z.infer<typeof orgSchema>

const branchSchema = z.object({
  nome: z.string().min(1, 'Nome é obrigatório').max(200, 'Nome não pode ter mais de 200 caracteres'),
  endereco: z.string().optional(),
  telefone: z.string().optional(),
})
type BranchFormValues = z.infer<typeof branchSchema>

const profissionalSchema = z.object({
  nome: z.string().min(1, 'Nome é obrigatório').max(200, 'Nome não pode ter mais de 200 caracteres'),
  especialidade: z.string().min(1, 'Especialidade é obrigatória').max(200, 'Especialidade não pode ter mais de 200 caracteres'),
  tipoContrato: z.enum(['Clt', 'Pj', 'Autonomo']),
  percentualComissaoDefault: z.string().optional(),
  email: z.string().email('Email inválido').optional().or(z.literal('')),
})
type ProfissionalFormValues = z.infer<typeof profissionalSchema>

const salaSchema = z.object({
  nome: z.string().min(1, 'Nome é obrigatório').max(200, 'Nome não pode ter mais de 200 caracteres'),
  capacidadeMaxima: z.string().optional(),
})
type SalaFormValues = z.infer<typeof salaSchema>

const procedimentoSchema = z.object({
  nome: z.string().min(1, 'Nome é obrigatório').max(200, 'Nome não pode ter mais de 200 caracteres'),
  valorPadrao: z.string().optional(),
  duracaoPadraoMinutos: z.string().optional(),
})
type ProcedimentoFormValues = z.infer<typeof procedimentoSchema>

/**
 * Seção "Empresa" — edita Nome/Cnpj/Telefone/Endereco da organização ativa. Era "fora de escopo"
 * da task 039 ("configurar depois" era aspiracional, sem tela) — esta é a tela dedicada, item
 * futuro anunciado lá.
 */
function OrganizationSection({ organizationId }: { organizationId: string }) {
  const queryClient = useQueryClient()
  const [serverError, setServerError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  const { data: org, isLoading } = useQuery({
    queryKey: ['organizations', organizationId],
    queryFn: () => getOrganization(organizationId),
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<OrgFormValues>({ resolver: zodResolver(orgSchema) })

  // Preenche o form assim que os dados chegam — reset (não defaultValues) porque a query só
  // resolve depois do primeiro render.
  useEffect(() => {
    if (org) reset({ nome: org.nome, cnpj: org.cnpj ?? '', telefone: org.telefone ?? '', endereco: org.endereco ?? '' })
  }, [org, reset])

  const mutation = useMutation({
    mutationFn: (values: OrgFormValues) => updateOrganization(organizationId, values),
    onSuccess: (updated) => {
      queryClient.setQueryData(['organizations', organizationId], updated)
      queryClient.invalidateQueries({ queryKey: ['me'] }) // nome aparece no OrgSwitcher
      setSaved(true)
      setTimeout(() => setSaved(false), 2000)
    },
    onError: (error) => setServerError(getApiErrorMessage(error)),
  })

  const onSubmit = (values: OrgFormValues) => {
    setServerError(null)
    mutation.mutate(values)
  }

  return (
    <Card>
      <CardHeader className="flex items-center gap-2">
        <Building2 size={18} className="text-brand" />
        <CardTitle>Empresa</CardTitle>
      </CardHeader>
      <CardBody>
        {isLoading ? (
          <p className="text-sm text-ink-muted">Carregando…</p>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} className="grid gap-4 sm:grid-cols-2" noValidate>
            <div className="sm:col-span-2">
              <Label htmlFor="nomeOrganizacao">Nome da empresa</Label>
              <Input id="nomeOrganizacao" error={errors.nome?.message} {...register('nome')} />
            </div>
            <div>
              <Label htmlFor="cnpjOrganizacao">CNPJ</Label>
              <Input id="cnpjOrganizacao" placeholder="00.000.000/0000-00" {...register('cnpj')} />
            </div>
            <div>
              <Label htmlFor="telefoneOrganizacao">Telefone</Label>
              <Input id="telefoneOrganizacao" placeholder="11999999999" {...register('telefone')} />
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="enderecoOrganizacao">Endereço</Label>
              <Input id="enderecoOrganizacao" placeholder="Rua, número, cidade" {...register('endereco')} />
            </div>

            {serverError && <p className="text-sm text-danger sm:col-span-2">{serverError}</p>}

            <div className="flex items-center gap-3 sm:col-span-2">
              <Button type="submit" disabled={mutation.isPending || !isDirty}>
                {mutation.isPending ? 'Salvando…' : 'Salvar'}
              </Button>
              {saved && <span className="text-sm text-success">Salvo.</span>}
            </div>
          </form>
        )}
      </CardBody>
    </Card>
  )
}

/** Modal de edição de uma unidade — aberto pelo botão "Editar" da lista em `BranchesSection`. */
function EditBranchModal({ branch, onClose }: { branch: BranchDetails; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<BranchFormValues>({
    resolver: zodResolver(branchSchema),
    defaultValues: { nome: branch.nome, endereco: branch.endereco ?? '', telefone: branch.telefone ?? '' },
  })

  const mutation = useMutation({
    mutationFn: (values: BranchFormValues) => updateBranch(branch.id, values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['branches'] })
      onClose()
    },
    onError: (error) => setServerError(getApiErrorMessage(error)),
  })

  const onSubmit = (values: BranchFormValues) => {
    setServerError(null)
    mutation.mutate(values)
  }

  return (
    <Modal open onClose={onClose} title={`Editar ${branch.nome}`}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        <div>
          <Label htmlFor="nomeFilialEdit">Nome da unidade</Label>
          <Input id="nomeFilialEdit" error={errors.nome?.message} {...register('nome')} />
        </div>
        <div>
          <Label htmlFor="enderecoFilialEdit">Endereço</Label>
          <Input id="enderecoFilialEdit" {...register('endereco')} />
        </div>
        <div>
          <Label htmlFor="telefoneFilialEdit">Telefone</Label>
          <Input id="telefoneFilialEdit" {...register('telefone')} />
        </div>

        {serverError && <p className="text-sm text-danger">{serverError}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Salvando…' : 'Salvar'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}

/** Modal de edição de um profissional — aberto pelo botão "Editar" da lista em `ResourcesSection`.
 * Não reenvia convite (Email aqui é só dado de contato/pareamento futuro, ver task 042). */
function EditProfissionalModal({ profissional, onClose }: { profissional: Profissional; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ProfissionalFormValues>({
    resolver: zodResolver(profissionalSchema),
    defaultValues: {
      nome: profissional.nome,
      especialidade: profissional.especialidade,
      tipoContrato: profissional.tipoContrato,
      percentualComissaoDefault: profissional.percentualComissaoDefault?.toString() ?? '',
      email: profissional.email ?? '',
    },
  })

  const mutation = useMutation({
    mutationFn: (values: ProfissionalFormValues) =>
      updateProfissional(profissional.id, {
        nome: values.nome,
        especialidade: values.especialidade,
        tipoContrato: values.tipoContrato,
        percentualComissaoDefault: values.percentualComissaoDefault ? Number(values.percentualComissaoDefault) : undefined,
        email: values.email || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profissionais'] })
      onClose()
    },
    onError: (error) => setServerError(getApiErrorMessage(error)),
  })

  return (
    <Modal open onClose={onClose} title={`Editar ${profissional.nome}`}>
      <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="space-y-4" noValidate>
        <div>
          <Label htmlFor="nomeProfissionalEdit">Nome completo</Label>
          <Input id="nomeProfissionalEdit" error={errors.nome?.message} {...register('nome')} />
        </div>
        <div>
          <Label htmlFor="especialidadeProfissionalEdit">Especialidade</Label>
          <Input id="especialidadeProfissionalEdit" error={errors.especialidade?.message} {...register('especialidade')} />
        </div>
        <div className="grid grid-cols-2 gap-2">
          <div>
            <Label htmlFor="tipoContratoProfissionalEdit">Tipo de contrato</Label>
            <Select id="tipoContratoProfissionalEdit" {...register('tipoContrato')}>
              {(Object.keys(TIPO_CONTRATO_LABELS) as TipoContrato[]).map((tc) => (
                <option key={tc} value={tc}>
                  {TIPO_CONTRATO_LABELS[tc]}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <Label htmlFor="comissaoProfissionalEdit">Comissão %</Label>
            <Input id="comissaoProfissionalEdit" type="number" min={0} max={100} step="0.1" {...register('percentualComissaoDefault')} />
          </div>
        </div>
        <div>
          <Label htmlFor="emailProfissionalEdit">Email</Label>
          <Input id="emailProfissionalEdit" type="email" error={errors.email?.message} {...register('email')} />
        </div>

        {serverError && <p className="text-sm text-danger">{serverError}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Salvando…' : 'Salvar'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}

/** Modal de edição de uma sala — mesmo padrão de `EditProfissionalModal`. */
function EditSalaModal({ sala, onClose }: { sala: Sala; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<SalaFormValues>({
    resolver: zodResolver(salaSchema),
    defaultValues: { nome: sala.nome, capacidadeMaxima: sala.capacidadeMaxima?.toString() ?? '' },
  })

  const mutation = useMutation({
    mutationFn: (values: SalaFormValues) =>
      updateSala(sala.id, {
        nome: values.nome,
        capacidadeMaxima: values.capacidadeMaxima ? Number(values.capacidadeMaxima) : undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['salas'] })
      onClose()
    },
    onError: (error) => setServerError(getApiErrorMessage(error)),
  })

  return (
    <Modal open onClose={onClose} title={`Editar ${sala.nome}`}>
      <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="space-y-4" noValidate>
        <div>
          <Label htmlFor="nomeSalaEdit">Nome da sala</Label>
          <Input id="nomeSalaEdit" error={errors.nome?.message} {...register('nome')} />
        </div>
        <div>
          <Label htmlFor="capacidadeSalaEdit">Capacidade máxima</Label>
          <Input id="capacidadeSalaEdit" type="number" min={1} {...register('capacidadeMaxima')} />
        </div>

        {serverError && <p className="text-sm text-danger">{serverError}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Salvando…' : 'Salvar'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}

/** Modal de edição de um procedimento — mesmo padrão de `EditSalaModal` (task 044). */
function EditProcedimentoModal({ procedimento, onClose }: { procedimento: Procedimento; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ProcedimentoFormValues>({
    resolver: zodResolver(procedimentoSchema),
    defaultValues: {
      nome: procedimento.nome,
      valorPadrao: procedimento.valorPadrao?.toString() ?? '',
      duracaoPadraoMinutos: procedimento.duracaoPadraoMinutos?.toString() ?? '',
    },
  })

  const mutation = useMutation({
    mutationFn: (values: ProcedimentoFormValues) =>
      updateProcedimento(procedimento.id, {
        nome: values.nome,
        valorPadrao: values.valorPadrao ? Number(values.valorPadrao) : undefined,
        duracaoPadraoMinutos: values.duracaoPadraoMinutos ? Number(values.duracaoPadraoMinutos) : undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['procedimentos'] })
      onClose()
    },
    onError: (error) => setServerError(getApiErrorMessage(error)),
  })

  return (
    <Modal open onClose={onClose} title={`Editar ${procedimento.nome}`}>
      <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="space-y-4" noValidate>
        <div>
          <Label htmlFor="nomeProcedimentoEdit">Nome do procedimento</Label>
          <Input id="nomeProcedimentoEdit" error={errors.nome?.message} {...register('nome')} />
        </div>
        <div className="grid grid-cols-2 gap-2">
          <div>
            <Label htmlFor="valorProcedimentoEdit">Valor padrão (R$)</Label>
            <Input id="valorProcedimentoEdit" type="number" min={0} step="0.01" {...register('valorPadrao')} />
          </div>
          <div>
            <Label htmlFor="duracaoProcedimentoEdit">Duração padrão (min)</Label>
            <Input id="duracaoProcedimentoEdit" type="number" min={1} {...register('duracaoPadraoMinutos')} />
          </div>
        </div>

        {serverError && <p className="text-sm text-danger">{serverError}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Salvando…' : 'Salvar'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}

/** Seção "Unidades" — lista as branches da organização, edição via modal (uma de cada vez). */
function BranchesSection() {
  const [editing, setEditing] = useState<BranchDetails | null>(null)

  const { data: branches, isLoading } = useQuery({ queryKey: ['branches'], queryFn: listBranches })

  return (
    <Card>
      <CardHeader className="flex items-center gap-2">
        <MapPin size={18} className="text-brand" />
        <CardTitle>Unidades</CardTitle>
      </CardHeader>
      <CardBody>
        {isLoading ? (
          <p className="text-sm text-ink-muted">Carregando…</p>
        ) : !branches || branches.length === 0 ? (
          <p className="text-sm text-ink-muted">Nenhuma unidade cadastrada.</p>
        ) : (
          <ul className="divide-y divide-border">
            {branches.map((branch) => (
              <li key={branch.id} className="flex items-center justify-between gap-4 py-3 first:pt-0 last:pb-0">
                <div>
                  <p className="text-sm font-medium text-ink">{branch.nome}</p>
                  <p className="text-xs text-ink-muted">
                    {[branch.endereco, branch.telefone].filter(Boolean).join(' · ') || 'Sem endereço/telefone cadastrado'}
                  </p>
                </div>
                <Button variant="ghost" onClick={() => setEditing(branch)} aria-label={`Editar ${branch.nome}`}>
                  <Pencil size={16} />
                </Button>
              </li>
            ))}
          </ul>
        )}
      </CardBody>

      {editing && <EditBranchModal branch={editing} onClose={() => setEditing(null)} />}
    </Card>
  )
}

/**
 * Seção "Profissionais e Salas" — auditoria pré-venda (task 041): `POST /api/profissionais` e
 * `POST /api/salas` existiam no backend sem NENHUMA tela pra chamar. Sem isso, uma organização
 * nova nunca populava os selects de "Novo agendamento" e a Agenda — módulo #1 anunciado na
 * landing — ficava inutilizável no primeiro uso real. Edição via modal (task de melhoria
 * seguinte) — mesmo padrão de `EditBranchModal`; sem remoção/desativação na UI ainda (endpoint
 * existe no backend, mas desativar sem um jeito de reativar pela tela seria uma armadilha).
 */
function ResourcesSection() {
  const queryClient = useQueryClient()
  const [editingProfissional, setEditingProfissional] = useState<Profissional | null>(null)
  const [editingSala, setEditingSala] = useState<Sala | null>(null)
  const [nomeProfissional, setNomeProfissional] = useState('')
  const [especialidade, setEspecialidade] = useState('')
  const [tipoContrato, setTipoContrato] = useState<TipoContrato>('Clt')
  const [percentualComissao, setPercentualComissao] = useState('')
  const [emailProfissional, setEmailProfissional] = useState('')
  const [feedbackProfissional, setFeedbackProfissional] = useState<string | null>(null)
  const [nomeSala, setNomeSala] = useState('')
  const [editingProcedimento, setEditingProcedimento] = useState<Procedimento | null>(null)
  const [nomeProcedimento, setNomeProcedimento] = useState('')
  const [valorProcedimento, setValorProcedimento] = useState('')
  const [duracaoProcedimento, setDuracaoProcedimento] = useState('')

  const { data: profissionais } = useQuery({ queryKey: ['profissionais'], queryFn: listProfissionais })
  const { data: salas } = useQuery({ queryKey: ['salas'], queryFn: listSalas })
  const { data: procedimentos } = useQuery({ queryKey: ['procedimentos'], queryFn: listProcedimentos })

  const profissionalMutation = useMutation({
    mutationFn: () =>
      createProfissional({
        nome: nomeProfissional,
        especialidade,
        tipoContrato,
        percentualComissaoDefault: percentualComissao ? Number(percentualComissao) : undefined,
        email: emailProfissional || undefined,
      }),
    onSuccess: ({ conviteEnviado, conviteAviso }) => {
      queryClient.invalidateQueries({ queryKey: ['profissionais'] })
      setFeedbackProfissional(
        conviteEnviado
          ? `Profissional cadastrado — convite enviado para ${emailProfissional}.`
          : conviteAviso
            ? `Profissional cadastrado, mas o convite não foi enviado: ${conviteAviso}`
            : 'Profissional cadastrado.',
      )
      setNomeProfissional('')
      setEspecialidade('')
      setTipoContrato('Clt')
      setPercentualComissao('')
      setEmailProfissional('')
    },
  })

  const salaMutation = useMutation({
    mutationFn: () => createSala({ nome: nomeSala }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['salas'] })
      setNomeSala('')
    },
  })

  const procedimentoMutation = useMutation({
    mutationFn: () =>
      createProcedimento({
        nome: nomeProcedimento,
        valorPadrao: valorProcedimento ? Number(valorProcedimento) : undefined,
        duracaoPadraoMinutos: duracaoProcedimento ? Number(duracaoProcedimento) : undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['procedimentos'] })
      setNomeProcedimento('')
      setValorProcedimento('')
      setDuracaoProcedimento('')
    },
  })

  return (
    <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-3">
      <Card>
        <CardHeader className="flex items-center gap-2">
          <Stethoscope size={18} className="text-brand" />
          <CardTitle>Profissionais</CardTitle>
        </CardHeader>
        <CardBody className="space-y-3">
          <form
            className="space-y-2"
            onSubmit={(e) => {
              e.preventDefault()
              setFeedbackProfissional(null)
              if (nomeProfissional.trim() && especialidade.trim()) profissionalMutation.mutate()
            }}
          >
            <Input
              placeholder="Nome completo (ex.: Dra. Ana Souza)"
              value={nomeProfissional}
              onChange={(e) => setNomeProfissional(e.target.value)}
            />
            <Input placeholder="Especialidade (ex.: Ortodontia)" value={especialidade} onChange={(e) => setEspecialidade(e.target.value)} />
            <div className="grid grid-cols-2 gap-2">
              <div>
                <Label htmlFor="tipoContratoProfissional" className="sr-only">
                  Tipo de contrato
                </Label>
                <Select
                  id="tipoContratoProfissional"
                  value={tipoContrato}
                  onChange={(e) => setTipoContrato(e.target.value as TipoContrato)}
                >
                  {(Object.keys(TIPO_CONTRATO_LABELS) as TipoContrato[]).map((tc) => (
                    <option key={tc} value={tc}>
                      {TIPO_CONTRATO_LABELS[tc]}
                    </option>
                  ))}
                </Select>
              </div>
              <Input
                type="number"
                min={0}
                max={100}
                step="0.1"
                placeholder="Comissão % (opcional)"
                value={percentualComissao}
                onChange={(e) => setPercentualComissao(e.target.value)}
              />
            </div>
            <Input
              type="email"
              placeholder="Email (opcional) — envia convite de acesso"
              value={emailProfissional}
              onChange={(e) => setEmailProfissional(e.target.value)}
            />
            <Button
              type="submit"
              variant="secondary"
              className="w-full"
              disabled={profissionalMutation.isPending || !nomeProfissional.trim() || !especialidade.trim()}
            >
              {profissionalMutation.isPending ? 'Adicionando…' : '+ Adicionar profissional'}
            </Button>
          </form>

          {profissionalMutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(profissionalMutation.error)}</p>}
          {feedbackProfissional && <p className="text-sm text-success">{feedbackProfissional}</p>}

          <ul className="divide-y divide-border text-sm">
            {profissionais?.map((p) => (
              <li key={p.id} className="flex items-center justify-between gap-2 py-1.5">
                <div>
                  <p className="text-ink">{p.nome}</p>
                  <p className="text-xs text-ink-muted">
                    {p.especialidade} · {TIPO_CONTRATO_LABELS[p.tipoContrato]}
                    {p.percentualComissaoDefault ? ` · ${p.percentualComissaoDefault}% comissão` : ''}
                  </p>
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  {/* Status de acesso — só faz sentido pra quem tem email cadastrado (task 042). */}
                  {p.userId ? (
                    <span className={cn('rounded-full px-2 py-0.5 text-xs font-medium', toneClasses.success)}>Acesso ativo</span>
                  ) : p.email ? (
                    <span className={cn('rounded-full px-2 py-0.5 text-xs font-medium', toneClasses.info)}>Aguardando registro</span>
                  ) : null}
                  <Button variant="ghost" onClick={() => setEditingProfissional(p)} aria-label={`Editar ${p.nome}`}>
                    <Pencil size={16} />
                  </Button>
                </div>
              </li>
            ))}
            {profissionais?.length === 0 && <li className="py-1.5 text-ink-muted">Nenhum profissional cadastrado.</li>}
          </ul>
        </CardBody>
      </Card>

      <Card>
        <CardHeader className="flex items-center gap-2">
          <DoorOpen size={18} className="text-brand" />
          <CardTitle>Salas</CardTitle>
        </CardHeader>
        <CardBody className="space-y-3">
          <form
            className="flex gap-2"
            onSubmit={(e) => {
              e.preventDefault()
              if (nomeSala.trim()) salaMutation.mutate()
            }}
          >
            <Input placeholder="Nome da sala" value={nomeSala} onChange={(e) => setNomeSala(e.target.value)} />
            <Button type="submit" variant="secondary" disabled={salaMutation.isPending || !nomeSala.trim()}>
              + Adicionar
            </Button>
          </form>

          {salaMutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(salaMutation.error)}</p>}

          <ul className="divide-y divide-border text-sm">
            {salas?.map((s) => (
              <li key={s.id} className="flex items-center justify-between gap-2 py-1.5">
                <span className="text-ink">
                  {s.nome}
                  {s.capacidadeMaxima ? <span className="text-xs text-ink-muted"> · capacidade {s.capacidadeMaxima}</span> : null}
                </span>
                <Button variant="ghost" onClick={() => setEditingSala(s)} aria-label={`Editar ${s.nome}`}>
                  <Pencil size={16} />
                </Button>
              </li>
            ))}
            {salas?.length === 0 && <li className="py-1.5 text-ink-muted">Nenhuma sala cadastrada.</li>}
          </ul>
        </CardBody>
      </Card>

      <Card>
        <CardHeader className="flex items-center gap-2">
          <ClipboardList size={18} className="text-brand" />
          <CardTitle>Procedimentos</CardTitle>
        </CardHeader>
        <CardBody className="space-y-3">
          <form
            className="space-y-2"
            onSubmit={(e) => {
              e.preventDefault()
              if (nomeProcedimento.trim()) procedimentoMutation.mutate()
            }}
          >
            <Input placeholder="Nome (ex.: Limpeza, Extração)" value={nomeProcedimento} onChange={(e) => setNomeProcedimento(e.target.value)} />
            <div className="grid grid-cols-2 gap-2">
              <Input
                type="number"
                min={0}
                step="0.01"
                placeholder="Valor padrão (opcional)"
                value={valorProcedimento}
                onChange={(e) => setValorProcedimento(e.target.value)}
              />
              <Input
                type="number"
                min={1}
                placeholder="Duração min. (opcional)"
                value={duracaoProcedimento}
                onChange={(e) => setDuracaoProcedimento(e.target.value)}
              />
            </div>
            <Button
              type="submit"
              variant="secondary"
              className="w-full"
              disabled={procedimentoMutation.isPending || !nomeProcedimento.trim()}
            >
              {procedimentoMutation.isPending ? 'Adicionando…' : '+ Adicionar procedimento'}
            </Button>
          </form>

          {procedimentoMutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(procedimentoMutation.error)}</p>}

          <ul className="divide-y divide-border text-sm">
            {procedimentos?.map((p) => (
              <li key={p.id} className="flex items-center justify-between gap-2 py-1.5">
                <div>
                  <p className="text-ink">{p.nome}</p>
                  <p className="text-xs text-ink-muted">
                    {[p.valorPadrao ? `R$ ${p.valorPadrao.toFixed(2)}` : null, p.duracaoPadraoMinutos ? `${p.duracaoPadraoMinutos} min` : null]
                      .filter(Boolean)
                      .join(' · ') || 'Sem valor/duração padrão'}
                  </p>
                </div>
                <Button variant="ghost" onClick={() => setEditingProcedimento(p)} aria-label={`Editar ${p.nome}`}>
                  <Pencil size={16} />
                </Button>
              </li>
            ))}
            {procedimentos?.length === 0 && <li className="py-1.5 text-ink-muted">Nenhum procedimento cadastrado.</li>}
          </ul>
        </CardBody>
      </Card>

      {editingProfissional && <EditProfissionalModal profissional={editingProfissional} onClose={() => setEditingProfissional(null)} />}
      {editingSala && <EditSalaModal sala={editingSala} onClose={() => setEditingSala(null)} />}
      {editingProcedimento && <EditProcedimentoModal procedimento={editingProcedimento} onClose={() => setEditingProcedimento(null)} />}
    </div>
  )
}

/** Seção "Plano" — mesmos cards do onboarding (`PlanCards`), agora com `activeTier` pra distinguir
 * "plano atual" de "trocar". Troca de plano não valida limite de filial contra o tier novo (mesma
 * dívida técnica assumida na task 039 — `SelectPlanCommand` não confere). */
function PlanSection({ activeTier }: { activeTier: PlanTier | null }) {
  const queryClient = useQueryClient()
  const [serverError, setServerError] = useState<string | null>(null)

  const { data: plans, isLoading, isError } = useQuery({ queryKey: ['subscriptions', 'plans'], queryFn: listPlans })

  const mutation = useMutation({
    mutationFn: selectPlan,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['me'] }), // activePlanTier vem do GetMe
    onError: (error) => setServerError(getApiErrorMessage(error)),
  })

  return (
    <Card>
      <CardHeader className="flex items-center gap-2">
        <CreditCard size={18} className="text-brand" />
        <CardTitle>Plano</CardTitle>
      </CardHeader>
      <CardBody>
        {isLoading ? (
          <p className="text-sm text-ink-muted">Carregando…</p>
        ) : isError || !plans ? (
          <p className="text-sm text-danger">Não foi possível carregar os planos.</p>
        ) : (
          <>
            <PlanCards
              plans={plans}
              activeTier={activeTier}
              pendingTier={mutation.isPending ? mutation.variables : undefined}
              onSelect={(tier) => {
                setServerError(null)
                mutation.mutate(tier)
              }}
            />
            {serverError && <p className="mt-3 text-sm text-danger">{serverError}</p>}
          </>
        )}
      </CardBody>
    </Card>
  )
}

/**
 * Tela de configurações (item futuro anunciado na task 039) — Empresa/Unidades/Plano num só
 * lugar. Rota `/configuracoes`, Owner/Admin only (mesma role do backend nos 3 endpoints usados
 * aqui: `OrganizationsController`/`BranchesController`/`SubscriptionsController`).
 */
export function SettingsPage() {
  const { data: me } = useMe()

  if (!me?.activeOrganizationId) return null // RequireOrganization já cobre esse caso antes daqui

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-xl font-semibold text-ink">Configurações</h1>
        <p className="text-sm text-ink-muted">Dados da empresa, unidades e plano da assinatura.</p>
      </div>

      <OrganizationSection organizationId={me.activeOrganizationId} />
      <BranchesSection />
      <ResourcesSection />
      <PlanSection activeTier={(me.activePlanTier as PlanTier | null) ?? null} />
    </div>
  )
}
