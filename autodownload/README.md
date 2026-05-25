# AutoDownload Frontend

Protótipo Next.js conectado ao backend .NET 10 por REST.

## Executar com o backend local

Na raiz do projeto, prepare primeiro o PostgreSQL local conforme `backend/README.md`.

Para subir backend e frontend juntos em um unico terminal:

```powershell
.\run-dev.ps1
```

Se alguma instancia antiga ainda estiver usando as portas `5080` ou `3000`:

```powershell
.\run-dev.ps1 -StopExisting
```

No VS Code, tambem da para usar `Terminal > Run Task... > AutoDownload: rodar tudo`.

Se quiser rodar somente o frontend:

Depois, em outro terminal:

```powershell
cd .\autodownload
$env:NEXT_PUBLIC_API_URL="http://localhost:5080/api"
npm run dev -- -p 3000
```

Abra:

```text
http://localhost:3000
```

Credencial seedada:

```text
E-mail: eder.casagranda@email.com
Senha: 123456
```

## Build

```powershell
npm run build
```

Se `NEXT_PUBLIC_API_URL` não for informado, o front usa `http://localhost:5080/api`.
