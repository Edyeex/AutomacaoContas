# AUTODOWNLOAD: Desenvolvimento de uma Aplicação Web para Automação e Gestão de Boletos Recorrentes

Éder Casagranda¹  
Lucas Fogaça²

¹ Acadêmico do curso de Análise e Desenvolvimento de Sistemas da Universidade Luterana do Brasil - ULBRA.  
² Professor orientador do curso de Análise e Desenvolvimento de Sistemas da Universidade Luterana do Brasil - ULBRA.

## RESUMO

O presente artigo apresenta o desenvolvimento do AutoDownload, uma aplicação web voltada à automação do acesso a portais de operadoras e à gestão de boletos recorrentes. O problema abordado está relacionado à necessidade de muitos usuários acessarem mensalmente diferentes sites para localizar, baixar e organizar contas de internet, água, energia ou outros serviços, o que gera perda de tempo, risco de esquecimento de vencimentos e dificuldade de controle financeiro. O objetivo do projeto foi desenvolver uma solução capaz de centralizar o cadastro de contas, executar automações de download, armazenar registros no banco de dados e apresentar ao usuário histórico, notificações e arquivos baixados. A metodologia adotada foi aplicada, de caráter qualitativo e descritivo, combinando princípios de Design Thinking com desenvolvimento incremental de software. O processo envolveu levantamento de requisitos, definição de personas, prototipação, modelagem, implementação e validação funcional. A aplicação foi construída com frontend em Next.js e React, backend em .NET 10, banco PostgreSQL, Entity Framework Core, autenticação JWT, criptografia de credenciais e estratégias de automação com Selenium. Como resultado, obteve-se um sistema web funcional, com login, cadastro de contas, automações por operadora, histórico de execuções, notificações, agendamento mensal e download de boletos. Conclui-se que o AutoDownload contribui para reduzir tarefas repetitivas e organizar contas digitais, embora ainda dependa da estabilidade dos portais externos e de cuidados contínuos com segurança, privacidade e validação em ambiente de produção.

**Palavras-chave:** Automação web. Boletos recorrentes. Segurança da informação. PostgreSQL. Design Thinking.

## 1 INTRODUÇÃO

A digitalização de serviços financeiros, de telecomunicações e de utilidades públicas trouxe praticidade para o acesso a contas e documentos de cobrança. Entretanto, essa digitalização também fragmentou a rotina dos usuários, que frequentemente precisam acessar vários portais diferentes para consultar faturas, gerar segunda via, baixar boletos e organizar arquivos em seus dispositivos. Embora cada portal resolva uma parte do processo, a experiência mensal de quem precisa controlar diferentes contas continua sendo repetitiva, manual e sujeita a falhas.

No contexto doméstico e acadêmico, é comum que usuários tenham contas de internet, energia, água, telefone ou outros serviços recorrentes. Para cada uma delas, normalmente é necessário entrar em um site específico, informar login e senha, navegar até a área de faturas, localizar o boleto disponível, realizar o download e depois armazenar o arquivo em uma pasta adequada. Quando esse processo não é realizado com frequência, podem ocorrer atrasos de pagamento, perda de boletos, desorganização de arquivos e dependência de lembretes manuais.

Diante desse cenário, o projeto AutoDownload foi proposto como uma aplicação web para automatizar parte desse fluxo. A ideia central consiste em permitir que o usuário cadastre contas de operadoras, mantenha credenciais protegidas, execute automações para buscar boletos disponíveis e acompanhe tudo por meio de uma interface web com histórico, notificações e área de download. Dessa forma, o sistema busca transformar uma tarefa recorrente e dispersa em um processo centralizado e rastreável.

O problema de pesquisa que orienta este trabalho pode ser formulado da seguinte maneira: como uma aplicação web, apoiada em automação de navegação, boas práticas de engenharia de software e mecanismos de segurança, pode reduzir o esforço manual necessário para obter e organizar boletos recorrentes?

O objetivo geral deste artigo é apresentar o desenvolvimento do AutoDownload, descrevendo sua concepção, arquitetura, tecnologias utilizadas, funcionalidades implementadas e resultados obtidos. Como objetivos específicos, destacam-se: identificar as necessidades do usuário no gerenciamento de boletos recorrentes; projetar uma aplicação web com autenticação, cadastro de contas e organização de boletos; implementar um backend estruturado em camadas com persistência em PostgreSQL; desenvolver estratégias de automação para operadoras; registrar histórico e notificações; e validar o funcionamento da solução por meio de testes e execução controlada.

