# AUTODOWNLOAD: APLICAÇÃO WEB PARA AUTOMAÇÃO E GESTÃO DE BOLETOS RECORRENTES

Uma solução web para centralização, execução automatizada e acompanhamento de downloads de contas digitais

ULBRA - Análise e Desenvolvimento de Sistemas

Éder Casagranda¹

Lucas Fogaça²

¹ Acadêmico do Curso de Análise e Desenvolvimento de Sistemas - ULBRA. E-mail: [preencher e-mail institucional].

² Professor orientador do Curso de Análise e Desenvolvimento de Sistemas - ULBRA. E-mail: [preencher e-mail do orientador].

## RESUMO

O presente Trabalho de Conclusão de Curso apresenta o desenvolvimento do AutoDownload, uma aplicação web voltada à automação do acesso a portais de operadoras e à gestão de boletos recorrentes. A proposta surgiu a partir da observação de que muitos usuários ainda precisam acessar mensalmente diferentes sites para consultar faturas, gerar segunda via, baixar boletos e organizar arquivos manualmente, o que favorece perda de tempo, esquecimento de vencimentos e dificuldade no controle financeiro. Nesse contexto, o AutoDownload tem como objetivo centralizar o cadastro de contas, proteger credenciais de acesso, executar automações de download, registrar o histórico das execuções e disponibilizar notificações ao usuário em um ambiente digital unificado. O sistema foi desenvolvido com frontend em Next.js e React, utilizando JavaScript, HTML e CSS para construção da interface web, enquanto o backend foi implementado em C# com .NET 10, estruturado em camadas e integrado a uma API REST protegida por autenticação JWT. A persistência dos dados foi realizada com PostgreSQL, utilizando Entity Framework Core como ORM, migrations para versionamento do banco e seed para dados de demonstração. As automações foram implementadas com Selenium WebDriver e organizadas por meio do padrão Strategy, permitindo separar a lógica de cada operadora, como Vero Internet, RMS Telecom e operador demonstrativo. Também foram empregados Docker, Render, Vercel e Neon para preparação do ambiente de publicação. A metodologia adotada caracteriza-se como pesquisa aplicada de desenvolvimento tecnológico, com apoio em princípios de Design Thinking, levantamento de requisitos, prototipação, implementação incremental e validação funcional. Como resultado, obteve-se um protótipo funcional capaz de realizar login, cadastro de contas, execução de automações, agendamento mensal, registro de boletos, histórico e notificações. Conclui-se que o AutoDownload contribui para reduzir tarefas repetitivas e melhorar a organização de contas digitais, embora sua evolução ainda dependa de maior cobertura de testes, armazenamento persistente de arquivos e manutenção contínua das automações em razão da instabilidade de portais externos.

Palavras-chave: Automação web. Boletos recorrentes. Aplicação web. .NET. Next.js. PostgreSQL. Selenium.

## 1 INTRODUÇÃO

A digitalização de serviços financeiros, de telecomunicações e de utilidades públicas modificou a forma como usuários acessam documentos de cobrança. Atualmente, faturas, boletos e segundas vias costumam estar disponíveis em portais web ou aplicativos específicos de cada empresa. Embora essa disponibilidade digital represente um avanço em relação aos processos totalmente presenciais ou impressos, ela também fragmenta a rotina dos usuários, que precisam acessar diferentes plataformas para localizar documentos, autenticar-se, navegar até a área correta, baixar arquivos e organizar os comprovantes em seus dispositivos.

No contexto doméstico, é comum que uma pessoa precise gerenciar mensalmente contas de internet, telefone, energia, água, serviços digitais ou outros pagamentos recorrentes. Esse processo, quando realizado de forma manual, pode gerar perda de tempo, esquecimento de vencimentos, duplicidade de arquivos, desorganização de boletos e dificuldade de acompanhamento histórico. Além disso, a experiência varia conforme cada portal, exigindo que o usuário memorize caminhos, senhas e procedimentos diferentes.

