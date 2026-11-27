using System.Text.Json;

namespace Seryn.Server.Lumen.Core.Contracts;

public sealed record ChatMessage(string? Papel, string? Conteudo);
public sealed record ChatRequest(List<ChatMessage>? Mensagens, string? Referencia, string? Modelo = null, string? Sessao = null);
public sealed record ChatConnection(bool Disponivel, string Provedor, string? Modelo);
public sealed record ChatSource(string Nome, string Caminho);
public sealed record ChatQuery(string Ferramenta, JsonElement Argumentos, JsonElement Resultado, List<ChatSource> Fontes);
public sealed record ChatUsage(long EntradaTokens, long SaidaTokens, decimal? CustoUsd);
public sealed record ChatResponse(string Resposta, string Modelo, List<ChatQuery> Consultas, ChatUsage Uso);