A justificativa do projeto está relacionada à relevância prática do problema. Apesar de existirem aplicativos bancários e portais individuais das operadoras, muitos usuários ainda precisam realizar manualmente o processo de acesso e download de contas. Uma solução centralizada pode economizar tempo, reduzir esquecimentos e melhorar a organização financeira. Além disso, o projeto possui aderência ao curso de Análise e Desenvolvimento de Sistemas por envolver levantamento de requisitos, modelagem, frontend, backend, banco de dados, segurança, testes, deploy e integração com serviços externos.

Este artigo está organizado em cinco seções. A primeira apresenta a introdução, o problema, os objetivos e a justificativa. A segunda discute os fundamentos teóricos relacionados à automação web, sistemas de informação, segurança e Design Thinking. A terceira descreve a metodologia adotada. A quarta apresenta o desenvolvimento do sistema, incluindo modelagem, arquitetura, implementação e resultados. Por fim, a quinta seção traz as conclusões, limitações e possibilidades de evolução.

## 2 FUNDAMENTAÇÃO TEÓRICA

### 2.1 Sistemas de informação e automação de processos

Sistemas de informação são utilizados para coletar, processar, armazenar e disponibilizar dados de forma organizada, apoiando a tomada de decisão e a execução de atividades cotidianas. Em aplicações web modernas, esses sistemas frequentemente integram interfaces de usuário, regras de negócio, bancos de dados e serviços externos. No caso do AutoDownload, o sistema de informação tem como foco o controle de contas cadastradas, boletos baixados, histórico de execuções e notificações.

A automação de processos busca reduzir a necessidade de intervenção humana em atividades repetitivas. No contexto deste trabalho, a atividade automatizada é o acesso a portais de operadoras para localização e download de boletos. Esse tipo de automação pode ser realizado por meio de ferramentas capazes de controlar navegadores, preencher formulários, clicar em botões e capturar arquivos gerados pelo site. Entre as ferramentas utilizadas para esse fim está o Selenium WebDriver, que permite a automação de navegadores de forma programável.

Apesar dos benefícios, a automação web apresenta desafios importantes. Portais externos podem alterar sua estrutura visual, modificar identificadores de campos, inserir mecanismos de proteção ou exigir validações adicionais. Por isso, uma aplicação desse tipo precisa ser projetada com estratégias independentes por operadora, tratamento de falhas, logs e possibilidade de manutenção contínua. Essa necessidade influenciou diretamente a adoção do padrão Strategy no AutoDownload, permitindo que cada operadora tenha sua própria lógica de execução.

### 2.2 Segurança da informação e LGPD

Aplicações que manipulam dados pessoais e credenciais de acesso precisam considerar princípios de segurança desde as primeiras etapas de projeto. No AutoDownload, o usuário informa dados de login de portais externos, como CPF, identificador de cliente ou senha de acesso. Essas informações são sensíveis e não devem ser armazenadas em texto puro.

A segurança da informação envolve práticas como autenticação, autorização, criptografia, controle de acesso e proteção contra exposição indevida de dados. No projeto, as senhas dos usuários da aplicação são armazenadas por meio de hash seguro, enquanto as credenciais usadas nos portais das operadoras são protegidas com criptografia simétrica. Além disso, o acesso à API é protegido por autenticação JWT, permitindo que as rotas autenticadas identifiquem o usuário e retornem apenas os dados pertencentes à sua conta.

A Lei Geral de Proteção de Dados Pessoais, Lei nº 13.709/2018, estabelece princípios para tratamento de dados pessoais no Brasil, incluindo finalidade, necessidade, transparência e segurança. Assim, o AutoDownload deve limitar o armazenamento de dados ao que é necessário para funcionamento da aplicação, proteger credenciais, evitar exposição em repositórios públicos e permitir que dados sejam atualizados ou removidos quando solicitado pelo usuário.

### 2.3 Engenharia de software e arquitetura em camadas

A engenharia de software fornece métodos, práticas e padrões para construção de sistemas com qualidade, manutenibilidade e evolução controlada. Em projetos web, a separação de responsabilidades é uma prática essencial para evitar acoplamento excessivo entre interface, regras de negócio, persistência e serviços externos.