Diante desse cenário, o projeto AutoDownload foi proposto como uma aplicação web para centralizar e automatizar parte desse fluxo. A solução permite que o usuário cadastre contas vinculadas a operadoras previamente disponibilizadas pelo sistema, armazene credenciais de forma protegida, execute automações para buscar boletos disponíveis e acompanhe os resultados por meio de dashboard, histórico, notificações e área de download. O escopo atual concentra-se em portais de telecomunicações, como Vero Internet e RMS Telecom, além de uma automação demonstrativa para validação controlada. Outras categorias, como energia e água, permanecem como possibilidades futuras da arquitetura.

O problema de pesquisa que orienta este trabalho pode ser formulado da seguinte maneira: como uma aplicação web, apoiada em automação de navegação, boas práticas de engenharia de software e mecanismos de segurança, pode reduzir o esforço manual necessário para obter e organizar boletos recorrentes?

O objetivo geral deste artigo é apresentar o desenvolvimento do AutoDownload, descrevendo sua concepção, metodologia, arquitetura, tecnologias utilizadas, funcionalidades implementadas, validação e limitações. Como objetivos específicos, destacam-se: identificar necessidades relacionadas ao gerenciamento de boletos recorrentes; projetar uma aplicação web com autenticação, cadastro de contas e organização de documentos; implementar um backend estruturado em camadas com persistência em PostgreSQL; desenvolver estratégias de automação para operadoras; registrar histórico e notificações; permitir agendamento mensal de execuções; e validar o funcionamento da solução por meio de testes e demonstração controlada.

A justificativa do projeto está relacionada à relevância prática do problema e à aderência ao curso de Análise e Desenvolvimento de Sistemas. Do ponto de vista do usuário, uma ferramenta centralizada pode reduzir tarefas repetitivas e melhorar a organização financeira. Do ponto de vista técnico, o projeto envolve levantamento de requisitos, modelagem, frontend, backend, banco de dados, segurança, automação web, testes, deploy e integração com serviços externos.

Este artigo está organizado em cinco seções. A primeira apresenta introdução, problema, objetivos e justificativa. A segunda discute fundamentos teóricos relacionados a sistemas de informação, automação web, segurança, arquitetura de software e Design Thinking. A terceira descreve a metodologia utilizada. A quarta apresenta o desenvolvimento da solução, incluindo modelagem, arquitetura, implementação, resultados e validação. Por fim, a quinta seção apresenta as conclusões, limitações e possibilidades de evolução.

## 2 FUNDAMENTAÇÃO TEÓRICA

### 2.1 Sistemas de informação e automação de processos

Sistemas de informação são utilizados para coletar, processar, armazenar e disponibilizar dados de maneira organizada, apoiando a execução de atividades e a tomada de decisão. Em aplicações web modernas, esses sistemas combinam interface, regras de negócio, persistência de dados e integração com serviços externos. Segundo Sommerville (2019), a organização estrutural de um software influencia diretamente sua manutenção, evolução e capacidade de atender requisitos funcionais e não funcionais.

A automação de processos busca reduzir a intervenção humana em atividades repetitivas, padronizadas e suscetíveis a erros. No contexto do AutoDownload, a atividade automatizada corresponde ao acesso a portais de operadoras para localizar e baixar boletos disponíveis. Essa automação não elimina a necessidade de controle do usuário, mas reduz etapas manuais, registra resultados e permite maior rastreabilidade das execuções.

Entretanto, automações dependentes de sites externos apresentam riscos técnicos. Portais podem alterar elementos visuais, modificar identificadores de campos, inserir validações adicionais ou bloquear acessos automatizados. Por isso, o sistema precisa prever tratamento de falhas, logs, notificações e possibilidade de manutenção por operadora.

### 2.2 Automação web com Selenium WebDriver

