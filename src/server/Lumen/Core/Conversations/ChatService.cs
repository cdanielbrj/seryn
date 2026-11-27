using System.Globalization;
using Seryn.Server.Lumen.Core.Contracts;
using Seryn.Server.Lumen.Core.Providers;
using Seryn.Server.Lumen.Core.Tools;

namespace Seryn.Server.Lumen.Core.Conversations;

internal sealed class ChatService(IModelProvider provider, ILumenToolCatalog tools, ILumenPersona persona, ChatGate gate)
{
    public static Dictionary<string, string[]>? Validate(ChatRequest request)
    {
        if (request.Mensagens is not { Count: > 0 and <= 12 } messages ||
            messages.Any(message => message is null || message.Papel is not ("usuario" or "seryn") || string.IsNullOrWhiteSpace(message.Conteudo)
                || message.Conteudo.Length > (message.Papel == "usuario" ? 4000 : 12000)) ||
            messages.Sum(message => message.Conteudo!.Length) > 24000 || messages[^1].Papel != "usuario" ||
            messages.Where((message, index) => message.Papel != (index % 2 == 0 ? "usuario" : "seryn")).Any())
            return new() { ["mensagens"] = ["Envie uma conversa alternada, iniciada e terminada pelo usuário, com até 12 mensagens e 24 mil caracteres. Cada pergunta aceita até 4 mil caracteres."] };
        if (request.Referencia?.Length != 7 || !DateOnly.TryParseExact(request.Referencia, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return new() { ["referencia"] = ["Informe o mês de referência no formato AAAA-MM."] };
        if (request.Modelo is not null && request.Modelo.Trim().Length is < 1 or > 160)
            return new() { ["modelo"] = ["Informe um modelo com até 160 caracteres."] };
        if (request.Sessao is not null && (request.Sessao.Length != 36 || !Guid.TryParseExact(request.Sessao, "D", out _)))
            return new() { ["sessao"] = ["Informe um identificador de conversa UUID válido."] };
        return null;
    }

    public async Task<ChatResponse> Send(ChatRequest request, CancellationToken cancellationToken)
    {
        if (!await gate.Semaphore.WaitAsync(0, cancellationToken)) throw new ChatFailure(409, "A Seryn já está respondendo uma pergunta. Aguarde a conclusão.");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(90));
            var messages = new List<ConversationMessage>
            {
                new("system", persona.SystemMessage(request.Referencia!))
            };
            messages.AddRange(request.Mensagens!.Select(message => new ConversationMessage(message.Papel == "usuario" ? "user" : "assistant", message.Conteudo)));
            var queries = new List<ChatQuery>();
            var sessionId = request.Sessao?.ToLowerInvariant() ?? Guid.NewGuid().ToString("D");
            long input = 0, output = 0;
            decimal? cost = 0;
            for (var round = 0; round < 4; round++)
            {
                var reply = await provider.Complete(messages, tools.Definitions, request.Modelo?.Trim(), sessionId, timeout.Token);
                input += reply.InputTokens;
                output += reply.OutputTokens;
                cost = cost is not null && reply.CostUsd is not null ? cost + reply.CostUsd : null;
                if (reply.Tools.Count == 0)
                {
                    if (string.IsNullOrWhiteSpace(reply.Content) || reply.Content.Length > 12000)
                        throw new ChatFailure(502, "O modelo não retornou uma resposta utilizável. Tente novamente.");
                    return new(reply.Content, reply.Model, queries, new(input, output, cost));
                }
                if (reply.Tools.Count > 4 || queries.Count + reply.Tools.Count > 6 || round == 3 ||
                    reply.Tools.Any(call => string.IsNullOrWhiteSpace(call.Id) || call.Id.Length > 200 || string.IsNullOrWhiteSpace(call.Name) || call.Arguments is null) ||
                    reply.Tools.Select(call => call.Id).Distinct().Count() != reply.Tools.Count)
                    throw new ChatFailure(422, "A consulta atingiu o limite de ferramentas ou retornou chamadas inválidas. Tente uma pergunta mais específica.");
                messages.Add(new("assistant", reply.Content, reply.Tools));
                foreach (var call in reply.Tools)
                {
                    timeout.Token.ThrowIfCancellationRequested();
                    var query = await tools.Execute(call, timeout.Token);
                    queries.Add(query);
                    messages.Add(new("tool", query.Resultado.GetRawText(), ToolId: call.Id));
                }
                if (messages.Sum(message => message.Content?.Length ?? 0) > 64000)
                    throw new ChatFailure(422, "O contexto da consulta atingiu o limite. Refine a pergunta.");
            }
            throw new ChatFailure(422, "O modelo atingiu o limite de consultas.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ChatFailure(504, "O provedor demorou para responder. Tente novamente ou reduza a consulta.");
        }
        catch (HttpRequestException)
        {
            throw new ChatFailure(502, "Não foi possível comunicar com o provedor. Tente novamente mais tarde.");
        }
        finally { gate.Semaphore.Release(); }
    }
}

internal sealed class ChatGate : IDisposable
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public void Dispose() => Semaphore.Dispose();
}
