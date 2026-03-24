# Documento Técnico

## 1. Visão Geral
O DB Assistant é uma API REST em .NET 10 que transforma perguntas em linguagem natural em consultas SQL somente leitura para MySQL. A solução segue separação por camadas (Domain, Data, Services, UseCases e Api), com foco em baixo acoplamento, testabilidade e segurança operacional.

O objetivo principal é acelerar análise de dados sem expor usuários finais à complexidade de SQL e sem permitir mutações no banco.

## 2. Fluxo da Requisição
1. O cliente envia `POST /api/assistant/query` com pergunta e opção de execução.
2. O middleware valida a API key (`x-api-key` por padrão) antes de chegar ao controller.
3. O `AssistantController` delega para `IProcessNaturalLanguageQueryUseCase`.
4. O caso de uso monta contexto de schema via `ISchemaContextAssembler`:
   - Consulta o índice local de conhecimento (`knowledge/schema-index.json`) para RAG.
   - Faz fallback para metadados vivos de `INFORMATION_SCHEMA` para cobrir tabelas ainda não indexadas.
5. O `ISqlGenerationGateway` (OpenAI) gera SQL estruturado com base na pergunta e no contexto.
6. O domínio valida a instrução SQL e bloqueia comandos não permitidos (somente leitura).
7. Se `ExecuteQuery=true`, o `ISqlQueryExecutor` executa no MySQL e retorna colunas/linhas.
8. A API devolve SQL, explicação, origem do contexto (`rag+information_schema`) e resultado tabular quando aplicável.

## 3. Decisões Arquiteturais
- Clean Architecture + DDD leve: regras de domínio e portas no centro, detalhes de infraestrutura nas bordas.
- Acesso SQL direto (sem ORM): o produto depende de consultas dinâmicas e analíticas, então SQL explícito atende melhor o problema.
- Contratos por interface: facilita testes de integração por camada e substituição de gateways externos.
- Pipeline híbrido RAG + metadata vivo: melhora qualidade do prompt sem perder aderência ao estado real do banco.

## 4. Estratégia de Escalabilidade
### 4.1 Escalabilidade horizontal da API
A aplicação é stateless no runtime da API. Isso permite múltiplas réplicas em container sem sincronização de estado em memória entre instâncias.

### 4.2 Escalabilidade de custo
A implantação em Azure Container Apps permite ajustar réplicas conforme demanda. Em baixa carga, o custo permanece controlado por não exigir VMs dedicadas sempre ativas.

### 4.3 Escalabilidade de contexto
O índice de conhecimento (`schema-index.json`) reduz o volume de contexto enviado ao LLM na maioria das perguntas. Para casos não cobertos, `INFORMATION_SCHEMA` mantém completude funcional.

### 4.4 Escalabilidade de entrega
GitHub Actions automatiza build, testes e publicação de imagem. O deploy por tag imutável (`sha-<commit>`) melhora previsibilidade de rollback e rastreabilidade.

## 5. Estratégia de Segurança
### 5.1 Segurança de acesso
- Endpoint protegido por API key no header configurável.
- Sem API key válida, a requisição é rejeitada (`401 Unauthorized`).

### 5.2 Segurança de dados
- Guardrails no domínio aceitam apenas SQL de leitura.
- Não há endpoint para escrita/DDL.
- O acesso ao banco usa credenciais via variáveis de ambiente, fora do código-fonte.

### 5.3 Segurança de supply chain
- Build reproduzível no CI.
- Imagem versionada e publicada no GitHub Container Registry.
- Deploy consumindo imagem por digest lógico de commit (tag SHA).

### 5.4 Segurança de segredos
- Segredos sensíveis ficam em GitHub Secrets e Azure Container App Secrets.
- O workflow injeta o valor da API key no segredo gerenciado do Azure e referencia o segredo por `secretref`, evitando hardcode.

## 6. Observabilidade e Operação
A base atual já permite rastreabilidade por commit/tag de imagem e validação em testes. Como próximos incrementos operacionais, recomenda-se padronizar logs estruturados, métricas de latência por etapa (RAG, OpenAI, banco) e alertas para falhas de autenticação e indisponibilidade externa.
