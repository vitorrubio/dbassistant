# Regras gerais para desenvolvimento de software na VLR ME

## Role
Você é um programador Sr. desenvolvendo um sistema.


## Setup Inicial
1. Crie um arquivo .gitignore e coloque a lista mais comum de ignores do C# / .net core
2. Crie um arquivo .env, ele deve ser ignorado no .gitignore
3. Crie um arquivo .env_copy-me que será um template do .env, esse não deve ser ignorado pelo git


## Arquitetura

### Camadas
 0. Domain (aqui ficam as classes de entidades de domínio e interfaces dos repositórios)
 1. Data (aqui ficam as classes de implementação dos repositórios e o(s) DbContext(s))
 2. Services (qualquer serviço externo às fronteiras dessa aplicação: web apis de terceiros, integrações etc)
 3. UseCases (os casos de uso da aplicação, que vão usar services e domain para de fato executar as ações)
 4. Api web (restful e bem documentada)


## Tecnologia

### Backend
- Use DotNet core última versão
- use docker
- use docker compose

### Database
- Use MySql (a connectionstring estará no arquivo .env)


## Workflow

### A cada feature que for implementar utilize o seguinte workflow:
 - crie uma branch a partir da main com o nome da feature, prefixada com feature/ se for feature ou com fix/ se for fix
 - Planeje primeiro, salvando o .md com seu planejamento na pasta da sua memória .md
 - Crie prompts para si mesmo ou para outros agentes na pasta .agents para sua memória
 - a cada iteração faça um commit bem explicado e formatado (em inglês) e um PR igualmente bem formatado em inglês, para a main. Adicione também no changelog do readme, em inglês.
 - o PR precisa de duas aprovações para que seja feito o merge. Inicie um sub agente para fazer o code review e a primeira aprovação. Só eu posso fazer a segunda aprovação e o merge, a não ser que te autorize especificamente.


## Rules
1. Use DDD (Domain Driven Design)
2. Use Clean Arch
3. Não coloque código de obtenção de dados nos controllers
4. Use nomes claros
5. Use os princípios SOLID
6. Classes devem ter uma responsabilidade única
7. Controllers devem ser magros. 
8. Cada classe em um arquivo diferente
9. Nomes de propriedades e classes em PascalCase, nomes de constante em CAPSLOCK, nome de variáveis locais em camelCase, nome de fields privados em _camelCase precedido de _camelCase
10. Um diretório para cada camada
11. Não use magic numbers, mas constantes bem nomeadas.
12. Não use magic strings, mas constantes bem nomeadas.
13. Desenvolva orientado a interfaces
14. Use IoC/Injeção de Dependência
15. Crie um diretório .agents e coloque dentro dele todos os seus arquivos .md de planejamento e memória. Organize-os de forma que reflita a organização do próprio sistema.
16. O readme.md deve ser em inglês e ter 3 seções principais:
	- Nome, visão geral e objetivo do projeto
	- Instruções e comandos para clonar, instalar e rodar/depurar localmente
	- abaixo de tudo deve ter um changelog em ordem decrescente
17. Cada feature ou bugfix deve gerar um commit (em inglês) bem explicado e formatado e um PR (em inglês) igualmente bem explicado e formatado. Uma descrição resumida deve ir para o changelog do readme.md (em inglês)
18. O domínio sempre deve ter um projeto de testes unitários
19. A camada de Application (ou UseCase) deve ter uma bateria de testes de integração
20. A camada de api deve ter uma bateria de testes de integração usando TestServer
21. Não tome decisões arquiteturais ou de negócio sem antes perguntar
22. Ao preencher o .env pergunte sobre credenciais e url's

## Especificação

### Início
1. Leia o arquivo @DBASSISTANT.md
2. Mantenha na pasta .agents todos os arquivos para suas memórias, todo-list, internal chores e marque as tasks já terminadas para não desperdiçar tokens voltando nelas. 
3. Leia o arquivo @ACCEPTANCE.md e crie uma bateria de testes de integração que execute todos esses testes. 