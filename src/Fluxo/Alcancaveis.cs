namespace Conde.Fluxo;

/// <summary>
/// De onde sai o corte minimo depois que o fluxo acabou.
///
/// Esta e a parte do teorema que costuma ser decorada sem ser entendida, e ela e
/// curta: quando nao ha mais caminho aumentante, o conjunto S dos vertices
/// alcancaveis a partir da fonte PELO RESIDUAL e um corte minimo.
///
/// O argumento cabe em tres linhas. O sorvedouro nao esta em S, senao haveria
/// caminho. Toda aresta de S para T esta cheia, senao o destino estaria em S.
/// Toda aresta de T para S esta vazia, senao a reversa dela daria passagem e a
/// origem estaria em S. Logo a capacidade do corte e exatamente o fluxo que
/// atravessa, que e o valor. E como nenhum corte pode ser menor que um fluxo, ele
/// e minimo.
/// </summary>
public static class Alcancaveis
{
    /// <summary>Os vertices que a fonte alcanca andando so por residual positivo.</summary>
    public static bool[] APartirDe(Rede rede, int fonte)
    {
        ArgumentNullException.ThrowIfNull(rede);
        var visto = new bool[rede.Vertices];
        var fila = new Queue<int>();
        visto[fonte] = true;
        fila.Enqueue(fonte);
        while (fila.Count > 0)
        {
            var v = fila.Dequeue();
            foreach (var i in rede.Saindo(v))
            {
                if (rede.Residual(i) <= 0) continue;
                var u = rede.Destino(i);
                if (visto[u]) continue;
                visto[u] = true;
                fila.Enqueue(u);
            }
        }
        return visto;
    }

    /// <summary>O corte minimo que sai do residual saturado.</summary>
    public static Corte CorteMinimo(Rede rede, int fonte) => new(APartirDe(rede, fonte));
}
