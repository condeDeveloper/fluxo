namespace Conde.Fluxo;

/// <summary>
/// O juiz: enumera TODOS os cortes e devolve o menor.
///
/// Sao 2^(n-2) cortes, porque a fonte e o sorvedouro tem lado fixo e cada um dos
/// outros vertices escolhe um dos dois. Nenhum algoritmo de fluxo e usado aqui:
/// a unica coisa que esta funcao conhece e a DEFINICAO de capacidade de corte.
///
/// Isso importa porque o teorema do fluxo maximo e corte minimo e justamente o
/// que se quer testar. Se o juiz tambem fosse um algoritmo de fluxo, os dois
/// poderiam errar junto e o teste nao diria nada. Com a enumeracao, o teorema
/// vira uma afirmacao verificavel: o valor que os algoritmos acham tem de ser
/// EXATAMENTE o minimo desta lista.
/// </summary>
public static class ForcaBruta
{
    /// <summary>Acima disso a enumeracao deixa de ser razoavel.</summary>
    public const int MaximoDeVertices = 22;

    /// <summary>Todos os cortes que separam a fonte do sorvedouro.</summary>
    public static IEnumerable<Corte> Cortes(Rede rede, int fonte, int sorvedouro)
    {
        ArgumentNullException.ThrowIfNull(rede);
        if (rede.Vertices > MaximoDeVertices)
            throw new ArgumentException($"{rede.Vertices} vertices sao 2^{rede.Vertices - 2} cortes; o juiz vai ate {MaximoDeVertices}", nameof(rede));
        if (fonte == sorvedouro) throw new ArgumentException("fonte e sorvedouro iguais nao definem corte");

        var livres = new List<int>();
        for (var v = 0; v < rede.Vertices; v++)
            if (v != fonte && v != sorvedouro) livres.Add(v);

        var total = 1L << livres.Count;
        for (long mascara = 0; mascara < total; mascara++)
        {
            var lado = new bool[rede.Vertices];
            lado[fonte] = true;
            for (var k = 0; k < livres.Count; k++)
                lado[livres[k]] = ((mascara >> k) & 1) == 1;
            yield return new Corte(lado);
        }
    }

    /// <summary>A menor capacidade entre todos os cortes. E a resposta de referencia.</summary>
    public static long CorteMinimo(Rede rede, int fonte, int sorvedouro)
    {
        var menor = long.MaxValue;
        foreach (var c in Cortes(rede, fonte, sorvedouro))
            menor = Math.Min(menor, c.Capacidade(rede));
        return menor;
    }

    /// <summary>O corte minimo em si, e nao so a capacidade dele.</summary>
    public static Corte MelhorCorte(Rede rede, int fonte, int sorvedouro)
    {
        Corte? melhor = null;
        var menor = long.MaxValue;
        foreach (var c in Cortes(rede, fonte, sorvedouro))
        {
            var cap = c.Capacidade(rede);
            if (cap >= menor) continue;
            menor = cap;
            melhor = c;
        }
        return melhor ?? throw new InvalidOperationException("rede sem corte, o que nao deveria acontecer");
    }

    /// <summary>
    /// Quantos cortes existem. Entra no teste para que uma mudanca acidental na
    /// geracao nao passe despercebida: se o dominio encolher, o teste continuaria
    /// "passando" sobre outra coisa.
    /// </summary>
    public static long QuantosCortes(int vertices) => 1L << (vertices - 2);
}
