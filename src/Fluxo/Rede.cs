namespace Conde.Fluxo;

/// <summary>
/// Uma rede de fluxo, guardada como lista de arestas com a RESIDUAL embutida.
///
/// O truque de representacao e o mesmo de todo codigo serio de fluxo, e vale
/// entender porque ele e o que faz os algoritmos ficarem curtos: cada aresta
/// entra junto com a sua reversa, em posicoes consecutivas (2k e 2k+1). Mandar
/// fluxo por uma e tirar da outra, e a reversa com XOR 1.
///
/// A reversa nao e enfeite de implementacao, e o que da ao algoritmo o direito de
/// se ARREPENDER. Sem ela, mandar fluxo por um caminho ruim e definitivo, e o
/// maximo encontrado passa a depender da ordem em que os caminhos apareceram. Com
/// ela, empurrar de volta por uma reversa cancela uma decisao anterior, e e por
/// isso que qualquer ordem de caminhos chega ao mesmo maximo.
/// </summary>
public sealed class Rede
{
    private readonly List<int> _destino = [];
    private readonly List<long> _capacidade = [];
    private readonly List<long> _fluxo = [];
    private readonly List<int>[] _saindo;

    public Rede(int vertices)
    {
        if (vertices < 0) throw new ArgumentOutOfRangeException(nameof(vertices));
        Vertices = vertices;
        _saindo = new List<int>[vertices];
        for (var v = 0; v < vertices; v++) _saindo[v] = [];
    }

    public int Vertices { get; }

    /// <summary>Quantas arestas, contando as reversas. Sempre par.</summary>
    public int Arestas => _destino.Count;

    /// <summary>
    /// Liga de -> para com a capacidade dada, e cria a reversa com capacidade
    /// zero. Devolve o indice da aresta direta; a reversa e esse indice com XOR 1.
    /// </summary>
    public int Ligar(int de, int para, long capacidade)
    {
        if (de < 0 || de >= Vertices) throw new ArgumentOutOfRangeException(nameof(de));
        if (para < 0 || para >= Vertices) throw new ArgumentOutOfRangeException(nameof(para));
        if (capacidade < 0) throw new ArgumentOutOfRangeException(nameof(capacidade), "capacidade negativa nao e fluxo, e outro problema");
        var i = _destino.Count;
        _destino.Add(para); _capacidade.Add(capacidade); _fluxo.Add(0); _saindo[de].Add(i);
        _destino.Add(de); _capacidade.Add(0); _fluxo.Add(0); _saindo[para].Add(i + 1);
        return i;
    }

    public int Destino(int aresta) => _destino[aresta];
    public long Capacidade(int aresta) => _capacidade[aresta];
    public long FluxoDe(int aresta) => _fluxo[aresta];

    /// <summary>A reversa de uma aresta. Um XOR, porque elas nascem em par.</summary>
    public static int Reversa(int aresta) => aresta ^ 1;

    /// <summary>O quanto ainda cabe. Zero quer dizer que a aresta sumiu do residual.</summary>
    public long Residual(int aresta) => _capacidade[aresta] - _fluxo[aresta];

    public IReadOnlyList<int> Saindo(int vertice) => _saindo[vertice];

    /// <summary>
    /// Manda fluxo por uma aresta e tira da reversa. A simetria e o que mantem a
    /// conservacao: o que entra num vertice sai dele.
    /// </summary>
    public void Empurrar(int aresta, long quanto)
    {
        if (quanto < 0) throw new ArgumentOutOfRangeException(nameof(quanto));
        if (quanto > Residual(aresta))
            throw new InvalidOperationException($"a aresta {aresta} tem residual {Residual(aresta)} e pediram {quanto}");
        _fluxo[aresta] += quanto;
        _fluxo[aresta ^ 1] -= quanto;
    }

    /// <summary>Zera o fluxo sem mexer na topologia, para rodar outro algoritmo na mesma rede.</summary>
    public void ZerarFluxo()
    {
        for (var i = 0; i < _fluxo.Count; i++) _fluxo[i] = 0;
    }

    /// <summary>Uma copia independente, com as mesmas arestas e fluxo zero.</summary>
    public Rede Copiar()
    {
        var nova = new Rede(Vertices);
        for (var i = 0; i < _destino.Count; i += 2)
            nova.Ligar(_destino[i + 1], _destino[i], _capacidade[i]);
        return nova;
    }

    /// <summary>
    /// O fluxo que sai do vertice menos o que entra. Na fonte isso e o valor do
    /// fluxo, no sorvedouro e o negativo dele, e em todo o resto tem de ser zero:
    /// e a lei de conservacao, e conferi-la pega qualquer empurrao mal feito.
    /// </summary>
    public long Saldo(int vertice)
    {
        long saldo = 0;
        foreach (var i in _saindo[vertice]) saldo += _fluxo[i];
        return saldo;
    }

    /// <summary>O valor do fluxo, medido na fonte.</summary>
    public long Valor(int fonte) => Saldo(fonte);
}