O Selenium WebDriver é uma ferramenta utilizada para controlar navegadores de forma programável, permitindo a realização de ações como preenchimento de formulários, cliques, navegação entre páginas, leitura de elementos e captura de arquivos gerados por sites (SELENIUM, 2026). Em projetos de automação, essa ferramenta possibilita reproduzir fluxos que normalmente seriam realizados manualmente pelo usuário.

No AutoDownload, o Selenium foi utilizado para executar estratégias específicas de automação em portais de operadoras. Cada estratégia é responsável por realizar login, navegar até a seção de faturas, identificar boletos disponíveis e iniciar o download do arquivo. Essa abordagem foi combinada ao padrão Strategy, permitindo que cada operadora possua sua própria lógica sem comprometer o restante da aplicação.

Apesar de sua utilidade, o Selenium possui limitações quando executado em ambiente de hospedagem gratuita ou em containers com poucos recursos. A automação pode ser impactada por tempo de resposta do servidor, necessidade de navegador headless, ausência de interface gráfica, alterações de layout e indisponibilidade dos portais externos.

### 2.3 Segurança da informação, autenticação e LGPD

Aplicações que manipulam dados pessoais e credenciais precisam considerar segurança desde as etapas iniciais do desenvolvimento. A Lei Geral de Proteção de Dados Pessoais, Lei n. 13.709/2018, estabelece princípios como finalidade, necessidade, transparência, segurança e prevenção no tratamento de dados pessoais (BRASIL, 2018).

No AutoDownload, são tratados dados como nome, e-mail, identificadores de cliente, logins de portal e senhas de acesso a operadoras. Por esse motivo, o sistema utiliza autenticação com JSON Web Token (JWT) para proteger rotas da API, hash para senhas dos usuários e criptografia para credenciais dos portais externos. As chaves sensíveis devem ser configuradas por variáveis de ambiente, evitando exposição em arquivos versionados.

Ainda assim, não se deve afirmar conformidade plena com a LGPD sem uma política formal de privacidade, retenção, auditoria e resposta a incidentes. O mais adequado é afirmar que o projeto adota práticas alinhadas a princípios da LGPD, reconhecendo limitações e melhorias futuras, como fortalecimento de política de senha, avaliação de cookies HttpOnly para sessão, armazenamento persistente seguro de arquivos e documentação clara sobre exclusão de dados.

### 2.4 Engenharia de software, arquitetura em camadas e padrões

A engenharia de software fornece métodos e práticas para desenvolver sistemas com qualidade, manutenibilidade e evolução controlada. Pressman e Maxim (2016) destacam que um projeto de software deve considerar requisitos, arquitetura, implementação, testes e manutenção como partes integradas do ciclo de desenvolvimento.

No AutoDownload, o backend foi organizado em camadas, separando API, aplicação, domínio e infraestrutura. A API expõe endpoints REST para consumo do frontend. A camada de aplicação concentra serviços e contratos responsáveis por orquestrar regras de negócio. O domínio representa entidades centrais, como usuário, conta, operadora, boleto, notificação e execução de automação. A infraestrutura implementa persistência, criptografia, automações e integrações externas.

Essa organização favorece princípios SOLID, especialmente responsabilidade única, inversão de dependência e abertura para extensão. O padrão Repository auxilia no isolamento do acesso ao banco de dados, enquanto o padrão Strategy permite variar a lógica de automação conforme a operadora. A injeção de dependência contribui para substituir implementações e facilitar testes.

### 2.5 Design Thinking aplicado ao levantamento de requisitos

O Design Thinking é uma abordagem voltada à compreensão de problemas a partir das necessidades dos usuários, geralmente associada às etapas de empatia, definição, ideação, prototipação e teste (BROWN, 2008). Norman (2006) também destaca a importância de projetar soluções considerando a experiência real das pessoas que utilizarão o sistema.

