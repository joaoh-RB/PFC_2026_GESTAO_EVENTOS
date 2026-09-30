# Diagrama BPMN - Projeto Symplosio

Este arquivo descreve, em formato textual, um fluxo BPMN inicial para o projeto **Symplosio** (gestão de eventos).

## Processo: Criar e Publicar Evento

### Participantes (Pools/Lanes)
- **Organizador**
- **Plataforma Symplosio**
- **Participante**

### Fluxo principal
1. **[Organizador]** Inicia cadastro de evento.
2. **[Plataforma]** Valida dados obrigatórios.
3. **Gateway (XOR)** Dados válidos?
   - **Não** → Exibe erros e retorna ao cadastro.
   - **Sim** → Salva evento como rascunho.
4. **[Organizador]** Configura lotes/ingressos.
5. **[Plataforma]** Publica evento.
6. **[Participante]** Consulta lista de eventos.
7. **[Participante]** Seleciona evento e inicia inscrição.
8. **[Plataforma]** Processa inscrição/pagamento.
9. **Gateway (XOR)** Pagamento aprovado?
   - **Não** → Notifica falha e permite nova tentativa.
   - **Sim** → Confirma inscrição.
10. **[Plataforma]** Envia comprovante por e-mail.
11. **Fim do processo**.

## Processo: Check-in no Evento

### Fluxo principal
1. **[Participante]** Apresenta ingresso/QR Code.
2. **[Staff/Organizador]** Escaneia código.
3. **[Plataforma]** Valida ingresso.
4. **Gateway (XOR)** Ingresso válido e não utilizado?
   - **Não** → Bloqueia entrada e registra tentativa.
   - **Sim** → Registra check-in e libera entrada.
5. **Fim do processo**.

## Próximos passos
- Converter este fluxo para notação BPMN 2.0 em ferramenta visual (Bizagi, Camunda Modeler, Draw.io).
- Versionar arquivo `.bpmn` após validação com equipe.
