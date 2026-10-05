## 1. Identificação
- **Aluno(s):** Gabriel Antonio Vieira Cordeiro - 11231504427
João Henrique Rodrigues Batista - []
Pedro Henrique Santos Andrade - []
- **Projeto (PFC):** Symplosio
- **Branch:** feat/testes-automatizados

## 2. Resumo da entrega [PREENCHER]
[2 a 4 linhas: o que foi testado e qual parte do sistema passou a ter cobertura.]

## 3. Cenários de testes unitários implementados [PREENCHER]
| # | Classe testada | Método / regra | Cenário | Tipo | Arquivo de teste | Método de teste |
|---|----------------|----------------|---------|------|------------------|-----------------|
| 1 | PedidoService | criar() | Estoque suficiente | Feliz | PedidoServiceTest | deveCriarPedido |
| 2 | PedidoService | criar() | Sem estoque | Violação | PedidoServiceTest | deveLancarExcecao |

Tipo: Feliz | Violação | Limite
**Total de cenários unitários:** [10]

## 4. Cenários de testes de integração implementados [PREENCHER]
| # | Camadas envolvidas | Cenário | Arquivo de teste | Método de teste | Recurso usado |
|---|--------------------|---------|------------------|-----------------|---------------|
| 1 | Controller+BD | POST /produtos retorna 201 | ProdutoControllerIT | deveRetornar201 | MockMvc+H2 |

**Total de cenários de integração:** [4]

## 5. Arquivos de teste criados ou alterados [PREENCHER]
| Arquivo (caminho completo) | Criado / Alterado | Qtd. de testes |
|----------------------------|-------------------|----------------|
| src/test/java/.../PedidoServiceTest | Criado | [N] |

**Total de arquivos de teste:** [N]  |  **Total de testes:** [N]

## 6. Como executar os testes
```
dotnet test Gestao_Eventos.slnx
```

## 7. Evidências
- **Resultado da execução:** 
```
[xUnit.net 00:00:01.14]   Finished:    API_Gestao_Eventos.Tests
  API_Gestao_Eventos.Tests teste net10.0 êxito (1,6s)

Resumo do teste: total: 16; falhou: 0; bem-sucedido: 16; ignorado: 0; duração: 1,6s
Construir êxito em 3,0s
```
- **Link do CI (se houver):** não se aplica

## 8. Decisões e dificuldades
- **O que foi mockado e por quê:** [texto] [PREENCHER]
- **Bugs encontrados pelos testes (se houver):** Nenhum.
- **Dificuldades:** [texto] [PREENCHER]

## 9. Checklist de entrega
- [X] Todos os testes passam localmente com o comando da seção 6
- [FAZER] Cada cenário listado nas seções 3 e 4 existe no código
- [FAZER] Cada arquivo de teste alterado ou criado está listado na seção 5
- [X] Mínimos do exercício atendidos (10 unitários em 3 classes; 4 de integração)
- [X] Nenhum teste com @Disabled, sem asserção ou com Thread.sleep
- [FAZER] Professor adicionado como reviewer