No AutoDownload, o Design Thinking foi utilizado como inspiração para compreender a dor relacionada ao gerenciamento manual de boletos. A etapa de empatia ocorreu por meio da observação do fluxo manual de acesso a portais. A definição concentrou o problema na redução do esforço repetitivo para obter e organizar contas digitais. Na ideação, foram propostas funcionalidades como dashboard, cadastro de contas, automações por operadora, histórico, notificações e agendamento mensal. A prototipação ocorreu por meio da interface web, inicialmente com dados simulados e posteriormente integrada ao backend.

### 2.6 Trabalhos correlatos

A análise de trabalhos correlatos permite compreender alternativas existentes e delimitar a contribuição do AutoDownload. Embora existam portais de operadoras, aplicativos bancários e ferramentas de organização pessoal, essas soluções geralmente resolvem apenas partes do problema, sem centralizar a automação de acesso a diferentes portais.

Quadro 1 - Comparação entre soluções correlatas

| Solução analisada | Centraliza contas | Automatiza download | Histórico | Notificações | Limitações |
| --- | --- | --- | --- | --- | --- |
| Portais individuais de operadoras | Não | Não | Parcial | Parcial | Exigem acesso separado a cada portal |
| Aplicativos bancários | Parcial | Não | Parcial | Sim | Dependem de boletos já cadastrados ou pagos |
| Planilhas/lembretes manuais | Parcial | Não | Manual | Manual | Exigem disciplina e atualização constante |
| AutoBot | Não | Sim, para fluxo específico | Limitado | Não integrado | Automação isolada, sem dashboard e banco estruturado |
| AutoDownload | Sim | Sim, por estratégia de operadora | Sim | Sim | Depende da estabilidade dos portais externos |

Fonte: Elaborado pelo autor (2026).

## 3 METODOLOGIA

A pesquisa caracteriza-se como aplicada, pois busca resolver um problema prático relacionado à automação e organização de boletos recorrentes. Também pode ser classificada como pesquisa de desenvolvimento tecnológico, uma vez que resultou na construção de um protótipo funcional de aplicação web. Quanto à abordagem, possui caráter qualitativo, pois analisa necessidades, fluxos de uso, requisitos e validação funcional do sistema. Quanto aos objetivos, apresenta caráter exploratório e descritivo, pois investiga a viabilidade técnica da automação em portais externos e descreve o processo de desenvolvimento da solução (GIL, 2017).

Diferentemente de uma pesquisa com amostra estatística, este trabalho utilizou como unidade de análise o fluxo manual de obtenção de boletos recorrentes. A coleta de dados ocorreu por meio da observação de etapas executadas pelo usuário, análise de documentos de proposta do projeto, levantamento de requisitos, revisões recebidas durante o acompanhamento acadêmico, execução local da aplicação e testes funcionais das principais funcionalidades.

A análise dos dados foi realizada pela transformação das necessidades observadas em requisitos funcionais e não funcionais. Entre os requisitos funcionais estão autenticação, cadastro de contas, execução de automação, listagem de boletos, histórico, notificações e agendamento mensal. Entre os requisitos não funcionais estão segurança, usabilidade, manutenibilidade, disponibilidade, rastreabilidade e proteção de credenciais.

O processo de desenvolvimento combinou princípios de Design Thinking com desenvolvimento incremental. Inicialmente, foram identificadas dores do usuário e definida a proposta de valor. Em seguida, foram criadas telas de alta fidelidade diretamente na aplicação frontend, com dados simulados. Posteriormente, a solução foi integrada à API real, ao banco PostgreSQL e às estratégias de automação. A cada incremento, funcionalidades foram testadas localmente e ajustadas conforme necessidades encontradas.

A validação ocorreu por meio de testes de build, testes unitários no backend com xUnit, verificação do endpoint de saúde da API, uso manual da aplicação, execução de automações reais em ambiente local e uso de operador demonstrativo para reduzir dependência de portais externos. Os resultados foram registrados por meio de histórico de execuções, notificações, registros no banco e arquivos baixados.

## 4 DESENVOLVIMENTO

### 4.1 Modelagem do sistema

