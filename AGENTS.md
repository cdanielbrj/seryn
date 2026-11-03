# AGENTS.md — Seryn

Este repositório implementa **Seryn**, um planejador self-hosted de entradas/saídas previstas. **Stack aprovada: React/TypeScript/Vite no frontend, ASP.NET Core 10/C# no backend, EF Core 10 + SQLite na persistência.** **Leia `docs/SERYN_SPEC.md` integralmente antes de modificar o projeto.** O documento define o produto, a arquitetura, as regras financeiras, a UI aprovada e os critérios de aceite; ele prevalece sobre suposições genéricas sobre aplicativos de finanças.

## Regras que não podem ser violadas

- Um único modelo de registro para qualquer entrada/saída; **valor armazenado é sempre por ocorrência**, em centavos inteiros.
- Recorrências são **calculadas em consultas**, sem gerar parcelas ou lançamentos futuros no SQLite.
- Totais, fim de séries, projeções e resultados são derivados, não fonte de verdade persistida.
- **Todos os valores são previstos.** Não inventar status pago/quitado/pendente ou contas bancárias.
- Tipo e categoria são classificações editáveis, **nunca determinam comportamento financeiro**.
- CSV/Excel import/export deve preservar o registro original e permitir round-trip sem duplicações silenciosas.
- Aplicação self-hosted em **um único Docker container e processo ASP.NET Core (Kestrel)** com SQLite persistido em `/data`; Node somente durante o build do React.
- **Monólito modular, sem microserviços**, serviços auxiliares ou autenticação terceirizada obrigatória; usar API `/api/v1` e OpenAPI para permitir integrações futuras.
- UI, importações e APIs futuras devem compartilhar o mesmo serviço de aplicação e validação; nunca escrever diretamente no banco contornando as regras.
- **Usuário único e primeiro acesso seguro antes de disponibilizar dados reais**; proteger rotas e sessões, backups e exportações.
- Dashboard desktop deve preservar as seções do protótipo **`design/seryn_mock_v2.html`**; mobile deve seguir composição própria com navbar inferior. Temas claro e escuro.
- O mock contém dados fictícios, não algoritmos financeiros válidos. **Não copiar cálculos estáticos do mock**.
- Uma projeção **não é saldo bancário**: o gráfico deve usar resultado acumulado dentro do período, começando de zero.

## Como trabalhar

1. Leia `docs/SERYN_SPEC.md` e inspecione `design/`.
2. Respeite a stack .NET 10 LTS + EF Core 10, o monólito e a camada C# pura de recorrências; implemente em incrementos pequenos e testáveis.
3. Não acrescente funcionalidades fora do escopo da v0.1 sem solicitação, inclusive conectores da Aira, tokens de terceiros, contas bancárias ou módulos de investimentos.
4. Faça build, lint e testes, inclusive exemplos numéricos canônicos, antes de declarar conclusão.
5. Documente decisões técnicas de implementação que não constem do documento de produto.
6. Termine cada entrega com resumo do que funciona, como testar e o que falta.
7. Escreva em inglês todo conteúdo destinado ao repositório, incluindo `README.md`, documentos em `docs/`, ADRs, descrições de release e workflows. A interface e os contratos públicos do produto permanecem em português do Brasil conforme a especificação.

## Instância local de desenvolvimento

- Manter o projeto Compose `seryn` ativo em `http://localhost:8080` para validação compartilhada com o usuário.
- Atualizar com `docker compose -p seryn up --build -d --wait`. A recriação do container é permitida; preservar o volume `seryn_seryn-data`, credenciais e dados existentes.
- Não encerrar essa instância ao finalizar uma tarefa e não executar `down -v` nela. Testes destrutivos e E2E devem usar outro projeto Compose com volume descartável.
