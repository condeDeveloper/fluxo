using Conde.Fluxo;
using Xunit;

namespace Conde.Fluxo.Testes;

/// <summary>
/// A rede de livro que mostra por que a regra do Edmonds-Karp existe, e a
/// correcao que a medicao me impos sobre ela.
///
/// Eu escrevi que a busca em PROFUNDIDADE cairia nos 2C caminhos. Nao cai. O
/// limite classico vale para a pior escolha possivel de caminho, e a definicao do
/// metodo deixa essa escolha em aberto; uma busca em profundidade que empurra o
/// gargalo do caminho inteiro nao realiza a pior escolha por acaso.
/// </summary>
public class AdversariaTest
{
    /// <summary>
    /// O numero que me corrigiu: nas 120 ordens de insercao possiveis das cinco
    /// ligacoes, a PIOR gasta 4 caminhos, nao 2C. E todas chegam ao maximo.
    ///
    /// Um teste que so afirmasse "o Ford-Fulkerson e lento aqui" teria passado
    /// sem nunca ser verdade.
    /// </summary>
    [Fact]
    public void NenhumaOrdemDeInsercaoDegenera()
    {
        const long c = 50;
        var ligacoes = new (int De, int Para, long Cap)[] { (0, 1, c), (0, 2, c), (1, 3, c), (2, 3, c), (1, 2, 1) };
        var pior = 0L;
        var quantasOrdens = 0;

        foreach (var ordem in Permutacoes([0, 1, 2, 3, 4]))
        {
            var rede = new Rede(4);
            foreach (var k in ordem) rede.Ligar(ligacoes[k].De, ligacoes[k].Para, ligacoes[k].Cap);
            var r = FordFulkerson.Resolver(rede, 0, 3);
            Assert.Equal(2 * c, r.Valor);
            pior = Math.Max(pior, r.Caminhos);
            quantasOrdens++;
        }
        Assert.Equal(120, quantasOrdens);
        Assert.Equal(4, pior);
    }

    /// <summary>
    /// Preferir a aresta estreita tambem nao degenera: continua em 4. Assim que
    /// ela satura, a profundidade encontra a saida direta e leva o resto de uma
    /// vez. Mexer na ordem nao basta.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(1000)]
    [InlineData(1000000)]
    public void PreferirAArestaEstreitaTambemNaoDegenera(long c)
    {
        var rede = Geradores.Adversaria(c);
        var r = FordFulkerson.ResolverPreferindo(rede, 0, 3, Geradores.ArestaDoMeio);
        Assert.Equal(2 * c, r.Valor);
        Assert.True(r.Caminhos <= 4, $"deu {r.Caminhos} caminhos");
    }

    /// <summary>
    /// A execucao de 2C EXISTE, e aqui ela e exibida caminho a caminho.
    ///
    /// Alternando 0-1-2-3 e 0-2-1-3, cada passo carrega exatamente uma unidade: o
    /// primeiro satura a aresta do meio, o segundo a desatura pela reversa, e
    /// assim por diante. Sao 2C passos legais do metodo para um fluxo que o
    /// Edmonds-Karp resolve em dois.
    ///
    /// E isso que o limite O(E vezes o valor do fluxo) quer dizer: ele e
    /// alcancavel, so nao por acidente.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(500)]
    public void OAdversarioAlcancaOsDoisCeCaminhos(long c)
    {
        var rede = Geradores.Adversaria(c);
        int[] porCima = [0, 1, 2, 3];
        int[] porBaixo = [0, 2, 1, 3];

        long valor = 0, passos = 0;
        for (var k = 0; k < 2 * c; k++)
        {
            var folga = FordFulkerson.AumentarPor(rede, k % 2 == 0 ? porCima : porBaixo);
            Assert.True(folga == 1, $"o passo {k} carregou {folga} em vez de 1");
            valor += folga;
            passos++;
        }
        Assert.Equal(2 * c, valor);
        Assert.Equal(2 * c, passos);

        // E depois disso nao sobra caminho nenhum: o fluxo e maximo mesmo.
        Assert.Equal(0, FordFulkerson.AumentarPor(rede, porCima));
        Assert.Equal(ForcaBruta.CorteMinimo(rede, 0, 3), valor);
    }

    /// <summary>
    /// Os outros tres nao dependem de C: a mesma rede com capacidade dez ou um
    /// milhao gasta o mesmo trabalho. E essa independencia que separa um metodo
    /// de um algoritmo.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(1000)]
    [InlineData(1000000)]
    [InlineData(1000000000)]
    public void OsOutrosTresNaoDependemDaCapacidade(long c)
    {
        var modelo = Geradores.Adversaria(c);

        var ek = modelo.Copiar();
        var rek = EdmondsKarp.Resolver(ek, 0, 3);
        Assert.Equal(2 * c, rek.Valor);
        Assert.True(rek.Caminhos <= 3, $"edmonds-karp gastou {rek.Caminhos} caminhos com C={c}");

        var dn = modelo.Copiar();
        var rdn = Dinic.Resolver(dn, 0, 3);
        Assert.Equal(2 * c, rdn.Valor);
        Assert.True(rdn.Caminhos <= 3, $"dinic gastou {rdn.Caminhos} fases com C={c}");

        // O escalonamento ataca bit a bit: o trabalho acompanha log2(C).
        var es = modelo.Copiar();
        var res = Escala.Resolver(es, 0, 3);
        Assert.Equal(2 * c, res.Valor);
        var bits = (int)Math.Ceiling(Math.Log2(c + 1));
        Assert.True(res.Caminhos <= 4L * bits + 8, $"escala gastou {res.Caminhos} caminhos com C={c} ({bits} bits)");
    }

    /// <summary>
    /// O metodo e lento, nao errado: com capacidade inteira ele sempre chega ao
    /// maximo. Conferido contra a enumeracao de cortes.
    /// </summary>
    [Fact]
    public void OMetodoEhLentoENaoErrado()
    {
        for (var c = 1; c <= 60; c++)
        {
            var rede = Geradores.Adversaria(c);
            Assert.Equal(ForcaBruta.CorteMinimo(rede, 0, 3), FordFulkerson.Resolver(rede, 0, 3).Valor);
        }
    }

    private static IEnumerable<int[]> Permutacoes(int[] v)
    {
        if (v.Length <= 1) { yield return v; yield break; }
        for (var i = 0; i < v.Length; i++)
        {
            var resto = v.Where((_, k) => k != i).ToArray();
            foreach (var p in Permutacoes(resto)) yield return [v[i], .. p];
        }
    }
}