A modelagem do AutoDownload partiu da rotina de usuários que precisam acessar portais diferentes para baixar boletos. Foram definidos três atores principais: usuário, sistema AutoDownload e portal da operadora. O usuário realiza cadastro, login, cadastro de contas, configuração de agendamento, execução manual de automação, consulta de boletos, histórico e notificações. O sistema AutoDownload autentica o usuário, protege dados, executa estratégias de automação, registra resultados e disponibiliza informações. O portal externo representa os sistemas das operadoras acessados pela automação.

Figura 1 - Diagrama de casos de uso do AutoDownload.

[Inserir aqui o diagrama de casos de uso simplificado, com fundo claro.]

Fonte: Elaborado pelo autor (2026).

Quadro 2 - Requisitos funcionais

| Código | Requisito | Situação |
| --- | --- | --- |
| RF01 | Permitir cadastro e login de usuário | Implementado |
| RF02 | Proteger rotas da API com JWT | Implementado |
| RF03 | Permitir cadastro de contas vinculadas a operadoras | Implementado |
| RF04 | Permitir edição e remoção de contas | Implementado |
| RF05 | Executar automação manual de download | Implementado |
| RF06 | Registrar histórico de execuções | Implementado |
| RF07 | Registrar boletos baixados no banco | Implementado |
| RF08 | Exibir notificações de sucesso, falha ou alterações | Implementado |
| RF09 | Permitir agendamento mensal de automação | Implementado |
| RF10 | Recuperar senha por e-mail com token | Futuro ou parcial, conforme versão final |

Fonte: Elaborado pelo autor (2026).

Quadro 3 - Requisitos não funcionais

| Código | Requisito | Tratamento no projeto |
| --- | --- | --- |
| RNF01 | Segurança de acesso | Autenticação JWT e rotas autenticadas |
| RNF02 | Proteção de senhas | Hash de senha de usuário |
| RNF03 | Proteção de credenciais externas | Criptografia das senhas dos portais |
| RNF04 | Manutenibilidade | Arquitetura em camadas e Strategy |
| RNF05 | Persistência | PostgreSQL com Entity Framework Core |
| RNF06 | Rastreabilidade | Histórico, notificações e registros de execução |
| RNF07 | Usabilidade | Interface web com dashboard, filtros e modo claro/escuro |
| RNF08 | Disponibilidade | Deploy web com frontend, backend e banco separados |

Fonte: Elaborado pelo autor (2026).

### 4.2 Arquitetura e estrutura do software

O AutoDownload foi desenvolvido como uma aplicação web composta por frontend, backend e banco de dados. O frontend, criado com Next.js e React, concentra a interface de usuário, telas de login, dashboard, contas, boletos, histórico, notificações e configurações. A comunicação com o backend ocorre por chamadas HTTP para uma API REST.

O backend foi implementado em C# com .NET 10 e organizado em camadas. A camada de API recebe requisições, valida autenticação e retorna respostas ao frontend. A camada de aplicação orquestra os casos de uso, como cadastro de contas, execução de automação e listagem de boletos. A camada de domínio concentra entidades e regras centrais. A camada de infraestrutura implementa acesso ao banco, criptografia, serviços externos e automações.

O banco de dados PostgreSQL armazena usuários, operadoras, contas, boletos, execuções e notificações. O Entity Framework Core foi utilizado como Object-Relational Mapper (ORM), permitindo mapear entidades para tabelas, criar migrations e versionar a estrutura do banco.

Figura 2 - Arquitetura geral do AutoDownload.

[Inserir aqui diagrama com: Usuário -> Frontend Next.js -> API .NET -> PostgreSQL / Selenium -> Portais externos.]

Fonte: Elaborado pelo autor (2026).

### 4.3 Modelo de dados

O modelo de dados foi definido para registrar usuários, operadoras disponíveis, contas cadastradas, boletos baixados, execuções de automação e notificações. Essa organização permite que cada usuário acesse apenas seus próprios dados e que as execuções sejam rastreadas.

