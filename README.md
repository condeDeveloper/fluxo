# fluxo

Fluxo máximo e corte mínimo do zero em C# e .NET 8: Ford-Fulkerson, Edmonds-Karp,
Dinic e escalonamento de capacidade. O juiz é a enumeração de **todos** os cortes.

```
$ dotnet medidor.dll grade

  grade      vertices   fluxo   edmonds-karp        dinic       escala   | ms (dinic)
  10x10           102      52          6.529        3.537        2.381   |      0
  20x20           402      80         77.204       29.578       14.017   |      6
  40x40         1.602     145        697.501      248.681      132.092   |     16
  60x60         3.602     219      2.661.983      876.698      531.622   |     29
```

## O juiz não pode ser outro algoritmo de fluxo

O que se quer testar aqui é justamente o teorema do fluxo máximo e corte mínimo.
Se o juiz fosse outro algoritmo de fluxo, os dois poderiam errar junto e o teste
não diria nada.

Então o juiz enumera os **2^(n−2)** cortes possíveis — fonte e sorvedouro têm lado
fixo, cada um dos outros vértices escolhe — e devolve o menor, usando só a
definição de capacidade de corte. Com 8 vértices são 64 cortes por rede, e o teste
roda **3.000 redes sorteadas**: 192 mil cortes enumerados contra quatro
algoritmos, mais o corte que cada um devolve, que precisa separar de verdade e ter
capacidade igual ao fluxo.

Fora isso, três invariantes conferidas depois de **cada** execução, vértice a
vértice e aresta a aresta: conservação (o que entra sai, menos nas pontas),
capacidade (ninguém passa do limite) e simetria (o fluxo da reversa é o negativo
do da direta). Um empurrão mal feito pode dar o **valor certo** e deixar o fluxo
inválido, e só a conferência vértice a vértice pega isso.

## O que a medição me corrigiu, e é o melhor achado daqui

Eu escrevi, no código e no teste, que a rede de livro de quatro vértices faria a
busca em **profundidade** gastar 2C caminhos:

```
fonte → a (capacidade C)    a → sorvedouro (C)
fonte → b (C)               b → sorvedouro (C)
a → b (capacidade 1)
```

**Não faz.** Medi as 120 ordens de inserção possíveis das cinco ligações:

```
$ dotnet medidor.dll ordens

  caminhos   quantas ordens
         2               90
         4               30
```

A pior ordem gasta **4** caminhos, não 2C. Preferir a aresta estreita também dá 4.
O motivo fica óbvio depois de visto: a busca empurra o gargalo do caminho inteiro,
e assim que a aresta do meio satura ela encontra a saída direta e leva o resto de
uma vez.

O limite clássico, O(E × valor do fluxo), vale para a **pior escolha possível** de
caminho, e a definição do método deixa essa escolha em aberto. Isso não é a mesma
coisa que uma implementação ser lenta, e eu tinha juntado as duas.

A execução de 2C existe e é legal, e o teste a **exibe** passo a passo, alternando
`0-1-2-3` e `0-2-1-3`: cada passo carrega exatamente uma unidade, o primeiro satura
a aresta do meio e o segundo a desatura pela reversa. São 2C passos para um fluxo
que o Edmonds-Karp resolve em dois. O limite é alcançável, só não por acidente.

```
$ dotnet medidor.dll capacidade

            C   ford-fulkerson   edmonds-karp   dinic   escala   adversario exibido
           10                2              2       1        2                   20
        1.000                2              2       1        2                2.000
    1.000.000                2              2       1        2            2.000.000
```

## O segundo erro: capacidade infinita onde devia ser um

A versão de Menger por vértice divide cada vértice em entrada e saída ligadas por
capacidade 1, e eu dei capacidade infinita às arestas entre vértices. Uma aresta
**direta** da fonte ao sorvedouro não passa por vértice nenhum do meio, então nada
a limitava: o teste acusou 2.305.843.009.213.693.951 caminhos disjuntos num grafo
de sete vértices.

Foi a invariante que pegou, e ela é de uma linha: disjunto por vértice é uma
restrição mais forte que por aresta, então **nunca** pode dar mais.

## Por que o escalonamento ganha do Dinic aqui

Na tabela do topo, com grade de 60×60, o escalonamento visita 531 mil arestas
contra 876 mil do Dinic. Não é o que a notação O sugere, e o motivo é a carga: as
capacidades da grade vão até 20, então são cinco fases de bit, e em cada uma só
passam caminhos gordos. O Dinic gasta fases inteiras levando caminhos de uma
unidade no fim.

O Edmonds-Karp visita 2,66 milhões — três vezes o Dinic — e a razão é estrutural:
ele faz uma busca em largura completa **por caminho**, e o Dinic faz uma por
**fase**, aproveitando a mesma rotulação para todos os caminhos que couberem.

## Fluxo responde perguntas que não parecem ser sobre fluxo

- **Menger, 1927** (trinta anos antes do algoritmo): o número de caminhos
  disjuntos entre dois vértices é o número mínimo de arestas a remover para
  separá-los. Com capacidade 1 em tudo, isso **é** o teorema do corte mínimo. O
  juiz aqui também não é fluxo: é a enumeração de todos os subconjuntos de
  arestas, procurando o menor que desconecta.
- **König, 1931**: num grafo bipartido, o maior emparelhamento tem o tamanho da
  menor cobertura por vértices. Os dois saem do mesmo lugar, e os dois são
  conferidos contra força bruta sobre subconjuntos.
- **Hall, 1935**: quando o emparelhamento completo falha, existe um subconjunto do
  lado esquerdo com menos vizinhos que elementos. O teste **procura** essa
  testemunha, e exige que ela exista exatamente quando o emparelhamento não fecha.
- **Picard, 1976**: selecionar projetos com dependências é corte mínimo. Monta-se
  `fonte → projeto` com o lucro, `máquina → sorvedouro` com o custo, e
  `projeto → máquina` com capacidade **infinita**. Um corte finito nunca atravessa
  uma aresta infinita, então a regra de dependência vira consequência da
  estrutura em vez de algo que alguém precisa impor. Conferido contra a enumeração
  dos 2⁸ subconjuntos em 2.000 instâncias.

## As peças

| arquivo | o que faz |
|---|---|
| `Rede` | arestas em par com a reversa; empurrar é XOR 1 |
| `Corte` | capacidade conta só o que atravessa de S para T |
| `ForcaBruta` | o juiz: todos os 2^(n−2) cortes |
| `Alcancaveis` | de onde sai o corte mínimo depois que o fluxo acaba |
| `FordFulkerson` | o método, com a busca em profundidade e o aumento por caminho dado |
| `EdmondsKarp` | sempre o caminho mais curto |
| `Dinic` | fases por nível, com o ponteiro que não volta |
| `Escala` | ataca os bits da capacidade, do mais alto para o mais baixo |
| `Menger` | caminhos disjuntos por aresta e por vértice |
| `Projeto` | seleção de projetos por corte mínimo |
| `Geradores` | adversária, grade, bipartida, unitária |

São 33 testes.

## Como rodar

```
dotnet test testes/Fluxo.Testes/Fluxo.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- capacidade
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- ordens
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- grade
```

## Licença

MIT.
