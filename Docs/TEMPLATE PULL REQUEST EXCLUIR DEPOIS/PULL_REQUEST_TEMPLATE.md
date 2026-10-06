## 1. Identificação
- **Aluno(s):** Gabriel Antonio Vieira Cordeiro - 11231504427
João Henrique Rodrigues Batista - []
Pedro Henrique Santos Andrade - 11231103572
- **Projeto (PFC):** Symplosio
- **Branch:** feat/testes-automatizados

## 2. Resumo da entrega [PREENCHER]
[2 a 4 linhas: o que foi testado e qual parte do sistema passou a ter cobertura.]

## 3. Cenários de testes unitários implementados
| # | Classe testada | Método / regra | Cenário | Tipo | Arquivo de teste | Método de teste |
|---|----------------|----------------|---------|------|------------------|-----------------|
| 1 | CourseService | CreateCourseAsync() | Dados válidos cadastram o curso | Feliz | CourseServiceTests | CreateCourseAsync_QuandoDadosValidos_DeveCadastrarComSucessoEChamarAddAsync |
| 2 | CourseService | CreateCourseAsync() | Nome de curso já existente | Violação | CourseServiceTests | CreateCourseAsync_QuandoNomeJaExisteNaInstituicao_DeveLancarInvalidOperationExceptionENaoChamarAddAsync |
| 3 | CourseService | GetCoursesForSelectAsync() | Instituição vazia | Limite | CourseServiceTests | GetCoursesForSelectAsync_QuandoInstitutionIdForVazioOuNulo_DeveRetornarListaVaziaSemConsultarRepositorio |
| 4 | CreateCourseValidator | Validate() | Dados válidos passam | Feliz | CreateCourseValidatorTests | Validate_QuandoDadosForemValidos_DevePassarNaValidacao |
| 5 | CreateCourseValidator | Validate() | Nome vazio | Violação | CreateCourseValidatorTests | Validate_QuandoNomeForVazio_DeveFalharComMensagemObrigatoria |
| 6 | CreateCourseValidator | Validate() | Nome com exatamente 150 caracteres | Limite | CreateCourseValidatorTests | Validate_QuandoNomeTiverExatamente150Caracteres_DevePassarNaValidacao |
| 7 | EventService | AddAsync() | Dados válidos cadastram o evento | Feliz | EventServiceTests | CreateEventAsync_QuandoDadosForemValidos_DeveCadastrarEventoComSucessoEChamarAddAsync |
| 8 | EventService | AddAsync() | Curso não pertence à instituição | Violação | EventServiceTests | CreateEventAsync_QuandoCursoNaoPertencerAInstituicao_DeveLancarInvalidOperationExceptionENaoChamarAddAsync |
| 9 | EventService | SetActiveAsync() | Inativar evento que já começou | Limite | EventServiceTests | SetActiveAsync_QuandoDataForPassadaEForDesativar_DeveLancarArgumentExceptionENaoChamarSetActiveAsyncEDeactivateEventAsync |
| 10 | CourseService | CreateCourseAsync() | Nome existente e nome novo | Violação | CourseServiceTests | CreateCourseAsync_QuandoNomesJaExistiremNaInstituicao_DeveLancarInvalidOperationExceptionENaoChamarAddAsync |

Tipo: Feliz | Violação | Limite
**Total de cenários unitários:** 10

## 4. Cenários de testes de integração implementados
| # | Camadas envolvidas | Cenário | Arquivo de teste | Método de teste | Recurso usado |
|---|--------------------|---------|------------------|-----------------|---------------|
| 1 | Controller+Service+BD | GET /api/institutions retorna 200 | ApiIntegrationTests | GetInstitutions_QuandoCaminhoFeliz_DeveRetornarStatus200ECorpoComInstituicoes | WebApplicationFactory+SQLite |
| 2 | Controller+Service+BD | GET /api/institutions/{id} inexistente retorna 404 | ApiIntegrationTests | GetInstitutionById_QuandoNaoEncontrado_DeveRetornarStatus404ECorpoDeErro | WebApplicationFactory+SQLite |
| 3 | Repository+BD | Salvar e recuperar instituição | PersistenceTests | InstitutionRepository_DeveSalvarERecuperarEConsultarCustomizado | SQLite em memória |
| 4 | Controller+Service+BD | POST retorna 201 e GET devolve o criado | ApiIntegrationTests | FluxoCompleto_CriarEConsultarInstituicao_DeveCruzarApiComServiceEBanco | WebApplicationFactory+SQLite |

**Total de cenários de integração:** 4

## 5. Arquivos de teste criados ou alterados
| Arquivo (caminho completo) | Criado / Alterado | Qtd. de testes |
|----------------------------|-------------------|----------------|
| api_projetos/tests/API_Gestao_Eventos.Tests/Services/CourseServiceTests.cs | Criado | 6 |
| api_projetos/tests/API_Gestao_Eventos.Tests/Services/EventServiceTests.cs | Criado | 3 |
| api_projetos/tests/API_Gestao_Eventos.Tests/Validators/CreateCourseValidatorTests.cs | Criado | 3 |
| api_projetos/tests/API_Gestao_Eventos.Tests/Integration/ApiIntegrationTests.cs | Criado | 3 |
| api_projetos/tests/API_Gestao_Eventos.Tests/Integration/PersistenceTests.cs | Criado | 1 |
| api_projetos/tests/API_Gestao_Eventos.Tests/Integration/CustomWebApplicationFactory.cs | Criado | 0 |

**Total de arquivos de teste:** 6  |  **Total de testes:** 16

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
