namespace Conde.Fluxo;

/// <summary>
/// Caminhos disjuntos, pelo teorema de Menger (1927).
///
/// O teorema e de trinta anos antes do algoritmo de fluxo e diz a mesma coisa em
/// outra lingua: o numero maximo de caminhos disjuntos por aresta entre dois
/// vertices e igual ao numero minimo de arestas que precisam ser removidas para
/// separar os dois.
///
/// Com capacidade um em todas as arestas, isso E o teorema do fluxo maximo e
/// corte minimo. O fluxo vira um conjunto de caminhos que nao compartilham
/// aresta, e o corte vira o conjunto minimo de arestas a remover. E a razao de
/// fluxo responder perguntas que nao parecem ser sobre fluxo.
///
/// Para caminhos disjuntos por VERTICE o truque e dividir cada vertice em dois,
/// ligados por uma aresta de capacidade um: passar pelo vertice passa a custar
/// essa aresta, e um vertice so pode ser usado por um caminho.
/// </summary>
public static class Menger
{
    /// <summary>Quantos caminhos sem aresta em comum existem entre os dois vertices.</summary>
    public static long CaminhosDisjuntosPorAresta(int vertices, IEnumerable<(int De, int Para)> arestas, int de, int para)
    {
        var rede = Geradores.Unitaria(vertices, arestas);
        return Dinic.Resolver(rede, de, para).Valor;
    }

    /// <summary>
    /// Quantos caminhos sem VERTICE em comum (fora as pontas). Cada vertice vira
    /// um par entrada/saida ligado por capacidade um, que e o que limita a
    /// passagem a um caminho so.
    /// </summary>
    public static long CaminhosDisjuntosPorVertice(int vertices, IEnumerable<(int De, int Para)> arestas, int de, int para)
    {
        ArgumentNullException.ThrowIfNull(arestas);
        // entrada do vertice v = 2v, saida = 2v+1
        var rede = new Rede(vertices * 2);
        for (var v = 0; v < vertices; v++)
        {
            // As pontas nao podem ser limitadas, senao o proprio caminho nao sai.
            var capacidade = v == de || v == para ? long.MaxValue / 4 : 1;
            rede.Ligar(v * 2, v * 2 + 1, capacidade);
        }
        // Capacidade UM nas arestas, nao infinito: uma aresta direta da fonte ao
        // sorvedouro nao passa por vertice do meio, entao nada a limitaria e o
        // fluxo saia infinito. Era isso que o teste pegava, com 2.305.843.009.213.693.951
        // caminhos disjuntos num grafo de sete vertices.
        foreach (var (a, b) in arestas) rede.Ligar(a * 2 + 1, b * 2, 1);
        return Dinic.Resolver(rede, de * 2, para * 2 + 1).Valor;
    }

    /// <summary>
    /// Extrai os caminhos de verdade a partir do fluxo, seguindo arestas com
    /// fluxo positivo e descontando conforme anda.
    ///
    /// Isso serve de conferencia forte: o algoritmo diz que existem k caminhos
    /// disjuntos, e aqui eles sao EXIBIDOS. Um valor sem os caminhos poderia estar
    /// certo por acaso; os caminhos nao.
    /// </summary>
    public static List<List<int>> Caminhos(Rede rede, int fonte, int sorvedouro)
    {
        ArgumentNullException.ThrowIfNull(rede);
        var restante = new long[rede.Arestas];
        for (var i = 0; i < rede.Arestas; i++) restante[i] = Math.Max(0, rede.FluxoDe(i));

        var saida = new List<List<int>>();
        while (true)
        {
            var caminho = new List<int> { fonte };
            var v = fonte;
            var andou = true;
            while (v != sorvedouro && andou)
            {
                andou = false;
                foreach (var i in rede.Saindo(v))
                {
                    if (restante[i] <= 0) continue;
                    restante[i]--;
                    v = rede.Destino(i);
                    caminho.Add(v);
                    andou = true;
                    break;
                }
            }
            if (v != sorvedouro) break;
            saida.Add(caminho);
        }
        return saida;
    }
}
