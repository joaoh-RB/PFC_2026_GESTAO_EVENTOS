# Integração com a API do Google Calendar

Documentação da sincronização de eventos acadêmicos do Symplosio com um calendário Google compartilhado. O usuário autenticado pode adicionar ou remover o evento da própria agenda; o sistema faz isso incluindo (ou retirando) o e-mail da pessoa como convidado no evento do Google Calendar.

Esta funcionalidade foi introduzida na branch `entrega2809` pelos commits:

| Commit | Mensagem | O que entrega |
| --- | --- | --- |
| [`8983f8e`](https://github.com/joaoh-RB/PFC_2026_GESTAO_EVENTOS/commit/8983f8e44eaefce4d0c6e3a1b9baa8cd2ba203c9) | Integração com a agenda do Google Calendar adicionada | Cliente da API Calendar v3, criação/atualização/cancelamento de eventos, inclusão de convidado e botão no front-end |
| [`8e06e1b`](https://github.com/joaoh-RB/PFC_2026_GESTAO_EVENTOS/commit/8e06e1bbe6073b63cf9a82149fa3fbe4a8cb489e) | Integração da funcionalidade de adicionar eventos à agenda com registros no banco de dados | Persistência local de quem adicionou o evento, remoção da agenda e flags na listagem |

## Objetivo

- Toda criação de evento no sistema gera um evento correspondente no Google Calendar da aplicação.
- Edição de nome, descrição e datas replica no Calendar.
- Inativação marca o evento Google como `cancelled`; reativação volta para `confirmed`.
- O participante autenticado, na listagem de eventos futuros, pode **Adicionar à Agenda** ou **Remover da Agenda**. Isso o coloca (ou tira) da lista de *attendees*; o Google envia o convite/atualização para o e-mail cadastrado no sistema.

## Visão geral da arquitetura

```
[Front-end React]
  EventList + AddToCalendarButton / RemoveFromCalendarButton
        │  POST /api/events/{id}/add-to-calendar
        │  POST /api/events/{id}/remove-from-calendar
        ▼
[EventsController]  ──►  [EventService]
                              │
              ┌───────────────┼────────────────┐
              ▼               ▼                ▼
     EventRepository   AuthService      GoogleCalendarService
     (PostgreSQL)      (e-mail do        (Google Calendar API v3)
                        usuário)
```

- Camada de aplicação: `api_projetos/src/Application/Services/EventService.cs`
- Cliente Google: `api_projetos/src/Infrastructure/Services/GoogleCalendar/CalendarService.cs` (`GoogleCalendarService`)
- Registro DI: `services.AddScoped<GoogleCalendarService>()` em `DependencyInjection.cs`
- Pacotes: `Google.Apis.Calendar.v3` (1.75.0.4206) e `Google.Apis.Auth` (1.76.0)

Há **um único calendário** (`GoogleCalendar:CalendarId`). Não há calendário por instituição. O vínculo evento local ↔ evento Google é o campo `Events.GoogleCalendarEventId`.

## Autenticação no Google (conta da aplicação)

A API não faz OAuth por usuário final. Ela usa **credenciais OAuth 2.0 de uma conta de serviço da aplicação** (fluxo *refresh token* de usuário):

1. `ClientId` e `ClientSecret` de um cliente OAuth no Google Cloud.
2. `RefreshToken` de uma conta Google que tenha permissão de escrita no calendário configurado.
3. Escopo `https://www.googleapis.com/auth/calendar` (`CalendarService.Scope.Calendar`).
4. `UserCredential` com user id interno `gestao-eventos-admin` e `ApplicationName` `GestaoEventosAcademicos`.

Com o refresh token, a biblioteca obtém access tokens automaticamente. O usuário final **não** precisa autorizar o Google no navegador do Symplosio: ele só precisa ter o mesmo e-mail da conta Google para receber o convite.

## Configuração local e de deploy

Inclua a seção abaixo em `appsettings.json`, User Secrets ou variáveis de ambiente. **Não versione** `ClientSecret` nem `RefreshToken`.

```json
"GoogleCalendar": {
  "CalendarId": "id-do-calendario@group.calendar.google.com",
  "ClientId": "xxxxx.apps.googleusercontent.com",
  "ClientSecret": "xxxxx",
  "RefreshToken": "xxxxx"
}
```

| Chave | Uso |
| --- | --- |
| `CalendarId` | Identificador do calendário (e-mail do calendário ou `primary` da conta dona do token) |
| `ClientId` / `ClientSecret` | Cliente OAuth 2.0 no Google Cloud Console |
| `RefreshToken` | Token de longa duração da conta que gerencia o calendário |

Se qualquer chave faltar, a construção de `GoogleCalendarService` lança `InvalidOperationException` e a API não sobe corretamente nas rotas que dependem do serviço.

### Como obter as credenciais

1. No [Google Cloud Console](https://console.cloud.google.com/), crie (ou use) um projeto.
2. Ative a **Google Calendar API**.
3. Em **APIs e serviços → Credenciais**, crie um ID do cliente OAuth 2.0 (tipo *Aplicativo para computador* ou *Aplicativo da Web*).
4. Configure a tela de consentimento e o escopo `https://www.googleapis.com/auth/calendar`.
5. Autorize a conta administradora uma vez (OAuth Playground ou um fluxo local) e **salve o refresh token**.
6. Crie um calendário (ou use o principal), copie o ID em *Configurações do calendário* e compartilhe-o com a conta do refresh token, se não for a dona.

## Modelo de dados

### `Events.GoogleCalendarEventId` (commit `8983f8e`)

- Coluna nullable em `Events`.
- Preenchida após `Events.Insert` na API Google.
- Eventos antigos (criados antes da integração) ficam com `null`: a listagem envia `allowsAddToCalendar = false` e o botão não aparece.

Migrations: `20260923233708_AddGoogleCalendarEventId` e `20260923233801_AddGoogleCalendarEventId2`.

### `EventAddedToCalendars` (commit `8e06e1b`)

Tabela de associação usuário × evento (quem já pediu para colocar o evento na agenda).

| Coluna | Tipo | Observação |
| --- | --- | --- |
| `EventId` + `UserId` | PK composta | Um registro por par |
| `IsActive` | bool (default true) | Soft delete na remoção da agenda |
| `AddedAt` | timestamptz | Momento da inclusão |

- FKs para `Events` e `Users` com `ON DELETE CASCADE`.
- Filtro global `IsActive` no EF; a busca para reativar usa `IgnoreQueryFilters()`.
- Entidade: `EventAddedToCalendar` em `Domain/Entities/Event.cs`.
- Mapping: `EventAddedToCalendarConfiguration`.
- Migration: `20260924110044_AddEventAddedToCalendar`.

## Ciclo de vida no Google Calendar

Implementado em `GoogleCalendarService`. Fuso usado nas datas: **`America/Sao_Paulo`**. Convidados não podem convidar outros, alterar o evento nem ver a lista de convidados (`GuestsCanInviteOthers/Modify/SeeOtherGuests = false`).

| Operação no sistema | Método Google | `SendUpdates` | Comportamento |
| --- | --- | --- | --- |
| Criar evento | `Events.Insert` | `None` | Cria evento sem notificar; retorna o `Id` gravado em `GoogleCalendarEventId` |
| Editar evento | `Events.Get` + `Update` | `All` | Replica nome, descrição, início/fim; status `confirmed` |
| Inativar / reativar | `Events.Get` + `Update` | `All` | Status `cancelled` ou `confirmed` |
| Adicionar à agenda | `Events.Get` + `Update` (attendees) | `All` | Inclui e-mail do usuário com `responseStatus = accepted` |
| Remover da agenda | `Events.Get` + `Update` (attendees) | `All` | Remove o attendee correspondente (e-mail normalizado em minúsculas) |

Se o evento Google não existir mais (`404`), atualização e desativação apenas registram *warning* no log e não quebram o fluxo local.

Regras no `EventService`:

- **Criar:** valida cursos da instituição → `CreateEventAsync` → persiste o evento com o id Google.
- **Editar:** carrega o `GoogleCalendarEventId` existente → atualiza o Calendar → persiste no banco.
- **Ativar/inativar:** não permite inativar evento que já começou (`StartDate < UtcNow`); em seguida sincroniza o status no Google.
- **Adicionar à agenda:** exige usuário e evento existentes e `GoogleCalendarEventId` preenchido; depois cria ou reativa o registro em `EventAddedToCalendars`.
- **Remover da agenda:** retira o attendee no Google e marca `IsActive = false` no banco (não apaga a linha).

## Endpoints HTTP

Base: `/api/Events` (`EventsController`). JWT obrigatório (`[Authorize]`).

| Método | Rota | Quem chama | Sucesso |
| --- | --- | --- | --- |
| `POST` | `/{id}/add-to-calendar` | Usuário autenticado | `200` `{ "message": "Evento adicionado à sua agenda do Google com sucesso!" }` |
| `POST` | `/{id}/remove-from-calendar` | Usuário autenticado | `200` `{ "message": "Evento removido da sua agenda do Google com sucesso!" }` |

O `id` é o GUID do evento **no sistema**. O usuário é o `ClaimTypes.NameIdentifier` do JWT. Falhas de regra de negócio ou da API Google retornam `400` com `{ "message": "..." }`.

Endpoints já existentes que **também** tocam o Calendar (papéis Professor, Administrador ou Secretaria):

- `POST /api/Events` — cria no Google e no banco.
- `PUT /api/Events/{id}` — atualiza no Google e no banco.
- `PATCH /api/Events/{id}/active` — cancela ou reconfirma no Google.

A listagem `GET /api/Events` inclui, por item:

- `allowsAddToCalendar` — `true` se `GoogleCalendarEventId` não é nulo.
- `isAddedToCalendar` — `true` se existe registro ativo em `EventAddedToCalendars` para o usuário logado.

## Front-end

Arquivos:

- `Frontend/src/components/events/AddToCalendarButton.tsx`
- `Frontend/src/components/events/RemoveFromCalendarButton.tsx`
- `Frontend/src/components/events/EventList.tsx`

Na listagem, o botão só aparece se o evento está **ativo**, **ainda não começou** e `allowsAddToCalendar` é verdadeiro. O estado local `calendarOverrides` troca o botão imediatamente após o POST, sem recarregar a página.

Fluxo:

1. `POST /events/{id}/add-to-calendar` → ícone de check e texto “Adicionado à Agenda”.
2. `POST /events/{id}/remove-from-calendar` → “Removido da Agenda”, depois a UI volta a oferecer adicionar (via override).

O Axios usa o mesmo cliente autenticado (`@/services/api`). O Google Calendar do usuário é atualizado pelo convite enviado ao e-mail cadastrado, não por um embed do Calendar no React.