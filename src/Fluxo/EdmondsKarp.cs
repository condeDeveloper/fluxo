namespace Conde.Fluxo;

/// <summary>
/// Edmonds e Karp, 1972: o mesmo metodo de Ford-Fulkerson, com UMA regra a mais.
/// Sempre o caminho com menos arestas, achado por busca em largura.
///
/// A regra parece arbitraria e e o que transforma o metodo num algoritmo. O custo
/// passa a ser O(V*E^2), que nao depende das capacidades: a mesma rede com
/// capacidade um milhao ou com capacidade um gasta o mesmo numero de caminhos.
///
/// A prova tem duas partes, e a segunda e a bonita. Primeiro: a distancia da
/// fonte a cada vertice no residual nunca diminui ao longo da execucao. Segundo:
/// cada aresta so pode ser o GARGALO de um caminho O(V) vezes, porque depois de
/// ser gargalo ela some do residual e so volta quando a distancia do outro lado
/// tiver crescido em pelo menos dois. Como a distancia e limitada por V, sao
/// O(V*E) caminhos no total.
/// </summary>
public static class EdmondsKarp
{
    public static Resultado Resolver(Rede rede, int fonte, int sorvedouro)
    {
        ArgumentNullException.ThrowIfNull(rede);
        if (fonte == sorvedouro) throw new ArgumentException("fonte e sorvedouro iguais");
        rede.ZerarFluxo();
        long valor = 0, caminhos = 0, visitas = 0;
        var veioPor = new int[rede.Vertices];

        while (true)
        {
            Array.Fill(veioPor, -1);
            var fila = new Queue<int>();
            fila.Enqueue(fonte);
            veioPor[fonte] = -2;   // marca a fonte como visitada sem aresta de entrada

            while (fila.Count > 0 && veioPor[sorvedouro] == -1)
            {
                var v = fila.Dequeue();
                foreach (var i in rede.Saindo(v))
                {
                    visitas++;
                    if (rede.Residual(i) <= 0) continue;
                    var u = rede.Destino(i);
                    if (veioPor[u] != -1) continue;
                    veioPor[u] = i;
                    if (u == sorvedouro) break;
                    fila.Enqueue(u);
                }
            }
            if (veioPor[sorvedouro] == -1) break;

            // O gargalo e o menor residual do caminho, andando de tras para frente.
            var folga = long.MaxValue;
            for (var v = sorvedouro; v != fonte;)
            {
                var i = veioPor[v];
                folga = Math.Min(folga, rede.Residual(i));
                v = rede.Destino(Rede.Reversa(i));
            }
            for (var v = sorvedouro; v != fonte;)
            {
                var i = veioPor[v];
                rede.Empurrar(i, folga);
                v = rede.Destino(Rede.Reversa(i));
            }
            valor += folga;
            caminhos++;
        }
        return new Resultado(valor, Alcancaveis.CorteMinimo(rede, fonte), caminhos, visitas);
    }
}
