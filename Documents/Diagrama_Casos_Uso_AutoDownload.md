# Diagrama de Casos de Uso - AutoDownload

```mermaid
flowchart LR
    usuario["Usuario"]:::actor
    agendador["Agendador mensal"]:::actor
    portal["Portal da operadora<br/>(Vero Internet / RMS Telecom)"]:::external

    subgraph sistema["Sistema AutoDownload"]
        UC01(["Cadastrar usuario"])
        UC02(["Realizar login"])
        UC03(["Recuperar senha"])
        UC04(["Visualizar painel geral"])
        UC05(["Gerenciar perfil"])

        UC06(["Cadastrar conta de operadora"])
        UC07(["Editar conta de operadora"])
        UC08(["Remover conta de operadora"])
        UC09(["Criptografar credenciais do portal"])

        UC10(["Executar automacao manual"])
        UC11(["Agendar automacao mensal"])
        UC12(["Executar automacao agendada"])
        UC13(["Autenticar no portal da operadora"])
        UC14(["Localizar fatura disponivel"])
        UC15(["Baixar boleto em PDF"])
        UC16(["Salvar boleto no servidor"])
        UC17(["Registrar boleto no banco"])
        UC18(["Registrar historico da execucao"])
        UC19(["Gerar notificacao"])

        UC20(["Listar boletos baixados"])
        UC21(["Baixar boleto pelo site"])
        UC22(["Consultar historico"])
        UC23(["Gerenciar notificacoes"])
        UC24(["Marcar notificacoes como lidas"])
        UC25(["Excluir notificacoes"])
    end

    usuario --> UC01
    usuario --> UC02
    usuario --> UC03
    usuario --> UC04
    usuario --> UC05
    usuario --> UC06
    usuario --> UC07
    usuario --> UC08
    usuario --> UC10
    usuario --> UC11
    usuario --> UC20
    usuario --> UC21
    usuario --> UC22
    usuario --> UC23
    usuario --> UC24
    usuario --> UC25

    agendador --> UC12
    portal --> UC13
    portal --> UC14
    portal --> UC15

    UC06 -. "include" .-> UC09
    UC07 -. "include" .-> UC09
    UC10 -. "include" .-> UC13
    UC10 -. "include" .-> UC14
    UC10 -. "include" .-> UC15
    UC10 -. "include" .-> UC16
    UC10 -. "include" .-> UC17
    UC10 -. "include" .-> UC18
    UC10 -. "include" .-> UC19
    UC12 -. "include" .-> UC10
    UC21 -. "include" .-> UC20
    UC23 -. "include" .-> UC24
    UC23 -. "include" .-> UC25

    classDef actor fill:#ffffff,stroke:#1f4f8f,stroke-width:2px,color:#0f172a;
    classDef external fill:#fff7ed,stroke:#d97706,stroke-width:2px,color:#7c2d12;
```

## Atores

- **Usuario:** pessoa que acessa o AutoDownload para cadastrar contas, executar automacoes, baixar boletos e acompanhar historico/notificacoes.
- **Agendador mensal:** processo automatico do sistema que verifica contas com agendamento ativo e inicia a automacao no periodo configurado.
- **Portal da operadora:** sistemas externos das operadoras, como Vero Internet e RMS Telecom, acessados pela automacao para localizar e baixar boletos.

## Principais Casos de Uso

- **Autenticacao:** cadastrar usuario, realizar login, recuperar senha e manter sessao protegida por JWT.
- **Gestao de contas:** cadastrar, editar, remover e proteger credenciais das contas de operadoras.
- **Automacao de boletos:** executar manualmente ou por agendamento, acessar portal externo, localizar fatura, baixar PDF e registrar resultado.
- **Acompanhamento:** visualizar painel geral, listar boletos, baixar arquivos, consultar historico e receber notificacoes.
- **Notificacoes:** marcar como lidas, remover notificacoes individuais ou limpar todas.
