using Conde.Fluxo;
using Xunit;

namespace Conde.Fluxo.Testes;

/// <summary>
/// Selecao de projetos por corte minimo, de Picard (1976). O juiz e a enumeracao
/// dos 2^p subconjuntos, que nao sabe o que e fluxo.
/// </summary>
public class ProjetoTest
{
    /// <summary>
    /// 2.000 instancias sorteadas de 8 projetos e 6 maquinas: 256 subconjuntos
    /// enumerados em cada, contra a reducao a corte minimo.
    ///
    /// A reducao parece boa demais para ser verdade, e e esse o motivo de o teste
    /// ser exaustivo do lado do juiz: um erro na montagem da rede daria valores
    /// plausiveis e errados, nunca uma excecao.
    /// </summary>
    [Fact]
    public void ACorteMinimoBateComAEnumeracaoDeSubconjuntos()
    {
        var aleatorio = new Random(1976);
        for (var caso = 0; caso < 2000; caso++)
        {
            const int p = 8, m = 6;
            var lucros = new long[p];
            var custos = new long[m];
            for (var i = 0; i < p; i++) lucros[i] = aleatorio.Next(0, 30);
            for (var j = 0; j < m; j++) custos[j] = aleatorio.Next(0, 30);

            var deps = new List<(int Projeto, int Maquina)>();
            for (var i = 0; i < p; i++)
                for (var j = 0; j < m; j++)
                    if (aleatorio.Next(100) < 25) deps.Add((i, j));

            var (lucro, _, _) = Projeto.Melhor(lucros, custos, deps);
            Assert.Equal(Projeto.MelhorPorForcaBruta(lucros, custos, deps), lucro);
        }
    }

    /// <summary>
    /// A escolha devolvida tem de ser VIAVEL e valer o que foi prometido: todo
    /// projeto escolhido tem as maquinas dele compradas, e o lucro liquido fecha
    /// com a conta. Um valor certo com uma escolha invalida seria inutil na
    /// pratica, e so esta conferencia pega isso.
    /// </summary>
    [Fact]
    public void AEscolhaDevolvidaEhViavelEValeOQueDiz()
    {
        var aleatorio = new Random(7);
        for (var caso = 0; caso < 800; caso++)
        {
            const int p = 7, m = 5;
            var lucros = new long[p];
            var custos = new long[m];
            for (var i = 0; i < p; i++) lucros[i] = aleatorio.Next(0, 40);
            for (var j = 0; j < m; j++) custos[j] = aleatorio.Next(0, 40);
            var deps = new List<(int Projeto, int Maquina)>();
            for (var i = 0; i < p; i++)
                for (var j = 0; j < m; j++)
                    if (aleatorio.Next(100) < 30) deps.Add((i, j));

            var (lucro, escolhidos, maquinas) = Projeto.Melhor(lucros, custos, deps);

            foreach (var (projeto, maquina) in deps)
                if (escolhidos[projeto])
                    Assert.True(maquinas[maquina], $"o projeto {projeto} entrou sem a maquina {maquina}");

            long conta = 0;
            for (var i = 0; i < p; i++) if (escolhidos[i]) conta += lucros[i];
            for (var j = 0; j < m; j++) if (maquinas[j]) conta -= custos[j];
            Assert.Equal(lucro, conta);
        }
    }

    /// <summary>
    /// Um projeto que custa mais do que da nao entra. E o caso mais simples, e e
    /// o que pega a reducao montada com as capacidades trocadas de lado.
    /// </summary>
    [Fact]
    public void ProjetoQueNaoPagaAMaquinaFicaDeFora()
    {
        long[] lucros = [5];
        long[] custos = [10];
        var (lucro, escolhidos, maquinas) = Projeto.Melhor(lucros, custos, [(0, 0)]);
        Assert.Equal(0, lucro);
        Assert.False(escolhidos[0]);
        Assert.False(maquinas[0]);
    }

    /// <summary>
    /// Duas maquinas caras que se pagam JUNTAS por causa de um terceiro projeto:
    /// isoladamente nenhum dos dois primeiros vale, e os tres juntos valem. E o
    /// caso que uma heuristica gulosa erra e o corte minimo acerta.
    /// </summary>
    [Fact]
    public void ProjetosQueSoSePagamJuntosEntramJuntos()
    {
        // Os projetos 0 e 1 dependem da mesma maquina cara. Nenhum deles sozinho
        // paga os 15, mas os dois somam 20.
        long[] lucros = [11, 9];
        long[] custos = [15];
        var (lucro, escolhidos, maquinas) = Projeto.Melhor(lucros, custos, [(0, 0), (1, 0)]);
        Assert.Equal(5, lucro);
        Assert.True(escolhidos[0]);
        Assert.True(escolhidos[1]);
        Assert.True(maquinas[0]);
        Assert.Equal(5, Projeto.MelhorPorForcaBruta(lucros, custos, [(0, 0), (1, 0)]));
    }

    /// <summary>
    /// Sem dependencia nenhuma, a resposta e a soma dos lucros: nao ha o que
    /// pagar. E o caso degenerado que mostra se a rede foi montada com as arestas
    /// infinitas no lugar certo.
    /// </summary>
    [Fact]
    public void SemDependenciaOLucroEhASomaDeTudo()
    {
        long[] lucros = [3, 7, 11];
        long[] custos = [100, 200];
        var (lucro, _, maquinas) = Projeto.Melhor(lucros, custos, []);
        Assert.Equal(21, lucro);
        Assert.All(maquinas, m => Assert.False(m));
    }
}
