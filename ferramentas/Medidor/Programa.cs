using System.Diagnostics;
using System.Globalization;
using Conde.Fluxo;

// A ferramenta RODA para registrar numeros. Nao ha asseveracao de tempo: os
// numeros que entram no README saem daqui, nunca da minha cabeca.

// Com InvariantGlobalization a cultura pt-BR nao existe e lancaria. O formato vai
// montado na mao, que e o padrao ja adotado nos outros projetos.
var br = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };
string N(long x) => x.ToString("N0", br);

var comando = args.Length > 0 ? args[0] : "tudo";
switch (comando)
{
    case "capacidade": Capacidade(); break;
    case "grade": Grade(); break;
    case "ordens": Ordens(); break;
    default: Capacidade(); Console.WriteLine(); Ordens(); Console.WriteLine(); Grade(); break;
}

return;

// A pergunta que separa metodo de algoritmo: o trabalho depende do VALOR das
// capacidades ou do tamanho do grafo?
void Capacidade()
{
    Console.WriteLine("rede adversaria: o trabalho depende da capacidade?");
    Console.WriteLine();
    Console.WriteLine("            C   ford-fulkerson   edmonds-karp   dinic   escala   adversario exibido");
    foreach (long c in new long[] { 10, 100, 1_000, 10_000, 100_000, 1_000_000 })
    {
        var modelo = Geradores.Adversaria(c);
        var ff = FordFulkerson.Resolver(modelo.Copiar(), 0, 3).Caminhos;
        var ek = EdmondsKarp.Resolver(modelo.Copiar(), 0, 3).Caminhos;
        var dn = Dinic.Resolver(modelo.Copiar(), 0, 3).Caminhos;
        var es = Escala.Resolver(modelo.Copiar(), 0, 3).Caminhos;
        Console.WriteLine($"  {N(c),11}   {N(ff),14}   {N(ek),12}   {N(dn),5}   {N(es),6}   {N(2 * c),18}");
    }
    Console.WriteLine();
    Console.WriteLine("  A ultima coluna e a execucao LEGAL de 2C passos, exibida caminho a caminho");
    Console.WriteLine("  pelo teste. Nenhuma busca em profundidade a produz: medi as 120 ordens de");
    Console.WriteLine("  insercao possiveis e a pior gasta 4 caminhos.");
}

// Todas as ordens de insercao da rede adversaria, para mostrar que a escolha do
// caminho e que manda, nao a topologia.
void Ordens()
{
    Console.WriteLine("as 120 ordens de insercao da rede adversaria (C = 1.000)");
    Console.WriteLine();
    const long c = 1000;
    var ligacoes = new (int De, int Para, long Cap)[] { (0, 1, c), (0, 2, c), (1, 3, c), (2, 3, c), (1, 2, 1) };
    var histograma = new SortedDictionary<long, int>();
    foreach (var ordem in Permutacoes([0, 1, 2, 3, 4]))
    {
        var rede = new Rede(4);
        foreach (var k in ordem) rede.Ligar(ligacoes[k].De, ligacoes[k].Para, ligacoes[k].Cap);
        var caminhos = FordFulkerson.Resolver(rede, 0, 3).Caminhos;
        histograma.TryGetValue(caminhos, out var quantas);
        histograma[caminhos] = quantas + 1;
    }
    Console.WriteLine("  caminhos   quantas ordens");
    foreach (var (caminhos, quantas) in histograma)
        Console.WriteLine($"  {N(caminhos),8}   {N(quantas),14}");
}

// Carga realista: grade com capacidade sorteada. Aqui o que importa nao e o
// numero de caminhos, e o trabalho de verdade, medido em arestas visitadas.
void Grade()
{
    Console.WriteLine("grade com capacidade sorteada: arestas visitadas e tempo");
    Console.WriteLine();
    Console.WriteLine("  grade      vertices   fluxo   edmonds-karp        dinic       escala   | ms (dinic)");
    foreach (var lado in new[] { 10, 20, 40, 60 })
    {
        var (modelo, fonte, sorvedouro) = Geradores.Grade(lado, lado, 1);
        var ek = EdmondsKarp.Resolver(modelo.Copiar(), fonte, sorvedouro);

        var redeDinic = modelo.Copiar();
        var relogio = Stopwatch.StartNew();
        var dn = Dinic.Resolver(redeDinic, fonte, sorvedouro);
        relogio.Stop();

        var es = Escala.Resolver(modelo.Copiar(), fonte, sorvedouro);
        if (ek.Valor != dn.Valor || dn.Valor != es.Valor)
            throw new InvalidOperationException("os algoritmos discordaram; isso e defeito, nao medida");

        Console.WriteLine($"  {lado}x{lado,-8}  {N(modelo.Vertices),8}  {N(dn.Valor),6}   {N(ek.Visitas),12}  {N(dn.Visitas),11}  {N(es.Visitas),11}   | {N(relogio.ElapsedMilliseconds),6}");
    }
}

static IEnumerable<int[]> Permutacoes(int[] v)
{
    if (v.Length <= 1) { yield return v; yield break; }
    for (var i = 0; i < v.Length; i++)
    {
        var resto = v.Where((_, k) => k != i).ToArray();
        foreach (var p in Permutacoes(resto)) yield return [v[i], .. p];
    }
}
