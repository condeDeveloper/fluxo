namespace Conde.Fluxo;

/// <summary>
/// Fluxo por escalonamento de capacidade, de Gabow (1985) e Ahuja-Orlin (1991).
///
/// A ideia e atacar os bits da capacidade do mais alto para o mais baixo. Comeca
/// so com as arestas de residual pelo menos D, onde D e a maior potencia de dois
/// que cabe na maior capacidade, satura o que da, e divide D por dois.
///
/// O ganho vem de um argumento de contagem. Quando a fase de D termina, nenhum
/// caminho tem residual maior ou igual a D, entao o corte correspondente tem
/// capacidade de sobra no maximo E*D, e portanto falta no maximo E*D para o
/// maximo. Na fase seguinte cada caminho carrega pelo menos D/2, entao sao no
/// maximo 2E caminhos por fase. Com log(C) fases, da O(E^2 * log C).
///
/// Isso coloca a dependencia da capacidade no LOGARITMO dela em vez de no valor,
/// que e exatamente onde o Ford-Fulkerson ingenuo quebra: na rede adversaria de
/// capacidade C, ele faz 2C caminhos e este faz uns poucos por bit.
/// </summary>
public static class Escala
{
    public static Resultado Resolver(Rede rede, int fonte, int sorvedouro)
    {
        ArgumentNullException.ThrowIfNull(rede);
        if (fonte == sorvedouro) throw new ArgumentException("fonte e sorvedouro iguais");
        rede.ZerarFluxo();

        long maior = 0;
        for (var i = 0; i < rede.Arestas; i++) maior = Math.Max(maior, rede.Capacidade(i));
        long valor = 0, caminhos = 0, visitas = 0;
        if (maior == 0) return new Resultado(0, Alcancaveis.CorteMinimo(rede, fonte), 0, 0);

        long limiar = 1;
        while (limiar <= maior / 2) limiar <<= 1;

        for (; limiar >= 1; limiar >>= 1)
        {
            while (true)
            {
                var visto = new bool[rede.Vertices];
                var folga = Procurar(rede, fonte, sorvedouro, long.MaxValue, limiar, visto, ref visitas);
                if (folga == 0) break;
                valor += folga;
                caminhos++;
            }
        }
        return new Resultado(valor, Alcancaveis.CorteMinimo(rede, fonte), caminhos, visitas);
    }

    /// <summary>Busca em profundidade que so enxerga arestas com residual a partir do limiar.</summary>
    private static long Procurar(Rede rede, int v, int sorvedouro, long limite, long limiar, bool[] visto, ref long visitas)
    {
        if (v == sorvedouro) return limite;
        visto[v] = true;
        foreach (var i in rede.Saindo(v))
        {
            visitas++;
            var r = rede.Residual(i);
            if (r < limiar) continue;
            var u = rede.Destino(i);
            if (visto[u]) continue;
            var passou = Procurar(rede, u, sorvedouro, Math.Min(limite, r), limiar, visto, ref visitas);
            if (passou <= 0) continue;
            rede.Empurrar(i, passou);
            return passou;
        }
        return 0;
    }
}
