# Instruções de execução
- É possível visualizar o deploy do sistema na web. Para acessá-lo, entre no link:
  https://symplosio.vercel.app/login
- Para executar localmente, é necessário ter instalado:
  - **Git** (para clonar o repositório)
  - **.NET 10 SDK** (para compilar e executar a API back-end em C#)
  - **EF Core CLI (`dotnet-ef`)** (para executar as *migrations* do banco de dados)
  - **Node.js (versão LTS)** e **npm** (para instalar as dependências e rodar o front-end em React/Vite)
  - **PostgreSQL** (instância local ou banco em nuvem via **Supabase**)
### Passo a passo para execução local
1. **Clonar o repositório:**
   ```bash
   git clone https://github.com/joaoh-RB/PFC_2026_GESTAO_EVENTOS
   cd PFC_2026_GESTAO_EVENTOS
   ```
2. **Configurar e executar o Back-end (`api_projetos`):**
   - Acesse a pasta `api_projetos` e crie o arquivo `appsettings.json` (utilizando `appsettings.Example.json` como base).
   - Preencha as configurações obrigatórias no `appsettings.json`:
     - `ConnectionStrings:DefaultConnection`: String de conexão do banco PostgreSQL.
     - `JwtSettings`: Chave secreta (`Secret`), `Issuer` e `Audience` para geração do token JWT.
     - `FrontendInfo:BaseUrl`: URL do front-end local (`http://localhost:3000`) para liberação de CORS.
     - `Supabase`: `Url`, `ServiceKey` e `BucketName` (para armazenamento de documentos dos grupos).
     - `Email`: `ApiKey` (SendGrid) e remetente (`From`, `FromName`) para envio de e-mails.
     - `GoogleCalendar`: `CalendarId`, `ClientId`, `ClientSecret` e `RefreshToken` (para sincronização com o Google Calendar).
   - No terminal, dentro da pasta `api_projetos`, execute:
     ```bash
     dotnet restore
     dotnet ef database update
     dotnet run --launch-profile https
     ```
   - A API estará disponível em `https://localhost:7168` (com documentação interativa em `https://localhost:7168/swagger`).
3. **Configurar e executar o Front-end (`Frontend`):**
   - Acesse a pasta `Frontend` e certifique-se de que o arquivo `.env` contém a URL da API local:
     ```env
     VITE_API_BASE_URL=https://localhost:7168/api
     ```
   - No terminal, dentro da pasta `Frontend`, instale os pacotes e inicie o servidor de desenvolvimento:
     ```bash
     npm install
     npm run dev
     ```
   - Acesse a aplicação no navegador em: `http://localhost:3000`
---


# Evidências
![alt text](image.png)
![alt text](image-1.png)