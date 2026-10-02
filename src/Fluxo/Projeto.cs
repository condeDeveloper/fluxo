namespace Conde.Fluxo;

/// <summary>
/// Selecao de projetos, ou o problema do fechamento de peso maximo.
///
/// Cada projeto da um lucro, cada maquina custa, e um projeto so pode ser feito
/// se todas as maquinas de que ele depende forem compradas. A pergunta e qual
/// subconjunto maximiza lucro menos custo, e ela parece combinatoria pura: sao
/// 2^n subconjuntos e as dependencias se cruzam.
///
/// A reducao e de Picard (1976) e e o melhor exemplo de por que corte minimo
/// importa alem de encanamento. Monta-se:
///
///   fonte -> projeto, capacidade = lucro
///   maquina -> sorvedouro, capacidade = custo
///   projeto -> maquina de que ele depende, capacidade INFINITA
///
/// Um corte finito nunca pode atravessar uma aresta infinita, entao ele nunca
/// separa um projeto escolhido da maquina de que ele depende: a restricao de
/// dependencia vira uma consequencia da estrutura, e nao uma regra que alguem
/// precisa impor.
///
/// A conta fecha assim: todo corte tem capacidade igual a (lucro dos projetos
/// REJEITADOS) mais (custo das maquinas COMPRADAS). Minimizar isso e maximizar
/// o lucro total menos o custo, e a resposta e o lucro de todos os projetos menos
/// o corte minimo.
/// </summary>
public static class Projeto
{
    /// <summary>O lucro liquido maximo e quais projetos entram.</summary>
    public static (long Lucro, bool[] Escolhidos, bool[] Maquinas) Melhor(
        IReadOnlyList<long> lucros,
        IReadOnlyList<long> custos,
        IEnumerable<(int Projeto, int Maquina)> dependencias)
    {
        ArgumentNullException.ThrowIfNull(lucros);
        ArgumentNullException.ThrowIfNull(custos);
        ArgumentNullException.ThrowIfNull(dependencias);
        foreach (var l in lucros) if (l < 0) throw new ArgumentException("lucro negativo: rejeite o projeto antes", nameof(lucros));
        foreach (var c in custos) if (c < 0) throw new ArgumentException("custo negativo nao e custo", nameof(custos));

        var p = lucros.Count;
        var m = custos.Count;
        var fonte = p + m;
        var sorvedouro = fonte + 1;
        var rede = new Rede(p + m + 2);

        long total = 0;
        for (var i = 0; i < p; i++) { rede.Ligar(fonte, i, lucros[i]); total += lucros[i]; }
        for (var j = 0; j < m; j++) rede.Ligar(p + j, sorvedouro, custos[j]);

        // Infinito na pratica: maior que qualquer corte possivel, sem estourar o
        // long quando varias dessas entram na mesma soma.
        var infinito = total + 1;
        foreach (var (projeto, maquina) in dependencias) rede.Ligar(projeto, p + maquina, infinito);

        var r = Dinic.Resolver(rede, fonte, sorvedouro);
        var escolhidos = new bool[p];
        var maquinas = new bool[m];
        for (var i = 0; i < p; i++) escolhidos[i] = r.Corte.NoLadoDaFonte(i);
        for (var j = 0; j < m; j++) maquinas[j] = r.Corte.NoLadoDaFonte(p + j);
        return (total - r.Valor, escolhidos, maquinas);
    }

    /// <summary>
    /// O juiz desta reducao: enumera os 2^p subconjuntos de projetos, compra as
    /// maquinas necessarias e fica com o melhor. Sem fluxo nenhum.
    /// </summary>
    public static long MelhorPorForcaBruta(
        IReadOnlyList<long> lucros,
        IReadOnlyList<long> custos,
        IEnumerable<(int Projeto, int Maquina)> dependencias)
    {
        ArgumentNullException.ThrowIfNull(lucros);
        ArgumentNullException.ThrowIfNull(custos);
        var deps = dependencias as (int Projeto, int Maquina)[] ?? [.. dependencias];
        var p = lucros.Count;
        if (p > 22) throw new ArgumentException("2^p cresce rapido; o juiz vai ate 22 projetos", nameof(lucros));

        long melhor = 0;
        for (long mascara = 0; mascara < 1L << p; mascara++)
        {
            long lucro = 0;
            var precisa = new bool[custos.Count];
            for (var i = 0; i < p; i++)
            {
                if (((mascara >> i) & 1) == 0) continue;
                lucro += lucros[i];
                foreach (var (projeto, maquina) in deps) if (projeto == i) precisa[maquina] = true;
            }
            for (var j = 0; j < custos.Count; j++) if (precisa[j]) lucro -= custos[j];
            melhor = Math.Max(melhor, lucro);
        }
        return melhor;
    }
}
