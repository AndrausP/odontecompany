using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Application.Common.Behaviors;
using FluentValidation;
using Identity.Application.Commands.Login;
using Identity.Infrastructure;
using Identity.Infrastructure.Security;
using Infrastructure.Common.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Billing.Application.Commands.CreateFaturaParticular;
using Billing.Infrastructure;
using OdontoPlatform.Api.Extensions;
using Patients.Application.Commands.CreatePatient;
using Patients.Infrastructure;
using Records.Application.Commands.CreateProntuario;
using Records.Infrastructure;
using Reporting.Application.Queries.GetDashboardResumo;
using Scheduling.Application.Commands.CreateAgendamento;
using Scheduling.Infrastructure;
using Estoque.Application.Commands.CreateItemEstoque;
using Estoque.Infrastructure;
using Tenancy.Application.Commands.CreateBranch;
using Tenancy.Infrastructure;
using Subscriptions.Application.Commands.SelectPlan;
using Subscriptions.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── MVC + JSON ────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "OdontoPlatform API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe: Bearer {seu token}"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// ── CQRS: MediatR + FluentValidation (pipeline behavior, sem exception pra erro de negócio) ─
// ValidationBehavior é único e compartilhado (Application.Common) — registrado UMA vez cobre
// todo módulo (Identity, Patients, ...) que passa pelo mesmo pipeline MediatR. Cada módulo
// tendo sua própria cópia do behavior faria o MediatR validar a mesma requisição N vezes (um
// open generic registrado por módulo), daí a extração pro Shared.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreatePatientCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateAgendamentoCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateProntuarioCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateFaturaParticularCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GetDashboardResumoQuery).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateBranchCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateItemEstoqueCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(SelectPlanCommand).Assembly);
});
builder.Services.AddValidatorsFromAssembly(typeof(LoginCommand).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreatePatientCommand).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateAgendamentoCommand).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateProntuarioCommand).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateFaturaParticularCommand).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(GetDashboardResumoQuery).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateBranchCommand).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateItemEstoqueCommand).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(SelectPlanCommand).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ── Módulo Identity (EF Core, Argon2id, emissor JWT, repositórios) ──────────
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// ── Módulo Patients (EF Core, repositórios) ─────────────────────────────────
// Depende do IOrganizationContext já registrado por AddIdentityInfrastructure acima — ver comentário
// em Patients.Infrastructure.DependencyInjection.
builder.Services.AddPatientsInfrastructure(builder.Configuration);

// ── Módulo Scheduling (EF Core, Redis lock/cache, repositórios) ─────────────
// Depende de IOrganizationContext (Identity) e IPatientLookup (Patients) já registrados acima.
builder.Services.AddSchedulingInfrastructure(builder.Configuration);

// ── Módulo Records (EF Core, criptografia AES-256-GCM, storage de anexo) ────
// Depende de IOrganizationContext (Identity) e IPatientLookup (Patients) já registrados acima.
builder.Services.AddRecordsInfrastructure(builder.Configuration);

// ── Módulo Billing (EF Core, ACL de convênio, tradutor de evento do Scheduling) ──
// Depende de IOrganizationContext (Identity) e IPatientLookup (Patients) já registrados acima.
builder.Services.AddBillingInfrastructure(builder.Configuration);

// ── Módulo Tenancy (EF Core, hierarquia Organization→Branch — Fase 5) ────────────
builder.Services.AddTenancyInfrastructure(builder.Configuration);

// ── Módulo Estoque (EF Core, materiais/insumos — Fase 5) ─────────────────────
builder.Services.AddEstoqueInfrastructure(builder.Configuration);

// ── Módulo Subscriptions (EF Core, plano da PLATAFORMA + esqueleto Stripe — sprint-11) ──────
// Não confundir com Billing (faturamento do PACIENTE, domínio irmão separado).
builder.Services.AddSubscriptionsInfrastructure(builder.Configuration);

// ── Autenticação JWT ──────────────────────────────────────────────────────
// Fail-fast: se a seção "Jwt" sumir do config, ou se a SigningKey ainda for o valor default de
// dev, recusa subir. Preferível a subir silenciosamente assinando token de produção com chave
// de desenvolvimento hardcoded.
var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
if (!jwtSection.Exists())
    throw new InvalidOperationException(
        "Configuração 'Jwt' ausente. Configure Jwt:Issuer/Audience/SigningKey (appsettings ou secrets) antes de subir a API.");

var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

if (jwtSettings.SigningKey == new JwtSettings().SigningKey)
    throw new InvalidOperationException(
        "Jwt:SigningKey está usando o valor padrão de desenvolvimento. Configure uma chave real via secrets/variável de ambiente antes de subir fora de dev local.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sem isso, o .NET 8 remapeia "sub" pro claim type XML antigo
        // (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier) e
        // CurrentUserAccessor.UserId (que procura por JwtRegisteredClaimNames.Sub) sempre
        // volta null pra qualquer request autenticado.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            RoleClaimType = "role",
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Policy fail-closed (task 014): endpoint marcado com ela recusa (403) qualquer token SEM a
// claim organization_id — inclusive token de usuário sem organization ativa (zero memberships).
// Endpoints que TÊM que funcionar sem org (GET /api/me, POST /api/organizations, convites — ver
// tasks 015/016/017) não aplicam esta policy.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireActiveOrganization", policy => policy.RequireClaim("organization_id"));

// ── Rate limiting (task 015) ──────────────────────────────────────────────
// Política fixa por IP no endpoint de signup — superfície anônima que escreve no banco é alvo
// natural de abuso/enumeração/spam. PermitLimit baixo de propósito: signup não é uma ação de
// alta frequência legítima por usuário.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("signup", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));
});

// ── CORS (frontend React/Vite em origem separada) ────────────────────────
// Dev server do Vite roda em localhost:5173 (porta padrão). Em produção, trocar a origem fixa
// por uma lista vinda de configuração (domínio real do frontend) — nunca AllowAnyOrigin com
// AllowCredentials (combinação insegura, o browser já bloqueia, mas documentando a intenção).
const string FrontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Pipeline HTTP ─────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Sem redirect HTTPS em dev: frontend chama http://localhost:5262 direto (VITE_API_BASE_URL);
// redirecionar pra https://localhost:7062 exigiria confiar no cert dev (`dotnet dev-certs
// https --trust`) e o browser bloqueia a chamada sem isso — erro genérico sem pista nenhuma.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseOrganizationResolution(); // popula IOrganizationContext a partir da claim organization_id do JWT
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// ── Seed do primeiro organization/admin ────────────────────────────────────────
// `dotnet run -- seed`: executa o seed e encerra, sem subir o host.
if (args.Contains("seed", StringComparer.OrdinalIgnoreCase))
{
    await app.Services.RunIdentitySeedAsync();
    return;
}

// Em Development, roda o seed automaticamente (idempotente) antes de aceitar requisições.
if (app.Environment.IsDevelopment())
{
    await app.Services.RunIdentitySeedAsync();
}

app.Run();

// Marcador pra `WebApplicationFactory<Program>` (task 021, tests/Api.IntegrationTests) conseguir
// enxergar a classe `Program` gerada pelos top-level statements — sem isso ela é `internal` e
// invisível de outro assembly. Não muda nenhum comportamento do host.
public partial class Program { }
