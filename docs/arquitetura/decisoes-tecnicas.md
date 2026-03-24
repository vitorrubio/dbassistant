# Decisões Técnicas

## 1. Cache
### Decisão
Não há cache distribuído implementado nesta versão.

### Justificativa
- A resposta depende de dados transacionais que podem mudar com frequência.
- Cache prematuro pode devolver resultados desatualizados sem política de invalidação robusta.
- O foco atual foi garantir corretude (SQL somente leitura, contexto híbrido) antes de otimização avançada.

### Próximo passo sugerido
Introduzir cache seletivo para perguntas repetitivas e de baixa volatilidade com TTL curto e chave por hash de pergunta + schema version.

## 2. RAG (Retrieval-Augmented Generation)
### Decisão
Usamos RAG local baseado em arquivo (`knowledge/schema-index.json`) com fallback para `INFORMATION_SCHEMA` em tempo real.

### Justificativa
- RAG reduz tokens e melhora precisão sem depender de varredura completa do schema a cada pergunta.
- O fallback evita “cegueira” quando surgem tabelas novas ainda não indexadas.
- O artefato versionado em Git facilita revisão e auditoria da base de conhecimento.

### Mitigação de risco
- Regerar o artefato periodicamente via `tools/DBAssistant.KnowledgeGenerator`.
- Monitorar taxa de fallback para identificar drift entre índice e banco real.

## 3. Processamento síncrono vs assíncrono
### Decisão
Fluxo síncrono para geração e execução da consulta dentro da requisição HTTP.

### Justificativa
- Simplifica contrato da API e experiência do consumidor.
- Adequado para consultas analíticas de curta/média duração.
- Facilita troubleshooting por correlação direta request/response.

### Quando evoluir para assíncrono
- Consultas longas ou concorrência alta.
- Necessidade de fila, retentativas e callbacks/polling.
- Controle de custo por orquestração de jobs.

## 4. Mitigação de riscos
- **Risco de segurança SQL**: validação de domínio para aceitar apenas leitura.
- **Risco de indisponibilidade OpenAI**: exceções de serviço externo tratadas na camada de use case/gateway.
- **Risco de drift de schema**: estratégia híbrida RAG + `INFORMATION_SCHEMA`.
- **Risco de segredo exposto**: uso de variáveis de ambiente e secret stores (GitHub/Azure).
- **Risco de regressão**: testes unitários, integração por camada e testes de API com TestServer.

## 5. GitHub Actions
### Por que usamos
- Pipeline único para restore, build, testes e publicação.
- Feedback rápido em PR e padronização da qualidade.
- Integração nativa com GitHub Secrets e permissões do repositório.

### Benefícios
- Menor variação entre ambientes de desenvolvimento e entrega.
- Evidência de qualidade por execução automatizada.
- Menor esforço operacional para releases frequentes.

## 6. Containers
### Por que usamos
- Empacotamento previsível da aplicação e dependências.
- Portabilidade entre local, CI e nuvem.
- Escalabilidade horizontal simplificada.

### Benefícios
- Menos problemas de “funciona na minha máquina”.
- Deploy mais rápido e com rollback mais seguro via imagem versionada.

## 7. GitHub Container Registry (GHCR)
### Por que usamos
- Registry integrado ao ecossistema do código e pipeline.
- Controle de acesso alinhado ao repositório.
- Menor fricção para versionamento e distribuição da imagem.

### Benefícios
- Governança centralizada de artefatos.
- Redução de complexidade operacional em relação a manter registry separado.

## 8. Plano Azure (Azure Container Apps)
### Decisão
A entrega está direcionada para Azure Container Apps, com deploy automatizado no workflow.

### Por que esse plano
- Modelo gerenciado para containers HTTP com auto scaling nativo.
- Bom equilíbrio para workload de API stateless com variação de carga.
- Reduz carga operacional comparado a orquestração própria de cluster.

### Observação
O tipo exato de cobrança (por consumo/dedicado) deve ser confirmado no ambiente Azure da organização; o repositório mostra o alvo (Container App), mas não expõe o SKU financeiro.

## 9. Variáveis de ambiente e secrets
### Como gerenciamos
- `.env` local para desenvolvimento e testes locais.
- `.env_copy-me` como template versionado sem dados sensíveis.
- GitHub Secrets para credenciais de CI/CD e deploy.
- Azure Container App Secrets para segredo em runtime (ex.: API key do cliente).

### Por que assim
- Separação entre código e configuração sensível.
- Rotação de credenciais sem mudança de código.
- Menor risco de vazamento em commits e PRs.

## 10. Custos da aplicação
### 10.1 Principais drivers
- Chamadas à API da OpenAI (tokens de entrada/saída).
- Execução da API containerizada no Azure Container Apps.
- Consumo do MySQL gerenciado/externo.
- Armazenamento e transferência de imagem no GHCR.
- Execuções do GitHub Actions (minutos de CI).

### 10.2 Estratégias de controle
- Limitar tamanho do contexto enviado ao LLM (RAG seletivo).
- Definir `max_tokens` e políticas de timeout.
- Priorizar execução sob demanda com auto scaling.
- Reutilizar imagem por tags SHA e evitar rebuild desnecessário.
- Medir custo por requisição (observabilidade de tokens + latência + frequência).

### 10.3 Conclusão de custo
O maior componente tende a ser OpenAI em cenários de alto volume, seguido por compute da API e banco. A arquitetura atual favorece controle progressivo de custos por ser stateless, containerizada e com automação de deploy.
