# DB Assistant
O nome do projeto que estamos criando é DB Assistant. 

## Problema 
Percebemos que a FinTechX, uma empresa líder no setor financeiro, está
enfrentando desafios em manter sua posição de destaque no mercado de vendas
digitais.
Clientes e empresas em busca de soluções mais eficientes, consideram que a
falta de personalização no atendimento, a complexidade dos processos e a
ausência de ferramentas de análise preditiva - são grandes obstáculos na jornada
atual, além dos custos elevados associados aos serviços da FinTechX em relação
aos concorrentes.
Temos a oportunidade de oferecer soluções inovadoras que capacitem
clientes e empresas a realizar vendas de forma mais ágil e eficiente. Acreditamos
que ninguém melhor do que você pode nos ajudar a superar esse desafio. 

## Desafio
Desenvolvimento de uma API baseada em LLM para impulsionar a eficiência
analítica e a tomada de decisões na FinTechX.
A FinTechX, referência no setor financeiro, vem enfrentando desafios
significativos na utilização de dados para decisões estratégicas. A ausência de
ferramentas inteligentes para análise preditiva, aliada à complexidade na
extração de informações relevantes, têm limitado a eficiência operacional da
empresa e sua competitividade frente ao mercado.

## Proposta
Para resolver esse problema, propomos o desenvolvimento de uma aplicação
moderna baseada em Modelos de Linguagem (LLMs), capaz de compreender
perguntas em linguagem natural e transformá-las em consultas SQL otimizadas,
que extraem dados diretamente do banco de forma precisa. Combinando
técnicas como engenharia de prompt, function calling, RAG
(retrieval-augmented generation), busca vetorial, cache inteligente e
validação semântica de SQL, a solução busca oferecer respostas
contextualizadas, explicadas e confiáveis.
- Prompt Engineering
- Function Calling
- Guardrails
- Busca vetorial
- Cache inteligente
- RAG

## Projeto
- Faremos um agente de IA utilizando a api da OpenAI onde a única tool disponível para esse agente é o acesso ao banco de dados da empresa FinTechX (northwind no MySql)
- O system prompt desse agente deve ser um prompt que oriente a API da OpenAI a traduzir uma pergunta do usuário em linguagem natural para uma consulta SQL no banco de dados conectado a ele.
- Como "motor" de IA usaremos a API da OpenAI, onde a url e as credenciais ficarão no arquivo .env

## Constraints
- O agente só poderá fazer SELECT
- O agente só poderá responder sobre tabelas e dados existentes nesse banco de dados
- O agente deve ser plenamente capaz de criar queries complexas com joins, subqueries e utilizar todos os recursos de queries do MySql. Deve ser capaz de usar funções agregadoras como count, avg, sum etc e deve ser capaz de criar tabelas, relatórios e respostas baseadas nos dados obtidos desse banco. 

## Entregáveis
1. Documentação da solução em .md
2. Diagramas com o desenho da arquitetura
3. Arquivos .yaml para configuração de pipelines de CI/CD no github actions
4. A API deve usar swagger