Quadro 4 - Principais entidades do banco de dados

| Entidade | Finalidade |
| --- | --- |
| users | Armazena usuários da aplicação |
| operators | Armazena operadoras disponíveis para automação |
| accounts | Armazena contas cadastradas pelo usuário |
| bills | Armazena registros dos boletos baixados |
| automation_runs | Armazena histórico das execuções |
| notifications | Armazena notificações apresentadas ao usuário |

Fonte: Elaborado pelo autor (2026).

Figura 3 - Modelo de dados do AutoDownload.

[Inserir aqui DER ou diagrama de classes das entidades principais.]

Fonte: Elaborado pelo autor (2026).

### 4.4 Prototipação e interface

A prototipação foi realizada como protótipo codificado de alta fidelidade. Inicialmente, as telas foram construídas com dados simulados para validar organização visual, navegação e fluxo de uso. Posteriormente, o frontend foi integrado à API real, substituindo os dados mockados por informações vindas do banco de dados.

As principais telas implementadas foram login, cadastro de usuário, dashboard, contas cadastradas, boletos, histórico, notificações e configurações. A interface também recebeu suporte a modo claro e escuro, busca, indicadores de resumo, menu lateral e opções de perfil. Essa escolha buscou tornar a aplicação mais acessível e simples para usuários que desejam executar tarefas recorrentes com poucos cliques.

Figura 4 - Tela de dashboard do AutoDownload.

[Inserir captura da tela de visão geral.]

Fonte: Elaborado pelo autor (2026).

Figura 5 - Tela de contas cadastradas.

[Inserir captura da tela de contas.]

Fonte: Elaborado pelo autor (2026).

### 4.5 Implementação

No frontend, Next.js e React foram utilizados para construir a interface da aplicação. A comunicação com o backend é configurada por variável de ambiente, permitindo alternar entre ambiente local e ambiente publicado. As páginas autenticadas consomem endpoints da API para exibir dashboard, contas, boletos, histórico e notificações.

No backend, .NET 10 foi utilizado para criar uma API REST. A autenticação foi implementada com JWT, permitindo identificar o usuário autenticado e filtrar informações por proprietário. As senhas dos usuários são armazenadas por hash, enquanto credenciais dos portais externos são criptografadas antes da persistência. As configurações sensíveis, como connection string, chave JWT e chave de criptografia, devem permanecer em variáveis de ambiente.

As automações foram implementadas por meio de estratégias específicas. A estratégia da Vero Internet acessa o portal da operadora, realiza login, navega até a área de fatura e tenta baixar o boleto disponível. A estratégia da RMS Telecom segue lógica equivalente, adaptada ao portal correspondente. Já a estratégia demonstrativa permite simular o fluxo completo sem depender de portais externos, sendo importante para apresentações e testes controlados.

O sistema também recebeu suporte a agendamento mensal. Nessa funcionalidade, o usuário escolhe dia e horário para execução automática de determinada conta. O backend calcula a próxima execução e mantém o agendamento ativo até que o usuário opte por desativá-lo. Ao executar, o sistema registra o resultado no histórico, cria notificações e, quando houver arquivo disponível, registra o boleto baixado.

Para publicação, o frontend foi configurado para Vercel, o backend para Render com Docker e o banco para Neon. Essa arquitetura separa responsabilidades e permite que o sistema funcione como aplicação web hospedada. Entretanto, hospedagens gratuitas podem hibernar serviços, limitar recursos e não garantir persistência local de arquivos, o que deve ser considerado como limitação.

### 4.6 Segurança e privacidade

A segurança do AutoDownload foi tratada em diferentes pontos da aplicação. O login do usuário utiliza autenticação JWT para proteger rotas da API. As senhas cadastradas na aplicação não devem ser armazenadas em texto puro, sendo protegidas por hash. As credenciais utilizadas para acessar portais externos são criptografadas antes de serem salvas no banco de dados.

