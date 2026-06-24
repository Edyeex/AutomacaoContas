# AutoDownload Backend

Backend .NET 10 para o AutoDownload, conectado ao protótipo Next.js por API REST e persistido em PostgreSQL com Entity Framework Core.

## Arquitetura

- `AutoDownload.Domain`: entidades, value objects, enums e regras invariantes do domínio.
- `AutoDownload.Application`: casos de uso, contratos HTTP, portas de persistência, segurança e automação.
- `AutoDownload.Infrastructure`: EF Core, PostgreSQL, migrations, seed, Data Protection, hash de senha, JWT e estratégia demo de automação.
- `AutoDownload.Api`: API ASP.NET Core Minimal APIs, CORS, autenticação JWT Bearer e endpoints REST.
- `AutoDownload.Tests`: suíte xUnit com testes unitários de domínio e dos serviços principais, sem exigir banco rodando.

Padrões aplicados:

- Repository para isolar persistência.
- Strategy para automações por operadora.
- Dependency Injection para inversão de controle.
- Result Pattern para respostas previsíveis de casos de uso.
- Adapter/Port para criptografia, JWT, hash de senha, clock e automação.

## Configuração Segura

O repositório não contém senha do PostgreSQL nem chave de assinatura JWT. Em desenvolvimento, esses valores ficam no `user-secrets` do .NET, fora da pasta do projeto. Em hospedagem, configure as variáveis descritas em `backend/.env.example`.

As chaves usadas pela API são:

```text
ConnectionStrings__AutoDownload
Security__AccessToken__SigningKey
Security__CredentialEncryption__Key
Cors__AllowedOrigins__0
```

Não coloque valores reais em `appsettings.json` nem faça commit de arquivos `.env`.

## Docker e Render

O backend possui um `Dockerfile` multi-stage na raiz desta pasta. A imagem publica a API com .NET 10, instala Chromium e ChromeDriver para as automações Selenium e executa o processo como usuário sem privilégios.

No Render, crie um **Web Service** com estas configurações:

```text
Root Directory: backend
Runtime: Docker
Dockerfile Path: ./Dockerfile
Health Check Path: /api/health
```

Configure as variáveis abaixo usando exatamente os dois sublinhados (`__`):

```text
ConnectionStrings__AutoDownload=CONNECTION_STRING_DO_NEON
Security__AccessToken__SigningKey=CHAVE_ALEATORIA_COM_PELO_MENOS_32_CARACTERES
Security__CredentialEncryption__Key=CHAVE_BASE64_COM_32_BYTES
Cors__AllowedOrigins__0=https://SEU-FRONTEND.vercel.app
Database__ApplyMigrationsOnStartup=true
```

Gere `Security__CredentialEncryption__Key` com:

