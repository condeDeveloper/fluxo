namespace Conde.Fluxo;

/// <summary>
/// Ford e Fulkerson, 1956: ache um caminho da fonte ao sorvedouro no residual,
/// mande por ele o quanto couber, repita.
///
/// O metodo NAO DIZ qual caminho escolher, e e so por isso que ele e lento. O
/// limite classico, O(E vezes o valor do fluxo), vale para a PIOR escolha
/// possivel, nao para uma implementacao qualquer.
///
/// Essa distincao e facil de perder, e eu perdi. A rede de livro e esta:
///
///   fonte -> a (capacidade C), fonte -> b (C), a -> sorvedouro (C),
///   b -> sorvedouro (C), a -> b (capacidade 1)
///
/// Alternando o caminho que passa pela aresta do meio, cada caminho carrega UMA
/// unidade e sao 2C caminhos. Eu escrevi que a busca em profundidade cairia
/// nisso, e **nao cai**: medi as 120 ordens de insercao possiveis e a pior delas
/// gasta 4 caminhos, nao 2C. O motivo e simples depois de visto: assim que a
/// aresta do meio satura, a profundidade encontra a saida direta e leva o resto
/// de uma vez. Para ter 2C e preciso um adversario que force a volta pela
/// reversa, e e isso que <see cref="ResolverPreferindo"/> faz.
///
/// Com capacidades irracionais o metodo pode nem terminar, e converge para um
/// valor que nem e o maximo. Isso nao esta implementado aqui porque exigiria
/// aritmetica exata com irracionais.
/// </summary>
public static class FordFulkerson
{
    /// <summary>Busca em profundidade na ordem em que as arestas foram inseridas.</summary>
    public static Resultado Resolver(Rede rede, int fonte, int sorvedouro) =>
        Executar(rede, fonte, sorvedouro, null);

    /// <summary>
    /// A mesma busca, com uma aresta (e a reversa dela) tentada ANTES das outras.
    /// Serve para mostrar que mexer na ordem nao basta para degenerar o metodo.
    /// </summary>
    public static Resultado ResolverPreferindo(Rede rede, int fonte, int sorvedouro, int arestaPreferida) =>
        Executar(rede, fonte, sorvedouro, arestaPreferida);

    /// <summary>
    /// Aumenta o fluxo ao longo de um caminho DADO, e devolve quanto passou.
    ///
    /// Isto existe porque o limite classico do metodo nao e sobre uma
    /// implementacao, e sobre a LIBERDADE de escolha que a definicao deixa em
    /// aberto. Nenhuma busca em profundidade razoavel produz a execucao de 2C
    /// passos, porque ela empurra o gargalo do caminho inteiro e, assim que a
    /// aresta estreita satura, leva o resto de uma vez. Medi: nas 120 ordens de
    /// insercao possiveis, a pior gasta 4 caminhos. Preferir a aresta do meio
    /// tambem da 4.
    ///
    /// A execucao de 2C e legal e precisa ser EXIBIDA, caminho a caminho, e e
    /// isso que este metodo permite. O limite nao e teorico: ele e alcancavel, so
    /// nao por acidente.
    /// </summary>
    public static long AumentarPor(Rede rede, IReadOnlyList<int> caminho)
    {
        ArgumentNullException.ThrowIfNull(rede);
        ArgumentNullException.ThrowIfNull(caminho);
        if (caminho.Count < 2) throw new ArgumentException("um caminho tem ao menos dois vertices", nameof(caminho));

        var arestas = new int[caminho.Count - 1];
        var folga = long.MaxValue;
        for (var k = 0; k + 1 < caminho.Count; k++)
        {
            var achou = -1;
            foreach (var i in rede.Saindo(caminho[k]))
            {
                if (rede.Destino(i) != caminho[k + 1] || rede.Residual(i) <= 0) continue;
                achou = i;
                break;
            }
            if (achou < 0) return 0;
            arestas[k] = achou;
            folga = Math.Min(folga, rede.Residual(achou));
        }
        foreach (var i in arestas) rede.Empurrar(i, folga);
        return folga;
    }

    private static Resultado Executar(Rede rede, int fonte, int sorvedouro, int? preferida)
    {
        ArgumentNullException.ThrowIfNull(rede);
        if (fonte == sorvedouro) throw new ArgumentException("fonte e sorvedouro iguais");
        rede.ZerarFluxo();
        long valor = 0, caminhos = 0, visitas = 0;

        while (true)
        {
            var visto = new bool[rede.Vertices];
            var folga = Procurar(rede, fonte, sorvedouro, long.MaxValue, visto, preferida, ref visitas);
            if (folga == 0) break;
            valor += folga;
            caminhos++;
        }
        return new Resultado(valor, Alcancaveis.CorteMinimo(rede, fonte), caminhos, visitas);
    }

    /// <summary>
    /// Desce em profundidade e, ao voltar, ja empurra o fluxo. Devolve quanto
    /// passou, ou zero se nao achou caminho.
    /// </summary>
    private static long Procurar(Rede rede, int v, int sorvedouro, long limite, bool[] visto, int? preferida, ref long visitas)
    {
        if (v == sorvedouro) return limite;
        visto[v] = true;
        foreach (var i in NaOrdem(rede, v, preferida))
        {
            visitas++;
            var r = rede.Residual(i);
            if (r <= 0) continue;
            var u = rede.Destino(i);
            if (visto[u]) continue;
            var passou = Procurar(rede, u, sorvedouro, Math.Min(limite, r), visto, preferida, ref visitas);
            if (passou <= 0) continue;
            rede.Empurrar(i, passou);
            return passou;
        }
        return 0;
    }

    /// <summary>As arestas que saem de v, com a preferida e a reversa dela na frente.</summary>
    private static IEnumerable<int> NaOrdem(Rede rede, int v, int? preferida)
    {
        var saindo = rede.Saindo(v);
        if (preferida is null) return saindo;
        var par = preferida.Value & ~1;
        var frente = new List<int>();
        var resto = new List<int>();
        foreach (var i in saindo) ((i & ~1) == par ? frente : resto).Add(i);
        frente.AddRange(resto);
        return frente;
    }
}
