# Symplosio - Gestão de Palestras e Eventos

Sistema web para centralizar a divulgação, inscrição e gestão de palestras e eventos acadêmicos, permitindo o cadastro de instituições, cursos, usuários e eventos, com controle de presença, grupos de trabalho, avaliação de documentos e aprovações. Projeto de Final de Curso (PFC) do curso de Desenvolvimento de Software Corporativo.

## Equipe

- Gabriel Antonio Vieira Cordeiro
- João Henrique Rodrigues Batista
- Pedro Henrique Santos de Andrade

**Orientadora:** Viviane Guimarães Ribeiro

## Stack

### Back-end (`api_projetos`)
- **.NET 10** com **C#**
- **Entity Framework Core** + **Npgsql** (PostgreSQL)
- **FluentValidation** (validação das entradas)
- **Swashbuckle / Swagger** (documentação da API)
- **SendGrid** (envio de e-mails) e **Google Calendar API** (sincronização de eventos)
- **xUnit** + **Moq** (testes automatizados)
- Hospedagem: **Render** (API, via Docker) + **Supabase** (PostgreSQL e armazenamento de arquivos)

### Front-end (`Frontend`)
- **React 19** + **TypeScript** + **Vite**
- **TailwindCSS 4**
- **React Router 7**
- **React Hook Form** + **Zod** (formulários e validação)
- **Axios** (requisições HTTP)
- **shadcn/ui**, **lucide-react** (componentes e ícones)
- **xlsx** (exportação de dados em planilha)
- Hospedagem: **Vercel**

## Arquitetura (back-end)

O back-end segue uma organização em camadas:

- `Domain/` — entidades, enums e Value Objects, sem dependência de infraestrutura.
- `Application/` — regras de negócio (`Services`), DTOs de entrada/saída, e `Validators` (FluentValidation).
- `Infrastructure/` — acesso a dados (`Data/Context`, `Data/Configurations`, `Data/Repositories`, `Data/Migrations`) e serviços técnicos (`Services`), incluindo o cliente da API Google Calendar.
- `Controllers/` — endpoints da API, delegando a lógica para a camada `Application`.

Usuários (`Student`, `Teacher`, `Administrator`) são modelados por herança (estratégia *Table-Per-Hierarchy*), compartilhando a tabela `Users`, diferenciados pela coluna `Role`.

## Perfis de acesso

- **Administrador** — acesso irrestrito a todas as instituições, cursos, eventos e usuários; único perfil que cadastra instituições.
- **Secretaria** — vinculada a uma instituição; gerencia cursos, professores, alunos e eventos dessa instituição e aprova cadastros de alunos.
- **Professor (Teacher)** — vinculado a uma instituição; cria e aprova eventos, avalia documentos dos grupos, gerencia presença e acompanha o dashboard.
- **Aluno (Student)** — vinculado a um curso/instituição; inscreve-se em eventos, solicita eventos, forma grupos, envia documentos, confirma presença e envia feedback.

## Funcionalidades implementadas

- Autenticação (registro de aluno, login, logout, JWT em cookie HttpOnly, 2FA, recuperação de senha)
- Fluxo de aprovação de cadastro de alunos
- Cadastro de Instituição e Curso (criação, consulta, edição, inativação e reativação), com validação de CNPJ (incluindo formato alfanumérico vigente desde jul/2026) e regras de unicidade
- Gestão de Eventos, com tipo, vagas, cursos permitidos, prazo e limite de integrantes por grupo
- Solicitação de eventos pelo aluno, com aprovação ou reprovação por Professor ou Secretaria
- Inscrição em eventos e controle de presença por código ou de forma manual
- Grupos de trabalho, com convite, pedido de entrada aprovado pelo líder, limite por evento e prazo
- Envio e avaliação de documentos dos grupos, com histórico de aprovação e download compilado em ZIP
- Feedback do evento (nota de 0 a 10 e comentário)
- Dashboard por perfil, com indicadores, gráficos e filtros
- Exportação de listas em planilha XLSX
- Integração com Google Calendar (criação/edição/cancelamento no calendário da aplicação e inclusão do usuário como convidado — ver [Docs/integracao-google-calendar.md](Docs/integracao-google-calendar.md))
- Termos de Uso e Política de Privacidade com aceite versionado (LGPD) e trilha de auditoria

## Como rodar o projeto

### Pré-requisitos
- .NET 10 SDK
- Node.js (LTS)
- Uma instância PostgreSQL (local ou Supabase)

### Back-end

```bash
cd api_projetos
cp appsettings.Example.json appsettings.json
# edite appsettings.json: connection string, JWT e credenciais GoogleCalendar (ver docs/integracao-google-calendar.md)
dotnet restore
dotnet ef database update
dotnet run --launch-profile https
```

A API sobe em `https://localhost:7168` (e `http://localhost:5259`). Documentação interativa disponível em `/swagger` no ambiente de desenvolvimento.

### Front-end

```bash
cd Frontend
npm install
npm run dev
```

## Testes

Os testes automatizados da API ficam em `api_projetos/tests/API_Gestao_Eventos.Tests` e cobrem services, validadores e testes de integração dos endpoints, com banco SQLite temporário.

```bash
cd api_projetos/tests/API_Gestao_Eventos.Tests
dotnet test
```

## Diagramas

Os diagramas BPMN e de classes usados na monografia estão em formato Mermaid em `api_projetos/docs/diagrams`.

## Migrations

Para gerar uma nova migration após alterar entidades:

```bash
dotnet ef migrations add NomeDaMigration
dotnet ef database update
```