No AutoDownload, o backend foi organizado em camadas, separando domínio, aplicação, infraestrutura e API. A camada de domínio concentra entidades e conceitos centrais, como usuário, conta, operadora, boleto, execução de automação e notificação. A camada de aplicação contém serviços e contratos responsáveis por orquestrar regras de negócio. A infraestrutura implementa persistência, automações, criptografia e integrações. A API expõe endpoints REST para consumo pelo frontend.

Essa organização favorece princípios SOLID, especialmente responsabilidade única, inversão de dependência e abertura para extensão. A adoção de Repository permite isolar o acesso ao banco de dados; o padrão Strategy permite variar a automação conforme a operadora; e a injeção de dependência simplifica a substituição de implementações e os testes.

### 2.4 Design Thinking no levantamento de requisitos

O Design Thinking é uma abordagem voltada à solução de problemas com foco nas necessidades reais dos usuários. Ele é frequentemente associado às etapas de empatia, definição, ideação, prototipação e teste. Embora não substitua práticas tradicionais de engenharia de software, pode contribuir para compreender dores do usuário e transformar necessidades em funcionalidades.

No AutoDownload, ideias de Design Thinking foram utilizadas principalmente na fase inicial do projeto. A etapa de empatia envolveu observar a dificuldade de usuários ao lidar com múltiplos portais e boletos mensais. A etapa de definição permitiu delimitar o problema central: reduzir o esforço manual para localizar, baixar e organizar boletos recorrentes. Na ideação, foram consideradas soluções como painel centralizado, cadastro de contas, automações por operadora, notificações e histórico. A prototipação foi realizada por meio de telas web em Next.js, e a fase de teste ocorreu com validações manuais, testes automatizados e execução de automações controladas.

## 3 METODOLOGIA

A pesquisa realizada neste trabalho é classificada como aplicada, pois busca resolver um problema prático relacionado à automação do download de boletos recorrentes. Quanto à abordagem, possui caráter qualitativo, uma vez que analisa necessidades, fluxos de uso, requisitos e validação funcional do sistema. Quanto aos objetivos, trata-se de uma pesquisa descritiva e exploratória, pois descreve o desenvolvimento da aplicação e explora a viabilidade técnica de automações em portais externos.

A população considerada no projeto é composta por usuários que precisam gerenciar contas recorrentes de diferentes operadoras, como internet, energia, água ou serviços similares. A amostra foi definida de forma intencional, considerando o perfil de usuário comum, com pouco interesse em processos técnicos e maior necessidade de praticidade. Esse perfil orientou decisões de interface, como painel com indicadores, cadastro simplificado de contas, histórico, notificações e botão de execução da automação.

A coleta de dados ocorreu a partir de documentos de proposta do projeto, levantamento de requisitos funcionais e não funcionais, análise do protótipo, observação do fluxo manual de obtenção de boletos e testes práticos das funcionalidades implementadas. Também foram consideradas recomendações recebidas durante a avaliação do projeto, especialmente em relação à necessidade de demonstração ponta a ponta, melhoria da documentação, segurança, testes e separação clara entre automações reais e automação demonstrativa.

A análise dos dados foi conduzida por meio da transformação das necessidades levantadas em requisitos do sistema. Os requisitos funcionais foram relacionados às funcionalidades implementadas, como autenticação, cadastro de contas, download de boletos, histórico e notificações. Os requisitos não funcionais foram associados a decisões de arquitetura, segurança, usabilidade, disponibilidade e manutenção.

O processo de desenvolvimento combinou Design Thinking com ciclo incremental de software. Inicialmente, foram identificadas as dores do usuário e definida a proposta de valor. Em seguida, foram criadas telas de protótipo para validar o fluxo de navegação. Depois, a solução foi implementada em incrementos, começando pela estrutura do backend, autenticação, persistência, integração com frontend, cadastro de contas, boletos, notificações, histórico, automações e agendamento mensal.

As principais etapas metodológicas foram:

