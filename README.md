# Symplosio - Gestão de Palestras e Eventos

Sistema web para centralizar a divulgação, inscrição e gestão de palestras e eventos acadêmicos, permitindo o cadastro de instituições, cursos, usuários e eventos, com controle de presença e aprovações. Projeto de Final de Curso (PFC) do curso de Desenvolvimento de Software Corporativo.

## Equipe

- Gabriel Antonio Vieira Cordeiro
- João Henrique Rodrigues Batista
- Pedro Henrique Santos de Andrade

**Orientadora:** Viviane Guimarães Ribeiro

## Stack

### Back-end (`api_projetos`)
- **.NET 10** com **C#**
- **Entity Framework Core** + **Npgsql** (PostgreSQL)
- **Swashbuckle / Swagger** (documentação da API)
- Hospedagem: **Render** (API, via Docker) + **Supabase** (PostgreSQL)

### Front-end (`Frontend`)
- **React 19** + **TypeScript** + **Vite**
- **TailwindCSS 4**
- **React Router 7**
- **React Hook Form** + **Zod** (formulários e validação)
- **Axios** (requisições HTTP)
- **shadcn/ui**, **lucide-react** (componentes e ícones)

## Arquitetura (back-end)

O back-end segue uma organização em camadas:

- `Domain/` — entidades, enums e Value Objects, sem dependência de infraestrutura.
- `Application/` — regras de negócio (`Services`), DTOs de entrada/saída, e `Validators` (FluentValidation).
- `Infrastructure/` — acesso a dados (`Data/Context`, `Data/Configurations`, `Data/Repositories`, `Data/Migrations`) e serviços técnicos (`Services`).
- `Controllers/` — endpoints da API, delegando a lógica para a camada `Application`.

Usuários (`Student`, `Teacher`, `Administrator`) são modelados por herança (estratégia *Table-Per-Hierarchy*), compartilhando a tabela `Users`, diferenciados pela coluna `Role`.

## Perfis de acesso

- **Administrador** — acesso irrestrito a todas as instituições (cadastro de instituições, e demais operações em qualquer escopo).
- **Professor (Teacher)** — vinculado a uma instituição.
- **Aluno (Student)** — vinculado a um curso/instituição; inscreve-se e participa de eventos.

## Funcionalidades implementadas

- Autenticação (registro de aluno, login, logout, JWT em cookie HttpOnly, 2FA)
- Cadastro de Instituição e Curso (criação, consulta, edição e exclusão), com validação de CNPJ (incluindo formato alfanumérico vigente desde jul/2026) e regras de unicidade
- Cadastro e gestão de Eventos
- Fluxo de aprovação de usuários

## Como rodar o projeto

### Pré-requisitos
- .NET 10 SDK
- Node.js (LTS)
- Uma instância PostgreSQL (local ou Supabase)

### Back-end

```bash
cd api_projetos
cp appsettings.Example.json appsettings.json
# edite appsettings.json com sua connection string e uma chave secreta JWT
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

## Migrations

Para gerar uma nova migration após alterar entidades:

```bash
dotnet ef migrations add NomeDaMigration
dotnet ef database update
```
