namespace Conde.Fluxo;

/// <summary>
/// Redes com resposta conhecida de antemao, ou com defeito conhecido.
///
/// Rede sorteada mede tempo medio e nao testa nada: ninguem sabe a resposta sem
/// resolver, e o sorteio quase nunca cai nos casos que quebram. Estas sabem.
/// </summary>
public static class Geradores
{
    /// <summary>
    /// A rede de livro do limite do Ford-Fulkerson.
    ///
    /// Quatro vertices: 0 -> 1 e 0 -> 2 com capacidade C, 1 -> 3 e 2 -> 3 com C,
    /// e 1 -> 2 com capacidade UM. O maximo e 2C e sai em dois caminhos com busca
    /// em largura.
    ///
    /// A alternancia que leva a 2C caminhos e uma execucao LEGAL do metodo, nao o
    /// que uma busca em profundidade faz: medi as 120 ordens de insercao e a pior
    /// gasta 4 caminhos. Quem exibe a execucao de 2C e o teste, passo a passo,
    /// com FordFulkerson.AumentarPor.
    /// </summary>
    public static Rede Adversaria(long capacidade)
    {
        if (capacidade < 1) throw new ArgumentOutOfRangeException(nameof(capacidade));
        var r = new Rede(4);
        r.Ligar(0, 1, capacidade);
        r.Ligar(0, 2, capacidade);
        r.Ligar(1, 3, capacidade);
        r.Ligar(2, 3, capacidade);
        r.Ligar(1, 2, 1);
        return r;
    }

    /// <summary>
    /// O indice da aresta estreita do meio na rede adversaria. Fica aqui para o
    /// teste nao precisar contar ligacoes na mao.
    /// </summary>
    public const int ArestaDoMeio = 8;

    /// <summary>
    /// Grade de m por n com capacidade sorteada. Fonte liga na primeira coluna,
    /// sorvedouro recebe da ultima. Serve de carga realista para medir.
    /// </summary>
    public static (Rede Rede, int Fonte, int Sorvedouro) Grade(int linhas, int colunas, int semente, long capacidadeMaxima = 20)
    {
        if (linhas < 1 || colunas < 1) throw new ArgumentOutOfRangeException(nameof(linhas));
        var aleatorio = new Random(semente);
        var n = linhas * colunas;
        var r = new Rede(n + 2);
        var fonte = n;
        var sorvedouro = n + 1;
        int V(int i, int j) => i * colunas + j;

        for (var i = 0; i < linhas; i++)
        {
            r.Ligar(fonte, V(i, 0), capacidadeMaxima);
            r.Ligar(V(i, colunas - 1), sorvedouro, capacidadeMaxima);
            for (var j = 0; j < colunas; j++)
            {
                if (j + 1 < colunas) r.Ligar(V(i, j), V(i, j + 1), 1 + aleatorio.NextInt64(capacidadeMaxima));
                if (i + 1 < linhas) r.Ligar(V(i, j), V(i + 1, j), 1 + aleatorio.NextInt64(capacidadeMaxima));
            }
        }
        return (r, fonte, sorvedouro);
    }

    /// <summary>
    /// Rede sorteada pequena, para a comparacao exaustiva contra a enumeracao de
    /// cortes. O tamanho e pequeno de proposito: o juiz custa 2^(n-2).
    /// </summary>
    public static (Rede Rede, int Fonte, int Sorvedouro) Pequena(int vertices, int arestas, int semente, long capacidadeMaxima = 9)
    {
        if (vertices < 2) throw new ArgumentOutOfRangeException(nameof(vertices));
        var aleatorio = new Random(semente);
        var r = new Rede(vertices);
        for (var k = 0; k < arestas; k++)
        {
            var de = aleatorio.Next(vertices);
            var para = aleatorio.Next(vertices);
            if (de == para) continue;
            r.Ligar(de, para, aleatorio.NextInt64(capacidadeMaxima + 1));
        }
        return (r, 0, vertices - 1);
    }

    /// <summary>
    /// Rede bipartida de emparelhamento: fonte liga em cada esquerda com
    /// capacidade 1, cada direita liga no sorvedouro com 1, e as arestas do meio
    /// tambem valem 1. O fluxo maximo e o tamanho do emparelhamento maximo, e o
    /// corte minimo e uma cobertura minima por vertices, que e o teorema de
    /// Konig de 1931 saindo do mesmo lugar.
    /// </summary>
    public static (Rede Rede, int Fonte, int Sorvedouro) Bipartida(int esquerda, int direita, IEnumerable<(int A, int B)> pares)
    {
        ArgumentNullException.ThrowIfNull(pares);
        var n = esquerda + direita + 2;
        var r = new Rede(n);
        var fonte = esquerda + direita;
        var sorvedouro = fonte + 1;
        for (var a = 0; a < esquerda; a++) r.Ligar(fonte, a, 1);
        for (var b = 0; b < direita; b++) r.Ligar(esquerda + b, sorvedouro, 1);
        foreach (var (a, b) in pares) r.Ligar(a, esquerda + b, 1);
        return (r, fonte, sorvedouro);
    }

    /// <summary>
    /// Todas as arestas com capacidade um. Nessa rede o fluxo maximo e o numero
    /// de caminhos disjuntos por aresta, que e o teorema de Menger de 1927.
    /// </summary>
    public static Rede Unitaria(int vertices, IEnumerable<(int De, int Para)> arestas)
    {
        ArgumentNullException.ThrowIfNull(arestas);
        var r = new Rede(vertices);
        foreach (var (de, para) in arestas) r.Ligar(de, para, 1);
        return r;
    }
}