1. Levantamento e análise de requisitos, com definição do problema, objetivos, atores e funcionalidades esperadas.
2. Modelagem do sistema, com identificação de casos de uso, entidades principais e fluxo de automação.
3. Prototipação da interface web, com telas para login, dashboard, contas, boletos, histórico, notificações e configurações.
4. Implementação incremental, utilizando Next.js no frontend, .NET 10 no backend e PostgreSQL como banco de dados.
5. Validação funcional, por meio de testes unitários, testes de integração parcial, execução local, health check da API e testes controlados de automação.
6. Preparação para deploy, com frontend publicado na Vercel, backend em Docker no Render e banco PostgreSQL no Neon.

## 4 DESENVOLVIMENTO

### 4.1 Modelagem do sistema

#### 4.1.1 Análise

A análise do AutoDownload partiu da rotina de usuários que precisam acessar diferentes portais para baixar boletos. A partir dessa observação, foram identificados os principais atores do sistema: usuário, sistema AutoDownload e operadora externa. O usuário é responsável por realizar cadastro, autenticar-se, cadastrar contas, configurar automações, visualizar boletos, consultar histórico e receber notificações. O sistema é responsável por armazenar dados, proteger credenciais, executar automações e registrar resultados. A operadora externa representa os portais acessados para obtenção dos boletos.

Os requisitos funcionais definidos para o sistema incluem cadastro de usuário, autenticação, cadastro de contas, armazenamento seguro de credenciais, automação de login em operadoras, download de boletos, consulta de histórico, listagem de boletos anteriores, envio de notificações e gerenciamento de contas. Entre os requisitos não funcionais, destacam-se criptografia de credenciais, hash de senhas, conformidade com princípios da LGPD, interface intuitiva, logs de execução, disponibilidade e possibilidade de execução periódica das automações.

O escopo também definiu algumas restrições. O sistema limita a quantidade de contas cadastradas por usuário, depende da disponibilidade e estrutura dos portais externos e pode ser impactado por mecanismos como CAPTCHA, bloqueios de segurança ou alterações de layout. Por isso, foi criada uma automação demonstrativa, capaz de simular o fluxo completo sem depender de portais reais, além das estratégias reais para operadoras específicas.

#### 4.1.2 Diagramas

O diagrama de casos de uso representa as interações principais entre usuário, AutoDownload e operadoras externas. Entre os casos de uso estão: fazer cadastro, fazer login, recuperar senha, cadastrar conta, editar conta, remover conta, iniciar automação, realizar login na operadora, buscar boleto, baixar boleto, visualizar boletos, consultar histórico e receber notificações.

[Inserir Figura 1 - Diagrama de casos de uso do AutoDownload]

Além do diagrama de casos de uso, o projeto pode ser complementado por um diagrama de arquitetura, representando a comunicação entre frontend, backend, banco de dados e portais externos. O frontend consome a API REST por meio de requisições HTTP autenticadas. O backend processa regras de negócio, acessa o PostgreSQL via Entity Framework Core e aciona estratégias de automação quando necessário. As automações utilizam navegador controlado para acessar portais externos e salvar boletos encontrados.

[Inserir Figura 2 - Arquitetura geral da aplicação]

#### 4.1.3 Personas

Uma persona definida para o projeto é o usuário doméstico que possui várias contas mensais e deseja reduzir o esforço de acessar portais diferentes. Esse usuário utiliza computador ou celular para tarefas cotidianas, mas não deseja lidar com procedimentos técnicos. Suas dores incluem esquecer vencimentos, perder arquivos baixados, ter dificuldade para lembrar senhas de portais e precisar repetir o mesmo processo todos os meses.

Outra persona possível é o usuário responsável por organizar contas de familiares ou de uma pequena residência. Nesse caso, o valor do sistema está na centralização das informações, no histórico de downloads e nas notificações sobre falhas ou sucesso das automações. Essas personas orientaram a criação de uma interface simples, com navegação lateral, indicadores de resumo e ações diretas para executar, editar ou remover contas.

### 4.2 Estrutura do software

#### 4.2.1 Projeto

O AutoDownload foi desenvolvido como uma aplicação web dividida em frontend, backend e banco de dados. O frontend foi construído com Next.js e React, oferecendo telas de autenticação, painel geral, contas cadastradas, boletos, histórico, notificações e configurações. A interface possui suporte a modo claro e escuro, indicadores de quantidade de contas e boletos, além de uma área de perfil do usuário.

