import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'

/**
 * Termos de Uso + Política de Privacidade (auditoria pré-venda) — não existia NADA disso: zero
 * link, zero página, em todo o frontend. Cadastro coletava email/senha sem nenhum consentimento
 * formal de plataforma (diferente do consentimento LGPD do PACIENTE, que já existia de verdade em
 * `Patient` — este aqui é o consentimento de quem ASSINA o OdontoPlatform).
 *
 * Conteúdo redigido pelo time, NÃO é parecer jurídico — nota disso no rodapé de cada página, e
 * fica registrado aqui: antes de cobrar o primeiro cliente de verdade, revisar com advogado.
 */
function LegalLayout({ title, updatedAt, children }: { title: string; updatedAt: string; children: ReactNode }) {
  return (
    <div className="min-h-screen bg-surface">
      <header className="border-b border-border bg-surface-raised">
        <div className="mx-auto flex max-w-3xl items-center justify-between px-4 py-4">
          <Link to="/" className="text-sm font-semibold text-brand">
            OdontoPlatform
          </Link>
          <Link
            to="/"
            className="inline-flex items-center gap-1.5 text-sm text-ink-secondary hover:text-ink"
          >
            <ArrowLeft size={14} />
            Voltar
          </Link>
        </div>
      </header>

      <main className="mx-auto max-w-3xl px-4 py-10">
        <h1 className="text-2xl font-bold text-ink">{title}</h1>
        <p className="mt-1 text-sm text-ink-muted">Última atualização: {updatedAt}</p>

        <div className="mt-8 space-y-6 text-sm leading-relaxed text-ink-secondary [&_h2]:mt-8 [&_h2]:text-base [&_h2]:font-semibold [&_h2]:text-ink [&_p]:mt-2 [&_li]:mt-1 [&_ul]:list-disc [&_ul]:pl-5 [&_strong]:text-ink">
          {children}
        </div>

        <p className="mt-10 rounded-md border border-border bg-surface-sunken px-4 py-3 text-xs text-ink-muted">
          Este texto foi redigido pelo time do OdontoPlatform e não substitui aconselhamento
          jurídico. Se sua clínica tem exigências contratuais específicas, fale com nosso suporte
          antes de assinar.
        </p>
      </main>
    </div>
  )
}

export function TermsPage() {
  return (
    <LegalLayout title="Termos de Uso" updatedAt="19 de agosto de 2026">
      <section>
        <h2>1. O que é o OdontoPlatform</h2>
        <p>
          O OdontoPlatform é um software de gestão para clínicas odontológicas — agenda,
          cadastro de pacientes, financeiro e controle de estoque, organizados por empresa
          (organização) e, quando aplicável, por unidade. Ao criar uma conta, você contrata o
          acesso ao sistema nos termos descritos aqui.
        </p>
      </section>

      <section>
        <h2>2. Sua conta</h2>
        <p>
          Você é responsável por manter sua senha em sigilo e por toda atividade realizada com
          sua conta. A criação da primeira organização torna você <strong>Owner</strong> dela —
          papel com acesso total, incluindo convidar e remover outros usuários, editar dados da
          empresa e trocar de plano.
        </p>
      </section>

      <section>
        <h2>3. Dados de pacientes</h2>
        <p>
          Sua clínica (não o OdontoPlatform) é a controladora dos dados dos pacientes cadastrados
          no sistema, nos termos da Lei Geral de Proteção de Dados (LGPD). O OdontoPlatform atua
          como operador: armazena e processa esses dados exclusivamente para viabilizar o serviço
          contratado, sem uso próprio além disso. Cabe à sua clínica obter e registrar o
          consentimento de cada paciente — o campo de consentimento no cadastro existe justamente
          para isso.
        </p>
      </section>

      <section>
        <h2>4. Planos e cobrança</h2>
        <p>
          Os planos e valores vigentes estão descritos na página de preços. Nesta fase inicial, a
          cobrança é combinada diretamente com nossa equipe (o checkout automático dentro do
          produto ainda não está ativo) — escolher um plano libera o acesso, mas não gera cobrança
          automática. Isso muda quando o checkout automático entrar no ar; avisaremos antes.
        </p>
      </section>

      <section>
        <h2>5. Cancelamento</h2>
        <p>
          Você pode parar de usar o OdontoPlatform a qualquer momento. Entre em contato com nosso
          suporte para encerrar a assinatura e combinar a exportação dos dados da sua clínica, se
          desejar.
        </p>
      </section>

      <section>
        <h2>6. Disponibilidade e limitações</h2>
        <p>
          Fazemos o possível para manter o sistema disponível, mas não garantimos operação
          ininterrupta. Funcionalidades marcadas como "em breve" (como o checkout automático) não
          estão disponíveis ainda, mesmo que apareçam na interface.
        </p>
      </section>

      <section>
        <h2>7. Contato</h2>
        <p>Dúvidas sobre estes termos: fale com nosso suporte pelos canais informados no app.</p>
      </section>
    </LegalLayout>
  )
}

export function PrivacyPage() {
  return (
    <LegalLayout title="Política de Privacidade" updatedAt="19 de agosto de 2026">
      <section>
        <h2>1. Quem somos</h2>
        <p>
          O OdontoPlatform é a plataforma de software; sua clínica é quem contrata e opera o
          sistema. Esta política cobre os dois relacionamentos: o seu, como usuário da conta, e o
          da sua clínica com os pacientes dela.
        </p>
      </section>

      <section>
        <h2>2. Dados que coletamos de você (usuário)</h2>
        <ul>
          <li>Nome e email, no cadastro da sua conta.</li>
          <li>Senha, armazenada com hash (Argon2/BCrypt) — nunca em texto puro, nunca visível pra ninguém do nosso time.</li>
          <li>Dados da empresa (CNPJ, telefone, endereço), se você optar por preenchê-los — são opcionais.</li>
          <li>Registros técnicos de acesso (login, IP, data/hora), para segurança e suporte.</li>
        </ul>
      </section>

      <section>
        <h2>3. Dados de pacientes (sua clínica é a controladora)</h2>
        <p>
          Nome, CPF, contato, histórico e consentimento de pacientes são cadastrados pela sua
          clínica dentro do sistema. O OdontoPlatform armazena e processa esses dados só para
          operar o serviço — não os usa para nenhuma outra finalidade, não vende, não compartilha
          com terceiros fora do necessário para o funcionamento do produto (ex.: provedor de banco
          de dados, sempre sob contrato de confidencialidade).
        </p>
      </section>

      <section>
        <h2>4. Isolamento entre clínicas</h2>
        <p>
          Cada organização enxerga só os próprios dados — nunca de outra clínica cadastrada na
          plataforma. Esse isolamento é reforçado em todas as camadas do sistema, não é apenas uma
          regra de interface.
        </p>
      </section>

      <section>
        <h2>5. Seus direitos</h2>
        <p>
          Você pode pedir a exportação ou exclusão dos seus dados de conta, e sua clínica pode
          pedir a exportação ou exclusão dos dados de pacientes que cadastrou, a qualquer momento,
          pelo suporte.
        </p>
      </section>

      <section>
        <h2>6. Contato</h2>
        <p>Dúvidas sobre esta política: fale com nosso suporte pelos canais informados no app.</p>
      </section>
    </LegalLayout>
  )
}