Também foi adotada a separação de configurações sensíveis por variáveis de ambiente, evitando que chaves, senhas e connection strings reais fiquem versionadas no repositório. Essa prática reduz o risco de exposição acidental em ambientes públicos.

Em relação à LGPD, o projeto adota práticas alinhadas a princípios como finalidade, necessidade e segurança, pois armazena apenas dados necessários ao funcionamento da aplicação e permite remoção de contas e notificações pelo usuário. Ainda assim, a conformidade completa exige evoluções, como política de privacidade formal, definição de prazo de retenção, auditoria de acessos, revisão de armazenamento do token JWT e documentação do processo de exclusão de dados.

### 4.7 Resultados e validação

Como resultado, o AutoDownload entregou um protótipo funcional com login, cadastro de contas, automação manual, agendamento mensal, histórico, notificações, listagem de boletos e download de arquivos. O dashboard apresenta indicadores de contas cadastradas, boletos baixados, execuções com sucesso, falhas e próxima execução. A tela de contas permite adicionar, editar, remover, executar e agendar automações. A tela de boletos permite consultar documentos registrados e realizar download.

A validação foi realizada por meio de testes automatizados no backend, testes manuais no frontend, execução local da aplicação e verificação de endpoints da API. Também foram realizados testes com automações reais em ambiente local e com automação demonstrativa para reduzir dependência de instabilidade externa.

Quadro 5 - Validação funcional

| Requisito | Cenário testado | Resultado esperado | Resultado obtido | Situação |
| --- | --- | --- | --- | --- |
| Login | Usuário informa credenciais válidas | Acesso liberado e token gerado | Acesso realizado | Aprovado |
| Cadastro de conta | Usuário cadastra conta vinculada a operadora | Conta aparece na listagem | Conta registrada | Aprovado |
| Edição de conta | Usuário altera dados da conta | Dados atualizados no banco | Dados persistidos | Aprovado |
| Remoção de conta | Usuário remove conta | Conta deixa de aparecer após recarregar | Remoção persistente | Aprovado |
| Notificação | Sistema gera evento de sucesso/falha | Notificação exibida ao usuário | Notificação registrada | Aprovado |
| Automação demo | Usuário executa operador demonstrativo | Boleto e histórico registrados | Fluxo demonstrado | Aprovado |
| Automação real | Usuário executa Vero ou RMS | Boleto baixado quando disponível | Dependente do portal externo | Parcial |
| Agendamento mensal | Usuário define dia e horário | Próxima execução calculada | Agendamento exibido | Aprovado |

Fonte: Elaborado pelo autor (2026).

Quadro 6 - Baseline técnico e contribuições do AutoDownload

| Item | AutoBot / referência anterior | AutoDownload |
| --- | --- | --- |
| Automação de portal | Existia como referência de automação | Adaptada para estratégias no backend |
| Interface web | Não era o foco principal | Criada com Next.js e React |
| API REST | Não centralizada no fluxo original | Implementada em .NET 10 |
| Banco de dados | Limitado ou inexistente no fluxo original | PostgreSQL com EF Core |
| Histórico | Não integrado ao sistema web | Registrado em automation_runs |
| Notificações | Não integradas | Implementadas por usuário |
| Agendamento | Não centralizado | Implementado por conta |
| Deploy web | Não era foco | Preparado com Vercel, Render e Neon |

Fonte: Elaborado pelo autor (2026).

Apesar dos resultados obtidos, algumas limitações permanecem. A primeira envolve a dependência de portais externos, que podem alterar telas, exigir CAPTCHA ou bloquear navegadores automatizados. A segunda envolve armazenamento de arquivos em hospedagens gratuitas, pois containers podem ser recriados e apagar arquivos locais. A terceira diz respeito à necessidade de ampliar testes automatizados, especialmente testes de integração e testes de automação em páginas simuladas.

## 5 CONCLUSÃO