O backend foi construído com .NET 10 e organizado em camadas. A API expõe endpoints REST para autenticação, dashboard, contas, boletos, histórico, notificações e automações. A camada de aplicação contém serviços responsáveis por regras de negócio, enquanto a infraestrutura implementa persistência com Entity Framework Core, acesso ao PostgreSQL, proteção de credenciais e estratégias de automação.

O banco de dados PostgreSQL armazena entidades como usuários, operadoras, contas, boletos, execuções de automação e notificações. O uso do Entity Framework Core permitiu criar migrations, versionar a estrutura do banco e executar seed com dados de exemplo para demonstração.

#### 4.2.2 Diagramas e arquitetura

A arquitetura do sistema foi pensada para separar responsabilidades e facilitar manutenção. O frontend não acessa diretamente o banco de dados; toda comunicação ocorre por meio da API. O backend concentra a autenticação, autorização, validação, persistência e execução das automações. As estratégias de automação são desacopladas do restante da aplicação, permitindo que novas operadoras sejam adicionadas sem alterar profundamente os serviços principais.

A estrutura lógica pode ser representada em quatro blocos:

1. Interface web: telas em Next.js e React.
2. API: endpoints em .NET 10 com autenticação JWT.
3. Aplicação e domínio: serviços, entidades, regras de negócio e contratos.
4. Infraestrutura: PostgreSQL, Entity Framework Core, criptografia, Selenium e integrações externas.

[Inserir Figura 3 - Diagrama de camadas do backend]

#### 4.2.3 Prototipação

A prototipação foi realizada diretamente na aplicação frontend. Inicialmente, as telas foram criadas com dados simulados para validar a navegação e a apresentação das informações. Posteriormente, o frontend foi integrado à API real, substituindo dados mockados por respostas vindas do backend.

As principais telas desenvolvidas foram:

1. Login e cadastro de usuário.
2. Dashboard com indicadores de contas cadastradas, boletos baixados, execuções e próxima execução.
3. Tela de contas, com opção de adicionar, editar, remover, executar automação e configurar agendamento mensal.
4. Tela de boletos, com filtros, valor, vencimento, referência e botão de download.
5. Tela de histórico, exibindo execuções, status e mensagens.
6. Tela de notificações, permitindo marcar como lidas ou remover notificações.
7. Tela de configurações e perfil, incluindo preferências visuais.

[Inserir Figura 4 - Protótipo do dashboard]

### 4.3 Implementação

A implementação do AutoDownload utilizou tecnologias modernas de desenvolvimento web. No frontend, Next.js 16 e React foram empregados para construir a interface e consumir a API. O layout foi organizado com componentes reutilizáveis, navegação lateral, cabeçalho, cards informativos, tabelas e formulários. A comunicação com o backend é feita por meio da variável de ambiente `NEXT_PUBLIC_API_URL`, permitindo alternar entre ambiente local e produção.

No backend, .NET 10 foi utilizado para criar uma API REST. A autenticação foi implementada com JWT, permitindo que usuários autenticados acessem apenas seus próprios dados. As senhas de usuários são armazenadas com hash, e as credenciais de portais externos são criptografadas antes de serem persistidas. As chaves sensíveis são configuradas por variáveis de ambiente, evitando que valores reais sejam versionados no repositório.

O Entity Framework Core foi adotado como ORM para comunicação com o PostgreSQL. O banco é versionado por migrations e possui seed com dados de exemplo. As principais tabelas são:

1. `users`: usuários da aplicação.
2. `operators`: operadoras disponíveis.
3. `accounts`: contas cadastradas pelos usuários.
4. `bills`: boletos encontrados e baixados.
5. `automation_runs`: histórico de execuções das automações.
6. `notifications`: notificações geradas para o usuário.

As automações foram implementadas por meio de estratégias específicas. O padrão Strategy permite que cada operadora tenha uma classe própria para realizar login, navegar até a seção correta e tentar baixar o boleto disponível. Foram criadas estratégias para Vero Internet, RMS Telecom e uma automação demonstrativa. A automação demonstrativa é importante para apresentações e testes controlados, pois permite validar o fluxo completo mesmo quando portais externos estiverem indisponíveis.

Durante o desenvolvimento, também foi utilizada como referência técnica uma automação anterior disponível no repositório AutoBot, adaptando conceitos ao novo backend, ao registro em banco de dados e à interface web do AutoDownload. Essa adaptação reforça a separação entre baseline técnico e funcionalidades implementadas especificamente neste projeto, como integração com PostgreSQL, autenticação JWT, dashboard, notificações, histórico e agendamento.

