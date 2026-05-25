# AutoDownload Backend

Backend .NET 10 para o AutoDownload, conectado ao protótipo Next.js por API REST e persistido em PostgreSQL com Entity Framework Core.

## Arquitetura

- `AutoDownload.Domain`: entidades, value objects, enums e regras invariantes do domínio.
- `AutoDownload.Application`: casos de uso, contratos HTTP, portas de persistência, segurança e automação.
- `AutoDownload.Infrastructure`: EF Core, PostgreSQL, migrations, seed, Data Protection, hash de senha, JWT e estratégia demo de automação.
- `AutoDownload.Api`: API ASP.NET Core Minimal APIs, CORS, autenticação JWT Bearer e endpoints REST.
- `AutoDownload.Tests`: runner simples para checagens de domínio sem exigir banco rodando.

Padrões aplicados:

- Repository para isolar persistência.
- Strategy para automações por operadora.
- Dependency Injection para inversão de controle.
- Result Pattern para respostas previsíveis de casos de uso.
- Adapter/Port para criptografia, JWT, hash de senha, clock e automação.

## PostgreSQL Local Sem Docker

Instale o PostgreSQL direto no Windows. Pelo terminal do VS Code, você pode instalar com:

```powershell
winget install PostgreSQL.PostgreSQL.16
```

Depois execute, na raiz do projeto:

```powershell
.\database\create-autodownload-db.ps1
```

O script pede a senha do usuário administrador `postgres`, cria o usuário/banco `autodownload` e aplica as migrations do Entity Framework.

Se preferir fazer pelo **pgAdmin**, crie:

```text
Usuário: autodownload
Senha: autodownload
Database: autodownload
Owner do database: autodownload
```

No pgAdmin, o caminho é:

1. Clique com botão direito em `Login/Group Roles`.
2. Crie o usuário `autodownload`.
3. Em `Definition`, coloque a senha `autodownload`.
4. Clique com botão direito em `Databases`.
5. Crie o banco `autodownload`.
6. Em `Owner`, selecione `autodownload`.

A connection string padrão já está configurada em `backend/src/AutoDownload.Api/appsettings.json`:

```text
Host=localhost;Port=5432;Database=autodownload;Username=autodownload;Password=autodownload
```

Se você preferir usar outro usuário ou senha do PostgreSQL local, rode no terminal antes de iniciar a API:

```powershell
$env:ConnectionStrings__AutoDownload="Host=localhost;Port=5432;Database=autodownload;Username=postgres;Password=SUA_SENHA"
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