```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

Essa chave protege as senhas dos portais Vero/RMS no banco. Se ela mudar depois que contas forem cadastradas, edite cada conta, informe a senha novamente e salve.

O container escuta a porta `10000`, executa Vero/RMS em modo headless e grava PDFs locais em `/app/App_Data`. O sistema funciona sem disco persistente, mas os PDFs deixam de existir quando o Render recria o container. Para conservar arquivos entre deploys, monte um Persistent Disk nesse caminho ou migre os arquivos para um object storage.

Para construir a imagem em uma máquina com Docker:

```powershell
docker build -t autodownload-api .\backend
docker run --rm -p 10000:10000 --env-file .\backend\.env autodownload-api
```

No frontend publicado na Vercel, a variável correta é:

```text
NEXT_PUBLIC_API_URL=https://URL-DO-BACKEND.onrender.com/api
```

## PostgreSQL Local Sem Docker

Instale o PostgreSQL direto no Windows. Pelo terminal do VS Code, você pode instalar com:

```powershell
winget install PostgreSQL.PostgreSQL.16
```

Depois execute, na raiz do projeto:

```powershell
.\database\create-autodownload-db.ps1
```

O script pede uma senha para o usuário da aplicação e a senha do administrador `postgres`. Depois ele cria o usuário/banco `autodownload`, gera uma chave JWT aleatória, salva as configurações no `user-secrets` e aplica as migrations do Entity Framework.

Se preferir fazer pelo **pgAdmin**, crie:

```text
Usuário: autodownload
Senha: escolha uma senha local forte
Database: autodownload
Owner do database: autodownload
```

No pgAdmin, o caminho é:

1. Clique com botão direito em `Login/Group Roles`.
2. Crie o usuário `autodownload`.
3. Em `Definition`, coloque a senha local escolhida.
4. Clique com botão direito em `Databases`.
5. Crie o banco `autodownload`.
6. Em `Owner`, selecione `autodownload`.

Se criar o banco manualmente pelo pgAdmin, configure os segredos antes de iniciar a API:

```powershell
dotnet user-secrets set "ConnectionStrings:AutoDownload" "Host=localhost;Port=5432;Database=autodownload;Username=autodownload;Password=SUA_SENHA" --project .\backend\src\AutoDownload.Api\AutoDownload.Api.csproj
dotnet user-secrets set "Security:AccessToken:SigningKey" "UMA_CHAVE_ALEATORIA_COM_PELO_MENOS_32_CARACTERES" --project .\backend\src\AutoDownload.Api\AutoDownload.Api.csproj
```

## Executar API

Para subir backend e frontend juntos usando um unico terminal, rode na raiz do projeto:

```powershell
.\run-dev.ps1
```

Se alguma porta antiga ficar presa, use:

```powershell
.\run-dev.ps1 -StopExisting
```

No VS Code, tambem da para usar `Terminal > Run Task... > AutoDownload: rodar tudo`.

Se quiser rodar somente a API:

Na raiz do projeto:

```powershell
dotnet restore .\backend\AutoDownload.sln
dotnet run --project .\backend\src\AutoDownload.Api\AutoDownload.Api.csproj --launch-profile http
```

A API fica em:

```text
http://localhost:5080/api
```

Ao iniciar, a API aplica migrations e executa seed automaticamente, conforme `Database:ApplyMigrationsOnStartup`.

Credencial seedada para testar o front:

```text
E-mail: eder.casagranda@email.com
Senha: 123456
```

## Automação Demo

O operador `Operador Demo` executa todo o fluxo sem acessar um portal externo. Ele gera um PDF válido, registra o boleto no banco, adiciona a execução ao histórico e cria uma notificação.

Para demonstrar:

1. Entre com a credencial seedada.
2. Em `Contas`, adicione uma conta para `Operador Demo`.
3. Preencha login, identificador e senha com valores fictícios não vazios.
4. Clique em `Executar`.
5. Abra `Boletos` e faça o download do PDF gerado.

Os arquivos ficam em `App_Data/demo-bills` por padrão. O diretório pode ser alterado com `Automation__Demo__StorageDirectory`. Esse operador é simulado; Vero Internet e RMS Telecom permanecem como automações reais e dependem dos respectivos portais.

## Automação Vero Internet

A operadora `Vero Internet` usa uma integração Selenium baseada no projeto `Edyeex/AutoBot`.

Para executar:

1. Entre no site com o usuário seedado.
2. Vá em `Contas`.
3. Cadastre uma nova conta usando a operadora `Vero Internet`.
4. Informe o documento/login e a senha reais do portal Minha Vero.
5. Clique em executar na conta cadastrada.

Quando o portal permitir o download, a automação:

- baixa a fatura PDF pelo Chrome;
- salva o arquivo em `%USERPROFILE%\Downloads\AutoDownload\boletos`;
- grava o boleto na tabela `bills`;
- grava a execução na tabela `automation_runs`;
- exibe a execução no histórico do site.

Configuração em `appsettings.json`:

```json
"Automation": {
  "VeroInternet": {
    "LoginUrl": "https://verointernet.com.br/minhavero/login",
    "InvoiceUrl": "https://verointernet.com.br/minhavero/fatura",
    "DownloadDirectory": "%USERPROFILE%\\Downloads\\AutoDownload",
    "Headless": false
  }
}
```

## Testes Unitários

Execute na raiz do projeto:

```powershell
dotnet test .\backend\tests\AutoDownload.Tests\AutoDownload.Tests.csproj
```

A suíte cobre regras de domínio e os serviços de autenticação, contas, boletos e notificações usando repositórios em memória. Os testes de integração da API ficam em uma etapa separada.

## Migrations

Criar nova migration:

```powershell
dotnet ef migrations add NomeDaMigration --project .\backend\src\AutoDownload.Infrastructure\AutoDownload.Infrastructure.csproj --startup-project .\backend\src\AutoDownload.Api\AutoDownload.Api.csproj --output-dir Persistence\Migrations
```

Aplicar manualmente:

```powershell
dotnet ef database update --project .\backend\src\AutoDownload.Infrastructure\AutoDownload.Infrastructure.csproj --startup-project .\backend\src\AutoDownload.Api\AutoDownload.Api.csproj
```

## Rodar Junto Com O Front

Em outro terminal:

```powershell
cd .\autodownload
$env:NEXT_PUBLIC_API_URL="http://localhost:5080/api"
npm run dev -- -p 3000
```

Abra:

```text
http://localhost:3000
```
