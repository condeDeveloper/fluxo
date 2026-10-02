namespace Conde.Fluxo;

/// <summary>
/// Um corte (S, T): uma particao dos vertices com a fonte de um lado e o
/// sorvedouro do outro.
///
/// A capacidade de um corte conta SO as arestas que atravessam de S para T. As
/// que voltam de T para S nao entram, e esse detalhe e exatamente onde a conta
/// costuma sair errada. O motivo e que um corte limita o que PODE atravessar: o
/// fluxo que volta nao ajuda a passar, ele so se cancela com o que foi.
/// </summary>
public sealed class Corte
{
    private readonly bool[] _ladoDaFonte;

    public Corte(bool[] ladoDaFonte)
    {
        ArgumentNullException.ThrowIfNull(ladoDaFonte);
        _ladoDaFonte = (bool[])ladoDaFonte.Clone();
    }

    public int Vertices => _ladoDaFonte.Length;

    /// <summary>Verdadeiro quando o vertice esta no lado da fonte.</summary>
    public bool NoLadoDaFonte(int vertice) => _ladoDaFonte[vertice];

    /// <summary>Quantos vertices ficaram do lado da fonte.</summary>
    public int TamanhoDoLadoDaFonte()
    {
        var n = 0;
        foreach (var b in _ladoDaFonte) if (b) n++;
        return n;
    }

    /// <summary>
    /// A soma das capacidades das arestas que saem de S e chegam em T. As que
    /// voltam nao contam.
    /// </summary>
    public long Capacidade(Rede rede)
    {
        ArgumentNullException.ThrowIfNull(rede);
        if (rede.Vertices != _ladoDaFonte.Length)
            throw new ArgumentException("o corte e de outra rede", nameof(rede));
        long soma = 0;
        for (var v = 0; v < rede.Vertices; v++)
        {
            if (!_ladoDaFonte[v]) continue;
            foreach (var i in rede.Saindo(v))
            {
                // So as arestas de verdade, nao as reversas que nascem com
                // capacidade zero. A capacidade zero ja as deixa de fora, mas
                // dizer isso explicitamente evita a leitura errada.
                if (_ladoDaFonte[rede.Destino(i)]) continue;
                soma += rede.Capacidade(i);
            }
        }
        return soma;
    }

    /// <summary>
    /// As arestas que atravessam. Sao elas que, num problema real, viram a lista
    /// do que precisa ser rompido, comprado ou reforcado.
    /// </summary>
    public IEnumerable<(int De, int Para, long Capacidade)> Atravessando(Rede rede)
    {
        ArgumentNullException.ThrowIfNull(rede);
        for (var v = 0; v < rede.Vertices; v++)
        {
            if (!_ladoDaFonte[v]) continue;
            foreach (var i in rede.Saindo(v))
            {
                if (_ladoDaFonte[rede.Destino(i)]) continue;
                if (rede.Capacidade(i) == 0) continue;
                yield return (v, rede.Destino(i), rede.Capacidade(i));
            }
        }
    }

    /// <summary>
    /// Verdadeiro quando o corte separa mesmo: a fonte de um lado, o sorvedouro
    /// do outro. Um "corte" que deixa os dois no mesmo lado nao limita nada, e
    /// aceitar um desses faria a busca pelo minimo devolver zero.
    /// </summary>
    public bool Separa(int fonte, int sorvedouro) =>
        _ladoDaFonte[fonte] && !_ladoDaFonte[sorvedouro];

    public override string ToString()
    {
        var s = new List<int>();
        for (var v = 0; v < _ladoDaFonte.Length; v++) if (_ladoDaFonte[v]) s.Add(v);
        return "S={" + string.Join(",", s) + "}";
    }
}
