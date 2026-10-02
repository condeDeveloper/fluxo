using Conde.Fluxo;
using Xunit;

namespace Conde.Fluxo.Testes;

/// <summary>
/// O teste principal: os quatro algoritmos contra a ENUMERACAO DE CORTES.
///
/// O teorema do fluxo maximo e corte minimo e justamente o que se quer verificar,
/// entao o juiz nao pode ser outro algoritmo de fluxo: os dois poderiam errar
/// junto. O juiz enumera os 2^(n-2) cortes e devolve o menor, usando so a
/// definicao de capacidade de corte.
/// </summary>
public class TeoremaTest
{
    /// <summary>
    /// 3.000 redes sorteadas de 8 vertices. Cada uma tem 64 cortes enumerados, e
    /// os quatro algoritmos precisam bater com o minimo deles. Alem do valor, o
    /// corte que cada algoritmo devolve e conferido: ele tem de separar a fonte
    /// do sorvedouro e ter capacidade igual ao fluxo.
    /// </summary>
    [Fact]
    public void OsQuatroBatemComAEnumeracaoDeCortes()
    {
        const int vertices = 8;
        Assert.Equal(64, ForcaBruta.QuantosCortes(vertices));
        var conferidas = 0;

        for (var semente = 1; semente <= 3000; semente++)
        {
            var (modelo, fonte, sorvedouro) = Geradores.Pequena(vertices, 14, semente);
            var esperado = ForcaBruta.CorteMinimo(modelo, fonte, sorvedouro);

            foreach (var (nome, resolver) in Algoritmos())
            {
                var rede = modelo.Copiar();
                var r = resolver(rede, fonte, sorvedouro);
                Assert.True(esperado == r.Valor, $"{nome} deu {r.Valor} e o minimo dos cortes e {esperado} (semente {semente})");
                Assert.True(r.Confere(rede, fonte, sorvedouro), $"{nome} devolveu corte que nao fecha com o valor (semente {semente})");
            }
            conferidas++;
        }
        Assert.Equal(3000, conferidas);
    }

    /// <summary>
    /// A lei de conservacao, conferida vertice a vertice depois de cada execucao:
    /// o que entra sai, menos na fonte e no sorvedouro. Um empurrao mal feito
    /// pode dar o valor certo e deixar o fluxo invalido, e so esta conferencia
    /// pega isso.
    /// </summary>
    [Fact]
    public void OFluxoConservaEmTodoVerticeDoMeio()
    {
        for (var semente = 1; semente <= 400; semente++)
        {
            var (modelo, fonte, sorvedouro) = Geradores.Pequena(9, 18, semente);
            foreach (var (nome, resolver) in Algoritmos())
            {
                var rede = modelo.Copiar();
                var r = resolver(rede, fonte, sorvedouro);
                for (var v = 0; v < rede.Vertices; v++)
                {
                    if (v == fonte || v == sorvedouro) continue;
                    Assert.True(rede.Saldo(v) == 0, $"{nome} deixou saldo {rede.Saldo(v)} em {v}");
                }
                Assert.Equal(r.Valor, rede.Valor(fonte));
                Assert.Equal(-r.Valor, rede.Saldo(sorvedouro));
            }
        }
    }

    /// <summary>
    /// Nenhuma aresta pode passar da capacidade, e o fluxo da reversa e sempre o
    /// negativo do da direta. Sao as duas invariantes que o Empurrar mantem, e
    /// perder a segunda faria o residual mentir sem quebrar nada visivel.
    /// </summary>
    [Fact]
    public void NinguemPassaDaCapacidadeENemQuebraASimetria()
    {
        for (var semente = 1; semente <= 400; semente++)
        {
            var (modelo, fonte, sorvedouro) = Geradores.Pequena(9, 18, semente);
            foreach (var (_, resolver) in Algoritmos())
            {
                var rede = modelo.Copiar();
                resolver(rede, fonte, sorvedouro);
                for (var i = 0; i < rede.Arestas; i += 2)
                {
                    Assert.InRange(rede.FluxoDe(i), -rede.Capacidade(i + 1), rede.Capacidade(i));
                    Assert.Equal(rede.FluxoDe(i), -rede.FluxoDe(i + 1));
                }
            }
        }
    }

    /// <summary>
    /// O corte que sai dos alcancaveis no residual tem capacidade igual ao valor.
    /// E a parte construtiva do teorema, e conferi-la contra a enumeracao mostra
    /// que o corte achado nao e so UM corte minimo, e que o argumento fecha.
    /// </summary>
    [Fact]
    public void OCorteDosAlcancaveisEhMinimo()
    {
        for (var semente = 1; semente <= 600; semente++)
        {
            var (rede, fonte, sorvedouro) = Geradores.Pequena(8, 15, semente);
            var r = Dinic.Resolver(rede, fonte, sorvedouro);
            var corte = Alcancaveis.CorteMinimo(rede, fonte);
            Assert.True(corte.Separa(fonte, sorvedouro));
            Assert.Equal(r.Valor, corte.Capacidade(rede));
            Assert.Equal(ForcaBruta.CorteMinimo(rede, fonte, sorvedouro), corte.Capacidade(rede));
        }
    }

    /// <summary>
    /// Rede sem caminho nenhum da fonte ao sorvedouro: fluxo zero, e o corte
    /// vazio de capacidade zero. O caso de borda que um laco mal escrito
    /// transforma em laco infinito.
    /// </summary>
    [Fact]
    public void SemCaminhoOFluxoEhZero()
    {
        var rede = new Rede(4);
        rede.Ligar(0, 1, 10);
        rede.Ligar(2, 3, 10);
        foreach (var (nome, resolver) in Algoritmos())
        {
            var copia = rede.Copiar();
            var r = resolver(copia, 0, 3);
            Assert.True(r.Valor == 0, $"{nome} achou fluxo {r.Valor} onde nao ha caminho");
            Assert.True(r.Confere(copia, 0, 3));
        }
    }

    private static IEnumerable<(string Nome, Func<Rede, int, int, Resultado> Resolver)> Algoritmos()
    {
        yield return ("ford-fulkerson", FordFulkerson.Resolver);
        yield return ("edmonds-karp", EdmondsKarp.Resolver);
        yield return ("dinic", Dinic.Resolver);
        yield return ("escala", Escala.Resolver);
    }
}
