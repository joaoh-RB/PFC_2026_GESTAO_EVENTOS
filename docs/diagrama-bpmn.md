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
4. **[Organizador]** Configura lotes/inscrições.
5. **[Plataforma]** Publica evento.
6. **[Participante]** Consulta lista de eventos.
7. **[Participante]** Seleciona evento e realiza inscrição.
8. **[Plataforma]** Confirma inscrição.
9. **[Plataforma]** Envia comprovante de inscrição por e-mail.
10. **Fim do processo**.

## Processo: Check-in no Evento

### Fluxo principal
1. **[Participante]** Apresenta ingresso/QR Code.
2. **[Staff/Organizador]** Escaneia código.
3. **[Plataforma]** Valida inscrição.
4. **Gateway (XOR)** Inscrição válida e não utilizada?
   - **Não** → Bloqueia entrada e registra tentativa.
   - **Sim** → Registra check-in e libera entrada.
5. **Fim do processo**.

## Arquivo BPMN 2.0 XML

O diagrama também está disponível em formato BPMN 2.0 XML, pronto para ser aberto no Camunda Modeler ou Bizagi:
[`docs/diagrama-bpmn.bpmn`](./diagrama-bpmn.bpmn)

## Próximos passos
- Revisar o fluxo com a equipe.
- Ajustar lanes/pools conforme papéis reais do sistema.
- Versionar novas alterações no arquivo `.bpmn` conforme evolução do projeto.
