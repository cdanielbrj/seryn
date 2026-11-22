namespace Seryn.Server.Modules.ImportExport.Contracts;

public sealed record ImportCommitResult(int Criados, int Existentes, int Excluidos);
public sealed record WorkbookRequest(string? ConteudoBase64);
public sealed record WorkbookCommitRequest(string? ConteudoBase64, Guid PreviewId, bool ConfirmarCriacoes, bool SubstituirDados, bool ConfirmarSubstituicao);
public sealed record WorkbookSheetPreview(string Aba, int Linhas, int Criacoes, int Existentes);
public sealed record WorkbookPreview(Guid? PreviewIdMesclagem, Guid? PreviewIdSubstituicao, string ExpiraEm, List<WorkbookSheetPreview> Abas, int Criacoes, int Existentes, int DadosAtuais, bool Completa, List<string> Erros);