O sistema também recebeu suporte a agendamento mensal. Essa funcionalidade permite que o usuário escolha dia e horário para que uma conta seja executada automaticamente de forma recorrente. A próxima execução é calculada a partir da última execução ou da configuração do usuário, e o agendamento permanece ativo até que o usuário decida desativá-lo.

Para publicação, o frontend foi preparado para deploy na Vercel. O backend foi configurado com Docker para execução no Render, incluindo dependências necessárias para automação com Chromium e ChromeDriver. O banco PostgreSQL pode ser hospedado no Neon. Essa divisão permite que o sistema funcione como aplicação web hospedada, em vez de depender apenas do ambiente local.

### 4.4 Resultado

#### 4.4.1 Descrição das funcionalidades

O AutoDownload alcançou um conjunto de funcionalidades que permite demonstrar o fluxo principal da solução. O usuário pode criar uma conta, realizar login, cadastrar operadoras, informar credenciais de acesso, executar automações, visualizar histórico, receber notificações e acessar boletos baixados.

No dashboard, são exibidos indicadores como quantidade de contas cadastradas, boletos baixados, execuções com sucesso, falhas e próxima execução. A tela de contas apresenta cada conta cadastrada com dados da operadora, login, identificador, status e última execução. A partir dela, o usuário pode executar manualmente uma automação ou configurar execução mensal automática.

A tela de boletos lista os documentos registrados no banco, exibindo operadora, referência, vencimento, valor, data de download e botão para baixar o arquivo. A tela de histórico mostra execuções anteriores, incluindo status de sucesso, falha ou indisponibilidade. A tela de notificações informa eventos relevantes, como falha de login, boleto baixado ou alteração em uma conta.

A aplicação também permite remover contas e notificações de forma persistente. Isso significa que os dados deixam de aparecer após recarregar a página, pois as alterações são refletidas no banco de dados e não apenas na interface.

#### 4.4.2 Validação e avaliação

A validação do projeto ocorreu por meio de testes de build, testes automatizados e uso manual da aplicação. O backend possui testes com xUnit para validar regras e serviços essenciais. Também foram realizados testes locais executando frontend e backend juntos, além de validações de API por meio de endpoint de saúde.

Em ambiente de produção, a publicação foi estruturada com Vercel para o frontend, Render para o backend e Neon para o banco PostgreSQL. O endpoint de saúde da API permite verificar se o backend está ativo. A automação demonstrativa foi criada para reduzir dependência de portais externos durante apresentações, enquanto as automações reais continuam sujeitas a limitações dos sites de operadoras.

O quadro a seguir relaciona alguns requisitos às funcionalidades implementadas e evidências esperadas.

| Requisito | Funcionalidade implementada | Evidência sugerida |
|---|---|---|
| RF01 - Cadastro de usuário | Tela e endpoint de registro | Print da tela de cadastro e registro no banco |
| RF02 - Autenticação | Login com JWT | Token gerado e acesso ao dashboard |
| RF03 - Cadastro de contas | CRUD de contas | Tela de contas e tabela `accounts` |
| RF04 - Credenciais seguras | Criptografia de credenciais externas | Configuração de chave e dados protegidos |
| RF05 - Automação de login | Estratégias por operadora | Histórico de execução |
| RF06 - Download de boletos | Registro e download de boleto | Tabela `bills` e arquivo baixado |
| RF07 - Histórico | Tela de histórico | Registros em `automation_runs` |
| RF08 - Consulta de boletos | Tela de boletos | Listagem com valor, vencimento e arquivo |
| RF09 - Notificações | Tela e contador de notificações | Registros em `notifications` |
| RF10 - Gerenciamento de contas | Editar, remover e agendar conta | Operações refletidas após recarregar |

Embora o sistema esteja funcional, algumas limitações permanecem. A primeira está relacionada à dependência de portais externos, que podem alterar suas telas ou bloquear automações. A segunda envolve execução em hospedagens gratuitas, que podem hibernar serviços, limitar recursos ou não oferecer armazenamento persistente adequado. A terceira diz respeito à necessidade de ampliar testes automatizados, principalmente testes de integração e testes controlados de automação em páginas simuladas.