O desenvolvimento do AutoDownload demonstrou a viabilidade de uma aplicação web voltada à automação e gestão de boletos recorrentes. O projeto partiu de um problema prático, presente na rotina de usuários que precisam acessar diferentes portais mensalmente, e resultou em uma solução com frontend, backend, banco de dados, autenticação, cadastro de contas, histórico, notificações, agendamento e automações por operadora.

Do ponto de vista técnico, o projeto permitiu aplicar conceitos relevantes de engenharia de software, como arquitetura em camadas, padrões Repository e Strategy, injeção de dependência, persistência com Entity Framework Core, autenticação JWT, criptografia de credenciais, migrations, seed de dados, testes automatizados e deploy em ambiente web. A utilização de princípios de Design Thinking contribuiu para manter o foco na dor do usuário e orientar decisões de interface e funcionalidades.

Os resultados indicam que a solução pode reduzir tarefas repetitivas e melhorar a organização de boletos, especialmente quando utilizada com automação demonstrativa ou com portais estáveis. Entretanto, automações reais dependem do comportamento de sistemas externos, o que exige manutenção contínua, logs claros e estratégias de contingência. Também é necessário evoluir aspectos de segurança, documentação, testes, publicação e armazenamento persistente de arquivos.

Como trabalhos futuros, recomenda-se ampliar a cobertura de testes, criar um portal simulado para validação controlada das automações, integrar armazenamento de arquivos em serviço externo, melhorar observabilidade em produção, documentar políticas de privacidade e retenção de dados, criar release acadêmica do repositório e incluir novas operadoras. Com essas evoluções, o AutoDownload pode tornar-se uma solução mais robusta, demonstrável e alinhada às exigências acadêmicas e práticas do Projeto de Desenvolvimento de Software.

## REFERÊNCIAS

AUTOBOT. Repositório AutoBot. Disponível em: https://github.com/Edyeex/AutoBot. Acesso em: 25 jun. 2026.

BRASIL. Lei n. 13.709, de 14 de agosto de 2018. Lei Geral de Proteção de Dados Pessoais (LGPD). Brasília, DF: Presidência da República, 2018. Disponível em: https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm. Acesso em: 25 jun. 2026.

BROWN, Tim. Design Thinking. Harvard Business Review, 2008.

GIL, Antonio Carlos. Métodos e técnicas de pesquisa social. 7. ed. São Paulo: Atlas, 2017.

MICROSOFT. ASP.NET Core documentation. Disponível em: https://learn.microsoft.com/aspnet/core/. Acesso em: 25 jun. 2026.

MICROSOFT. Entity Framework Core documentation. Disponível em: https://learn.microsoft.com/ef/core/. Acesso em: 25 jun. 2026.

NORMAN, Donald A. O design do dia a dia. Rio de Janeiro: Rocco, 2006.

PRESSMAN, Roger S.; MAXIM, Bruce R. Engenharia de software: uma abordagem profissional. 8. ed. Porto Alegre: AMGH, 2016.

SELENIUM. Selenium WebDriver documentation. Disponível em: https://www.selenium.dev/documentation/webdriver/. Acesso em: 25 jun. 2026.

SOMMERVILLE, Ian. Engenharia de software. 10. ed. São Paulo: Pearson, 2019.

VERCEL. Next.js on Vercel. Disponível em: https://vercel.com/docs/frameworks/nextjs. Acesso em: 25 jun. 2026.

## APÊNDICES SUGERIDOS

APÊNDICE A - Artefatos de Design Thinking: persona, jornada do usuário e definição do problema.

APÊNDICE B - Diagrama de casos de uso revisado.

APÊNDICE C - Diagrama de arquitetura e modelo de dados.

APÊNDICE D - Evidências de testes: prints, comandos executados, resultados do xUnit e validação de API.

APÊNDICE E - Capturas das principais telas do sistema.

APÊNDICE F - Registro da versão acadêmica: tag, hash do commit e link do repositório.
