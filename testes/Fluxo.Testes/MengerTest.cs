using Conde.Fluxo;
using Xunit;

namespace Conde.Fluxo.Testes;

/// <summary>
/// Menger, 1927: caminhos disjuntos e separacao minima sao o mesmo numero. O
/// teorema e trinta anos mais velho que o algoritmo de fluxo.
/// </summary>
public class MengerTest
{
    /// <summary>
    /// O juiz aqui nao e fluxo: e a enumeracao de todos os subconjuntos de
    /// arestas, procurando o menor que desconecta. Com 10 arestas sao 1.024
    /// subconjuntos, e cada um e testado com uma busca em largura comum.
    ///
    /// Se o fluxo e a enumeracao concordam, o teorema vale naquela instancia; e
    /// como nenhuma das duas sabe da outra, concordarem por acaso seria muita
    /// coincidencia.
    /// </summary>
    [Fact]
    public void CaminhosDisjuntosBatemComARemocaoMinima()
    {
        var aleatorio = new Random(42);
        var conferidas = 0;
        for (var caso = 0; caso < 400; caso++)
        {
            const int n = 6;
            var arestas = new List<(int De, int Para)>();
            for (var a = 0; a < n; a++)
                for (var b = 0; b < n; b++)
                {
                    if (a == b) continue;
                    if (aleatorio.Next(100) < 28) arestas.Add((a, b));
                }
            if (arestas.Count > 14) continue;

            var porFluxo = Menger.CaminhosDisjuntosPorAresta(n, arestas, 0, n - 1);
            var porRemocao = MenorRemocaoQueDesconecta(n, arestas, 0, n - 1);
            Assert.True(porFluxo == porRemocao,
                $"fluxo deu {porFluxo} e a remocao minima deu {porRemocao} em {arestas.Count} arestas");
            conferidas++;
        }
        Assert.True(conferidas > 300, $"so {conferidas} casos couberam no limite de arestas");
    }

    /// <summary>O juiz: menor conjunto de arestas cuja remocao desconecta.</summary>
    private static int MenorRemocaoQueDesconecta(int n, List<(int De, int Para)> arestas, int de, int para)
    {
        for (var quantas = 0; quantas <= arestas.Count; quantas++)
        {
            foreach (var escolha in Subconjuntos(arestas.Count, quantas))
            {
                var ficam = new List<(int De, int Para)>();
                for (var i = 0; i < arestas.Count; i++) if (!escolha.Contains(i)) ficam.Add(arestas[i]);
                if (!Alcanca(n, ficam, de, para)) return quantas;
            }
        }
        return arestas.Count;
    }

    private static IEnumerable<HashSet<int>> Subconjuntos(int total, int quantos)
    {
        var atual = new int[quantos];
        return Gerar(0, 0);

        IEnumerable<HashSet<int>> Gerar(int profundidade, int inicio)
        {
            if (profundidade == quantos) { yield return [.. atual]; yield break; }
            for (var i = inicio; i < total; i++)
            {
                atual[profundidade] = i;
                foreach (var s in Gerar(profundidade + 1, i + 1)) yield return s;
            }
        }
    }

    private static bool Alcanca(int n, List<(int De, int Para)> arestas, int de, int para)
    {
        var vizinhos = new List<int>[n];
        for (var v = 0; v < n; v++) vizinhos[v] = [];
        foreach (var (a, b) in arestas) vizinhos[a].Add(b);
        var visto = new bool[n];
        var fila = new Queue<int>();
        visto[de] = true; fila.Enqueue(de);
        while (fila.Count > 0)
        {
            var v = fila.Dequeue();
            if (v == para) return true;
            foreach (var u in vizinhos[v]) if (!visto[u]) { visto[u] = true; fila.Enqueue(u); }
        }
        return visto[para];
    }

    /// <summary>
    /// Os caminhos sao EXIBIDOS e nao so contados: cada um vai da fonte ao
    /// sorvedouro e nenhum compartilha aresta com outro. Um valor pode estar
    /// certo por acaso; k caminhos exibidos e disjuntos, nao.
    /// </summary>
    [Fact]
    public void OsCaminhosExistemESaoDisjuntos()
    {
        var arestas = new List<(int De, int Para)>
        {
            (0,1),(0,2),(0,3),(1,4),(2,4),(3,5),(4,6),(5,6),(1,5),(2,3),
        };
        var rede = Geradores.Unitaria(7, arestas);
        var r = Dinic.Resolver(rede, 0, 6);
        var caminhos = Menger.Caminhos(rede, 0, 6);

        Assert.Equal(r.Valor, caminhos.Count);
        var usadas = new HashSet<(int, int)>();
        foreach (var caminho in caminhos)
        {
            Assert.Equal(0, caminho[0]);
            Assert.Equal(6, caminho[^1]);
            for (var k = 0; k + 1 < caminho.Count; k++)
            {
                Assert.Contains((caminho[k], caminho[k + 1]), arestas);
                Assert.True(usadas.Add((caminho[k], caminho[k + 1])), "duas rotas usaram a mesma aresta");
            }
        }
    }

    /// <summary>
    /// Disjunto por VERTICE e uma restricao mais forte que por aresta, entao
    /// nunca pode dar mais. A versao por vertice divide cada um em entrada e
    /// saida ligadas por capacidade um.
    /// </summary>
    [Fact]
    public void PorVerticeNuncaPassaDePorAresta()
    {
        var aleatorio = new Random(7);
        for (var caso = 0; caso < 300; caso++)
        {
            const int n = 7;
            var arestas = new List<(int De, int Para)>();
            for (var a = 0; a < n; a++)
                for (var b = 0; b < n; b++)
                {
                    if (a == b) continue;
                    if (aleatorio.Next(100) < 30) arestas.Add((a, b));
                }
            var porAresta = Menger.CaminhosDisjuntosPorAresta(n, arestas, 0, n - 1);
            var porVertice = Menger.CaminhosDisjuntosPorVertice(n, arestas, 0, n - 1);
            Assert.True(porVertice <= porAresta,
                $"por vertice deu {porVertice} e por aresta deu {porAresta}, o que e impossivel");
        }
    }

    /// <summary>
    /// Num grafo em que todos os caminhos passam por um gargalo de um vertice so,
    /// a versao por vertice tem de dar exatamente 1, por mais arestas que haja.
    /// </summary>
    [Fact]
    public void UmGargaloDeUmVerticeLimitaAUmCaminho()
    {
        // 0 -> a,b,c -> 3 (gargalo) -> d,e,f -> 7
        var arestas = new List<(int, int)>
        {
            (0,1),(0,2),(1,3),(2,3),(3,4),(3,5),(4,6),(5,6),
        };
        Assert.Equal(1, Menger.CaminhosDisjuntosPorVertice(7, arestas, 0, 6));
        Assert.Equal(2, Menger.CaminhosDisjuntosPorAresta(7, arestas, 0, 6));
    }
}
