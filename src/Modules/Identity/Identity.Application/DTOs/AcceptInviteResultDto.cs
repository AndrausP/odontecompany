using Identity.Domain.Enums;

namespace Identity.Application.DTOs;

/// <summary>
/// Resultado da aceitação de um convite (task 016) — não emite token novo aqui de propósito
/// (o comando <c>SwitchOrganization</c>, já existente desde a task 014, já cobre "trocar a
/// organization ativa sem refazer login"; reemitir token nesta rota duplicaria essa
/// responsabilidade). O frontend chama <c>POST /api/auth/switch-organization</c> em seguida se
/// quiser um token escopado à organization recém-afiliada.
/// </summary>
/// <summary><see cref="Email"/> adicionado na task 042 — o controller usa pra disparar
/// `LinkProfissionalUserCommand` (Scheduling) sem precisar de outra consulta.</summary>
public sealed record AcceptInviteResultDto(Guid OrganizationId, Role Role, string Email);
