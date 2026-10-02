namespace Conde.Fluxo;

/// <summary>
/// O valor do fluxo maximo, o corte minimo que o acompanha e o trabalho gasto.
///
/// O corte vem junto porque ele e a TESTEMUNHA do maximo. Dizer "o fluxo maximo
/// e 7" e uma afirmacao que exige confianca; dizer "o fluxo maximo e 7, e aqui
/// esta um corte de capacidade 7" e uma afirmacao que qualquer um confere em
/// tempo linear. Um fluxo de valor v prova que o maximo e pelo menos v; um corte
/// de capacidade v prova que e no maximo v. Juntos, fecham.
/// </summary>
public sealed class Resultado
{
    public Resultado(long valor, Corte corte, long caminhos, long visitas)
    {
        ArgumentNullException.ThrowIfNull(corte);
        Valor = valor;
        Corte = corte;
        Caminhos = caminhos;
        Visitas = visitas;
    }

    public long Valor { get; }

    /// <summary>Um corte de capacidade igual ao valor: a prova do maximo.</summary>
    public Corte Corte { get; }

    /// <summary>Quantos caminhos aumentantes (ou fases, no Dinic) foram usados.</summary>
    public long Caminhos { get; }

    /// <summary>Quantas arestas foram olhadas. E o trabalho de verdade.</summary>
    public long Visitas { get; }

    /// <summary>
    /// Confere a propria resposta pelo teorema: a capacidade do corte devolvido
    /// tem de ser igual ao valor, e o corte tem de separar mesmo. Isso nao
    /// depende de nenhum outro algoritmo.
    /// </summary>
    public bool Confere(Rede rede, int fonte, int sorvedouro)
    {
        ArgumentNullException.ThrowIfNull(rede);
        return Corte.Separa(fonte, sorvedouro) && Corte.Capacidade(rede) == Valor;
    }

    public override string ToString() => $"fluxo {Valor} (caminhos {Caminhos}, visitas {Visitas})";
}