## 5 CONCLUSÃO

O desenvolvimento do AutoDownload demonstrou a viabilidade de uma aplicação web voltada à automação e gestão de boletos recorrentes. O projeto partiu de um problema prático, presente na rotina de usuários que precisam acessar diferentes portais mensalmente, e resultou em uma solução com frontend, backend, banco de dados, autenticação, cadastro de contas, histórico, notificações, download de boletos e automações por operadora.

Do ponto de vista técnico, o projeto permitiu aplicar conceitos relevantes de engenharia de software, como separação em camadas, padrões Repository e Strategy, injeção de dependência, persistência com Entity Framework Core, autenticação JWT, criptografia de credenciais, migrations, seed de dados, testes automatizados e deploy em ambiente web. A utilização de Design Thinking contribuiu para manter o foco na dor do usuário e orientar decisões de interface e funcionalidades.

Os resultados indicam que a solução pode reduzir tarefas repetitivas e melhorar a organização de boletos, especialmente quando utilizada com automação demonstrativa ou com portais estáveis. Entretanto, automações reais dependem do comportamento de sistemas externos, o que exige manutenção contínua, logs claros e estratégias de contingência. Também é necessário evoluir aspectos de segurança, documentação, testes, publicação e armazenamento persistente de arquivos.

Como trabalhos futuros, recomenda-se ampliar a cobertura de testes, criar um portal simulado para validação das automações, integrar armazenamento de arquivos em serviço externo, melhorar a observabilidade em produção, documentar políticas de privacidade e retenção de dados, além de incluir novas operadoras. Com essas evoluções, o AutoDownload pode se tornar uma solução mais robusta, demonstrável e alinhada às exigências acadêmicas e práticas do projeto.

## REFERÊNCIAS

BRASIL. Lei nº 13.709, de 14 de agosto de 2018. Lei Geral de Proteção de Dados Pessoais (LGPD). Brasília, DF: Presidência da República, 2018. Disponível em: https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm. Acesso em: 23 jun. 2026.

BROWN, Tim. Design Thinking. Harvard Business Review, 2008.

GIL, Antonio Carlos. Métodos e técnicas de pesquisa social. 7. ed. São Paulo: Atlas, 2017.

MICROSOFT. ASP.NET Core documentation. Disponível em: https://learn.microsoft.com/aspnet/core/. Acesso em: 23 jun. 2026.

MICROSOFT. Entity Framework Core documentation. Disponível em: https://learn.microsoft.com/ef/core/. Acesso em: 23 jun. 2026.

NORMAN, Donald A. O design do dia a dia. Rio de Janeiro: Rocco, 2006.

PRESSMAN, Roger S.; MAXIM, Bruce R. Engenharia de software: uma abordagem profissional. 8. ed. Porto Alegre: AMGH, 2016.

SELENIUM. Selenium WebDriver documentation. Disponível em: https://www.selenium.dev/documentation/webdriver/. Acesso em: 23 jun. 2026.

SOMMERVILLE, Ian. Engenharia de software. 10. ed. São Paulo: Pearson, 2019.

VERCEL. Next.js on Vercel. Disponível em: https://vercel.com/docs/frameworks/nextjs. Acesso em: 23 jun. 2026.

## APÊNDICE A - Sugestão de figuras e evidências para inserir no artigo

1. Diagrama de casos de uso do AutoDownload.
2. Diagrama da arquitetura frontend, backend, banco e portais externos.
3. Print da tela de login.
4. Print do dashboard.
5. Print da tela de contas cadastradas.
6. Print da tela de boletos.
7. Print da tela de histórico.
8. Print da tela de notificações.
9. Print do banco PostgreSQL com tabelas principais.
10. Print do endpoint `/api/health` em produção.

## APÊNDICE B - Observações para revisão final

1. Confirmar e-mail institucional do autor e do orientador.
2. Confirmar nome completo, titulação e forma correta de identificação do orientador.
3. Inserir as figuras no padrão exigido pela instituição.
4. Ajustar citações conforme normas ABNT solicitadas pelo professor.
5. Atualizar a seção de resultados com prints finais do deploy.
6. Informar claramente quais automações são reais e qual automação é demonstrativa.
7. Revisar a política de segurança e LGPD antes da entrega final.
