namespace Conde.Fluxo;

/// <summary>
/// Dinic, 1970: em vez de um caminho por busca, uma FASE inteira de cada vez.
///
/// A fase tem duas partes. Primeiro uma busca em largura rotula cada vertice com
/// a distancia ate ele no residual, montando o grafo por niveis: so valem as
/// arestas que sobem exatamente um nivel. Depois uma busca em profundidade manda
/// quantos caminhos couberem DENTRO desse grafo, ate nao passar mais nada.
///
/// Duas ideias fazem a conta fechar:
///
/// - A distancia da fonte ao sorvedouro cresce estritamente a cada fase, entao
///   sao no maximo V fases.
/// - Dentro da fase, o ponteiro de cada vertice so anda para a frente. Uma aresta
///   que nao levou a lugar nenhum nao e visitada de novo naquela fase, o que
///   evita refazer o mesmo beco sem saida. Sem esse ponteiro o algoritmo continua
///   CERTO e perde a complexidade, que e o jeito mais silencioso de errar.
///
/// Em rede com capacidade unitaria, que e o caso de emparelhamento, o custo cai
/// para O(E * raiz(V)), e e por isso que o Hopcroft-Karp e, no fundo, este
/// algoritmo.
/// </summary>
public static class Dinic
{
    public static Resultado Resolver(Rede rede, int fonte, int sorvedouro)
    {
        ArgumentNullException.ThrowIfNull(rede);
        if (fonte == sorvedouro) throw new ArgumentException("fonte e sorvedouro iguais");
        rede.ZerarFluxo();
        long valor = 0, fases = 0, visitas = 0;
        var nivel = new int[rede.Vertices];
        var ponteiro = new int[rede.Vertices];

        while (Niveis(rede, fonte, sorvedouro, nivel, ref visitas))
        {
            Array.Clear(ponteiro);
            fases++;
            long passou;
            while ((passou = Bloqueio(rede, fonte, sorvedouro, long.MaxValue, nivel, ponteiro, ref visitas)) > 0)
                valor += passou;
        }
        return new Resultado(valor, Alcancaveis.CorteMinimo(rede, fonte), fases, visitas);
    }

    /// <summary>Rotula por distancia. Devolve falso quando o sorvedouro ficou fora.</summary>
    private static bool Niveis(Rede rede, int fonte, int sorvedouro, int[] nivel, ref long visitas)
    {
        Array.Fill(nivel, -1);
        nivel[fonte] = 0;
        var fila = new Queue<int>();
        fila.Enqueue(fonte);
        while (fila.Count > 0)
        {
            var v = fila.Dequeue();
            foreach (var i in rede.Saindo(v))
            {
                visitas++;
                if (rede.Residual(i) <= 0) continue;
                var u = rede.Destino(i);
                if (nivel[u] != -1) continue;
                nivel[u] = nivel[v] + 1;
                fila.Enqueue(u);
            }
        }
        return nivel[sorvedouro] != -1;
    }

    /// <summary>
    /// Desce pelo grafo por niveis mandando o maximo que couber. O ponteiro de
    /// cada vertice guarda ate onde a lista de saida ja foi tentada NESTA fase.
    /// </summary>
    private static long Bloqueio(Rede rede, int v, int sorvedouro, long limite, int[] nivel, int[] ponteiro, ref long visitas)
    {
        if (v == sorvedouro) return limite;
        var saindo = rede.Saindo(v);
        for (; ponteiro[v] < saindo.Count; ponteiro[v]++)
        {
            var i = saindo[ponteiro[v]];
            visitas++;
            var u = rede.Destino(i);
            if (rede.Residual(i) <= 0 || nivel[u] != nivel[v] + 1) continue;
            var passou = Bloqueio(rede, u, sorvedouro, Math.Min(limite, rede.Residual(i)), nivel, ponteiro, ref visitas);
            if (passou <= 0) continue;
            rede.Empurrar(i, passou);
            return passou;
        }
        return 0;
    }
}
