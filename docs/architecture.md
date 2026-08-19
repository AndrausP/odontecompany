# Architecture

> Maintained by Nirvana. Updated automatically every N sessions via /reflect.
> Last updated: 2026-08-17 (seeded from Arquitetura-Plataforma-Odontologica.pdf)

## Stack

- Backend: .NET 8 / C# 12, ASP.NET Core, EF Core 8, PostgreSQL 16, Redis, RabbitMQ + MassTransit
- App layer: MediatR (CQRS), FluentValidation, Result pattern, Pipeline Behaviors
- Auth: JWT (Microsoft.AspNetCore.Authentication.JwtBearer) + refresh token, hash Argon2/BCrypt, RBAC
- Frontend: React 18 + TypeScript, Vite, TanStack Query, React Hook Form + Zod, Tailwind + shadcn/ui, Zustand
- Infra: Docker/Compose, Kubernetes (opcional), NGINX/YARP, OpenTelemetry, GitHub Actions
- Testes: xUnit, FluentAssertions, Testcontainers, NSubstitute, Bogus (doc) — projeto usa NUnit+Moq por convenção global do usuário

## Project Structure

Monólito modular, Clean Architecture por módulo (Domain/Application/Infrastructure/Contracts), host único em `src/Bootstrap/OdontoPlatform.Api`. Ver `docs/modules.md` e o PDF de origem para o layout completo de `OdontoPlatform.sln`.

## Key Layers

- Domain: entidades, agregados, VOs, domain events — zero dependência de framework
- Application: commands/queries, handlers, validators, portas (interfaces)
- Infrastructure: EF Core, Redis, MassTransit/RabbitMQ, integrações externas
- API/Host: endpoints HTTP, auth, DI, middleware transversal (tenant resolution)

## External Dependencies

PostgreSQL 16, Redis, RabbitMQ, object storage (anexos de prontuário — fase 2).

## Request Flow

Requisição → middleware de tenant (resolve `tenant_id` do token/subdomínio) → Controller/Endpoint → MediatR Command/Query → Pipeline Behaviors (validação, log, transação) → Handler → Domain/Repository → resposta. Global query filter do EF Core aplica `tenant_id` automaticamente.
