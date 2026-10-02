using Conde.Fluxo;
using Xunit;

namespace Conde.Fluxo.Testes;

/// <summary>
/// Emparelhamento bipartido por fluxo, e o teorema de Konig (1931): num grafo
/// bipartido, o maior emparelhamento tem o mesmo tamanho da menor cobertura por
/// vertices.
///
/// Os dois numeros caem do mesmo lugar: o fluxo maximo e o emparelhamento, o
/// corte minimo e a cobertura. Em grafo qualquer isso e falso, e e a bipartição
/// que faz valer.
/// </summary>
public class KonigTest
{
    /// <summary>
    /// O juiz e a forca bruta sobre os subconjuntos de arestas: tenta todo
    /// conjunto e fica com o maior que nao repete vertice. Sem fluxo nenhum.
    /// </summary>
    [Fact]
    public void OEmparelhamentoBateComAForcaBruta()
    {
        var aleatorio = new Random(2026);
        var conferidos = 0;
        for (var caso = 0; caso < 500; caso++)
        {
            const int esquerda = 4, direita = 4;
            var pares = new List<(int A, int B)>();
            for (var a = 0; a < esquerda; a++)
                for (var b = 0; b < direita; b++)
                    if (aleatorio.Next(100) < 40) pares.Add((a, b));

            var (rede, fonte, sorvedouro) = Geradores.Bipartida(esquerda, direita, pares);
            var porFluxo = Dinic.Resolver(rede, fonte, sorvedouro).Valor;
            Assert.Equal(MaiorEmparelhamento(pares), porFluxo);
            conferidos++;
        }
        Assert.Equal(500, conferidos);
    }

    private static int MaiorEmparelhamento(List<(int A, int B)> pares)
    {
        var melhor = 0;
        for (var mascara = 0; mascara < 1 << pares.Count; mascara++)
        {
            var usadosA = new HashSet<int>();
            var usadosB = new HashSet<int>();
            var quantos = 0;
            var vale = true;
            for (var i = 0; i < pares.Count && vale; i++)
            {
                if ((mascara & (1 << i)) == 0) continue;
                if (!usadosA.Add(pares[i].A) || !usadosB.Add(pares[i].B)) vale = false;
                else quantos++;
            }
            if (vale) melhor = Math.Max(melhor, quantos);
        }
        return melhor;
    }

    /// <summary>
    /// Konig: emparelhamento maximo = cobertura minima por vertices. A cobertura
    /// sai do corte minimo, e o juiz dela e outra forca bruta, sobre os
    /// subconjuntos de vertices.
    /// </summary>
    [Fact]
    public void EmparelhamentoIgualCoberturaMinima()
    {
        var aleatorio = new Random(99);
        for (var caso = 0; caso < 400; caso++)
        {
            const int esquerda = 4, direita = 4;
            var pares = new List<(int A, int B)>();
            for (var a = 0; a < esquerda; a++)
                for (var b = 0; b < direita; b++)
                    if (aleatorio.Next(100) < 45) pares.Add((a, b));

            var (rede, fonte, sorvedouro) = Geradores.Bipartida(esquerda, direita, pares);
            var emparelhamento = Dinic.Resolver(rede, fonte, sorvedouro).Valor;
            Assert.Equal(MenorCobertura(esquerda, direita, pares), emparelhamento);
        }
    }

    private static int MenorCobertura(int esquerda, int direita, List<(int A, int B)> pares)
    {
        var n = esquerda + direita;
        for (var tamanho = 0; tamanho <= n; tamanho++)
        {
            for (var mascara = 0; mascara < 1 << n; mascara++)
            {
                if (System.Numerics.BitOperations.PopCount((uint)mascara) != tamanho) continue;
                var cobre = true;
                foreach (var (a, b) in pares)
                {
                    var temA = (mascara & (1 << a)) != 0;
                    var temB = (mascara & (1 << (esquerda + b))) != 0;
                    if (!temA && !temB) { cobre = false; break; }
                }
                if (cobre) return tamanho;
            }
        }
        return n;
    }

    /// <summary>
    /// Hall, 1935: o lado esquerdo inteiro so pode ser emparelhado quando TODO
    /// subconjunto dele tem pelo menos tantos vizinhos quanto elementos. Quando
    /// falha, existe um subconjunto que testemunha a falha, e o teste procura ele.
    /// </summary>
    [Fact]
    public void QuandoHallFalhaExisteUmConjuntoQueTestemunha()
    {
        var aleatorio = new Random(31);
        var achouAlgumaFalha = false;
        for (var caso = 0; caso < 300; caso++)
        {
            const int esquerda = 4, direita = 4;
            var pares = new List<(int A, int B)>();
            for (var a = 0; a < esquerda; a++)
                for (var b = 0; b < direita; b++)
                    if (aleatorio.Next(100) < 35) pares.Add((a, b));

            var (rede, fonte, sorvedouro) = Geradores.Bipartida(esquerda, direita, pares);
            var completo = Dinic.Resolver(rede, fonte, sorvedouro).Valor == esquerda;

            var existeTestemunha = false;
            for (var mascara = 1; mascara < 1 << esquerda; mascara++)
            {
                var quantos = System.Numerics.BitOperations.PopCount((uint)mascara);
                var vizinhos = new HashSet<int>();
                foreach (var (a, b) in pares) if ((mascara & (1 << a)) != 0) vizinhos.Add(b);
                if (vizinhos.Count < quantos) { existeTestemunha = true; break; }
            }
            Assert.Equal(completo, !existeTestemunha);
            if (existeTestemunha) achouAlgumaFalha = true;
        }
        Assert.True(achouAlgumaFalha, "nenhum caso falhou em Hall; o teste nao exercitou o lado interessante");
    }

    /// <summary>
    /// O grafo bipartido completo de n por n tem emparelhamento n, e a cobertura
    /// minima tambem e n: qualquer lado inteiro cobre tudo. E o caso em que
    /// Konig aperta.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void NoCompletoOEmparelhamentoEhTotal(int n)
    {
        var pares = new List<(int A, int B)>();
        for (var a = 0; a < n; a++)
            for (var b = 0; b < n; b++) pares.Add((a, b));
        var (rede, fonte, sorvedouro) = Geradores.Bipartida(n, n, pares);
        Assert.Equal(n, Dinic.Resolver(rede, fonte, sorvedouro).Valor);
    }
}
