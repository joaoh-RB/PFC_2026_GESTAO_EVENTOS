-- =============================================================================
-- Symplosio — reset de dados de teste + seed demo (Supabase / PostgreSQL)
-- =============================================================================
-- O que este script faz:
--   1) Apaga alunos, eventos, cursos, instituições, usuários, presenças,
--      feedbacks, grupos, documentos, aceites de termos e audit logs.
--   2) NÃO apaga Termos de Uso nem Política de Privacidade (tabela LegalDocuments).
--   3) Recria um cenário completo para demonstração/testes.
--
-- Como rodar no Supabase:
--   SQL Editor → New query → colar este arquivo → Run
--
-- Senha de TODOS os usuários criados: Senha@123
-- (hash BCrypt.Net EnhancedHashPassword SHA384, workFactor 12)
--
-- Contas úteis após o seed:
--   admin@symplosio.demo              Administrador (aprovado)
--   secretaria@fatec.demo             Secretaria (aprovada)
--   professor@fatec.demo              Professor (aprovado)
--   aluno.aprovado1@fatec.demo        Aluno aprovado
--   aluno.aprovado2@fatec.demo        Aluno aprovado
--   aluno.aprovado3@fatec.demo        Aluno aprovado
--   aluno.pendente1@fatec.demo        Aluno pendente de aprovação
--   aluno.pendente2@fatec.demo        Aluno pendente de aprovação
--   aluno.reprovado@fatec.demo        Aluno reprovado
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1) Limpeza (mantém LegalDocuments intacta)
-- ---------------------------------------------------------------------------
TRUNCATE TABLE
  "DocumentApprovalHistories",
  "Documents",
  "GroupMembers",
  "Groups",
  "EventFeedbacks",
  "EventStudents",
  "EventAllowedCourses",
  "EventAddedToCalendars",
  "Events",
  "TeacherCourses",
  "TermAcceptances",
  audit_logs,
  "Users",
  "Courses",
  "Institutions"
RESTART IDENTITY;

-- ---------------------------------------------------------------------------
-- 2) IDs fixos do seed
-- ---------------------------------------------------------------------------
-- Instituição / cursos
--   institution: 7d10ac30-7232-46a8-863d-3894c4a9c586
--   course ADS:  fa52caa8-2972-4881-b7b9-ff353e155b3d
--   course GPI:  b17fab1d-1044-4b29-8486-997f557c36da
--
-- Usuários
--   admin:       f198c87e-c951-436d-9065-133238888416
--   secretaria:  0c44704a-6b18-4cd8-97f2-d5b21aac3e39
--   professor:   d4e68b24-0d30-4aa8-a799-fea06e140105
--   aluno1:      abc41df7-9ab1-4406-96df-25c545050984  (aprovado)
--   aluno2:      9da518f3-5458-4713-b0f1-f6423f761a5f  (aprovado)
--   aluno3:      f153156d-c8e5-44ef-84fc-d53fca1c9ae3  (aprovado)
--   pendente1:   da9dd526-4ca3-4df2-95a2-c351cea6f635
--   pendente2:   ede4271e-577e-48fe-9a31-1b31a0406b76
--   reprovado:   860f7728-702c-43cb-9dbd-76099ce0c8dd
--
-- Eventos
--   passado1:    f9665c57-c7c5-4168-a153-3bce5edabd76
--   passado2:    b980e35a-68a8-45c9-ad73-77a48e51a811
--   futuro1:     24074938-c12c-45ea-ada1-cba8d9f636af
--   futuro2:     9b55626e-88e6-40c4-93f7-e4a70dd140dc
--   solicitado:  e6faf78a-811a-4101-8420-03803a7960ec

-- Senha: Senha@123
DO $$
DECLARE
  v_password_hash text := '$2a$12$M/ZYfbCmCQQdFhHn0n2Mt.TPJTPHRctQVi61xqMv69Glyb0HYQBS.';
  v_now timestamptz := NOW() AT TIME ZONE 'utc';

  v_institution_id uuid := '7d10ac30-7232-46a8-863d-3894c4a9c586';
  v_course_ads_id  uuid := 'fa52caa8-2972-4881-b7b9-ff353e155b3d';
  v_course_gpi_id  uuid := 'b17fab1d-1044-4b29-8486-997f557c36da';

  v_admin_id       uuid := 'f198c87e-c951-436d-9065-133238888416';
  v_secretaria_id  uuid := '0c44704a-6b18-4cd8-97f2-d5b21aac3e39';
  v_professor_id   uuid := 'd4e68b24-0d30-4aa8-a799-fea06e140105';
  v_aluno1_id      uuid := 'abc41df7-9ab1-4406-96df-25c545050984';
  v_aluno2_id      uuid := '9da518f3-5458-4713-b0f1-f6423f761a5f';
  v_aluno3_id      uuid := 'f153156d-c8e5-44ef-84fc-d53fca1c9ae3';
  v_pendente1_id   uuid := 'da9dd526-4ca3-4df2-95a2-c351cea6f635';
  v_pendente2_id   uuid := 'ede4271e-577e-48fe-9a31-1b31a0406b76';
  v_reprovado_id   uuid := '860f7728-702c-43cb-9dbd-76099ce0c8dd';

  v_event_past1_id     uuid := 'f9665c57-c7c5-4168-a153-3bce5edabd76';
  v_event_past2_id     uuid := 'b980e35a-68a8-45c9-ad73-77a48e51a811';
  v_event_future1_id   uuid := '24074938-c12c-45ea-ada1-cba8d9f636af';
  v_event_future2_id   uuid := '9b55626e-88e6-40c4-93f7-e4a70dd140dc';
  v_event_requested_id uuid := 'e6faf78a-811a-4101-8420-03803a7960ec';

  -- Enums (conforme Domain)
  -- UserRole: Aluno=1, Professor=2, Administrador=3, Secretaria=4
  -- UserApprovalStatus: Pendente=1, Aprovado=2, Reprovado=3
  -- EventType: Palestra=0, Simpósio=1, Congresso=2
  -- EventStatus: Agendado=1, Reprovado=2, Solicitado=3
  -- RegistrationStatus: Confirmed=1, Canceled=2, Attended=3
  -- AttendanceMethod: Code=1, Manual=2
BEGIN

  -- -------------------------------------------------------------------------
  -- Instituição e cursos
  -- -------------------------------------------------------------------------
  INSERT INTO "Institutions" ("Id", "Name", "Cnpj", "Address", "Phone", "IsActive", "CreatedAt")
  VALUES (
    v_institution_id,
    'Fatec Demo Symplosio',
    '11222333000181',
    'Av. Demo, 1000 - São Paulo/SP',
    '11999990000',
    true,
    v_now
  );

  INSERT INTO "Courses" ("Id", "Name", "InstitutionId", "IsActive", "CreatedAt")
  VALUES
    (v_course_ads_id, 'Análise e Desenvolvimento de Sistemas', v_institution_id, true, v_now),
    (v_course_gpi_id, 'Gestão da Produção Industrial', v_institution_id, true, v_now);

  -- -------------------------------------------------------------------------
  -- Usuários (staff + alunos)
  -- -------------------------------------------------------------------------
  INSERT INTO "Users" (
    "Id", "Name", "Email", "PasswordHash", "Role", "IsActive",
    "IsPasswordChangeRequired", "ApprovalStatus", "ApprovedByUserId", "ApprovedDate",
    "TwoFactorEnabled", "InstitutionId", "CreatedAt", "UpdatedAt",
    "CourseId", "UniqueIdentifier"
  )
  VALUES
    -- Admin (sem instituição)
    (
      v_admin_id, 'Admin Symplosio', 'admin@symplosio.demo', v_password_hash,
      3, true, false, 2, NULL, v_now, false, NULL, v_now, NULL, NULL, NULL
    ),
    -- Secretaria
    (
      v_secretaria_id, 'Secretaria Fatec Demo', 'secretaria@fatec.demo', v_password_hash,
      4, true, false, 2, v_admin_id, v_now, false, v_institution_id, v_now, NULL, NULL, NULL
    ),
    -- Professor
    (
      v_professor_id, 'Prof. Ana Oliveira', 'professor@fatec.demo', v_password_hash,
      2, true, false, 2, v_secretaria_id, v_now, false, v_institution_id, v_now, NULL, NULL, NULL
    ),
    -- Alunos aprovados
    (
      v_aluno1_id, 'João Aprovado Silva', 'aluno.aprovado1@fatec.demo', v_password_hash,
      1, true, false, 2, v_secretaria_id, v_now - interval '20 days', false,
      v_institution_id, v_now - interval '25 days', NULL, v_course_ads_id, 'RA2026001'
    ),
    (
      v_aluno2_id, 'Maria Aprovada Souza', 'aluno.aprovado2@fatec.demo', v_password_hash,
      1, true, false, 2, v_professor_id, v_now - interval '18 days', false,
      v_institution_id, v_now - interval '22 days', NULL, v_course_ads_id, 'RA2026002'
    ),
    (
      v_aluno3_id, 'Pedro Aprovado Lima', 'aluno.aprovado3@fatec.demo', v_password_hash,
      1, true, false, 2, v_secretaria_id, v_now - interval '15 days', false,
      v_institution_id, v_now - interval '19 days', NULL, v_course_gpi_id, 'RA2026003'
    ),
    -- Alunos pendentes
    (
      v_pendente1_id, 'Carla Pendente Rocha', 'aluno.pendente1@fatec.demo', v_password_hash,
      1, true, false, 1, NULL, NULL, false,
      v_institution_id, v_now - interval '2 days', NULL, v_course_ads_id, 'RA2026101'
    ),
    (
      v_pendente2_id, 'Bruno Pendente Alves', 'aluno.pendente2@fatec.demo', v_password_hash,
      1, true, false, 1, NULL, NULL, false,
      v_institution_id, v_now - interval '1 day', NULL, v_course_gpi_id, 'RA2026102'
    ),
    -- Aluno reprovado
    (
      v_reprovado_id, 'Lucas Reprovado Dias', 'aluno.reprovado@fatec.demo', v_password_hash,
      1, true, false, 3, v_secretaria_id, v_now - interval '5 days', false,
      v_institution_id, v_now - interval '10 days', NULL, v_course_ads_id, 'RA2026901'
    );

  INSERT INTO "TeacherCourses" ("CourseId", "TeacherId")
  VALUES
    (v_course_ads_id, v_professor_id),
    (v_course_gpi_id, v_professor_id);

  -- -------------------------------------------------------------------------
  -- Eventos (passados, futuros e solicitado)
  -- -------------------------------------------------------------------------
  INSERT INTO "Events" (
    "Id", "Name", "Description", "InstitutionId", "StartDate", "EndDate",
    "Capacity", "IsActive", "AllowDocuments", "MaxGroupSize", "SubmissionDeadline",
    "EventType", "AttendanceCode", "EventStatus", "CreatedByUserId", "GoogleCalendarEventId"
  )
  VALUES
    -- Passado 1: palestra com presença e feedback
    (
      v_event_past1_id,
      'Palestra: Carreira em Software',
      'Evento passado para demo de presença e feedback.',
      v_institution_id,
      v_now - interval '14 days',
      v_now - interval '14 days' + interval '2 hours',
      80, true, false, NULL, NULL,
      0, 'PASS01', 1, v_professor_id, NULL
    ),
    -- Passado 2: simpósio com documentos
    (
      v_event_past2_id,
      'Simpósio de Inovação 2026',
      'Evento passado com grupos e envio de trabalhos.',
      v_institution_id,
      v_now - interval '7 days',
      v_now - interval '7 days' + interval '4 hours',
      60, true, true, 4, v_now - interval '8 days',
      1, 'PASS02', 1, v_professor_id, NULL
    ),
    -- Futuro 1
    (
      v_event_future1_id,
      'Congresso Acadêmico Symplosio',
      'Evento futuro aberto para inscrição.',
      v_institution_id,
      v_now + interval '10 days',
      v_now + interval '10 days' + interval '6 hours',
      120, true, true, 5, v_now + interval '8 days',
      2, 'FUTU01', 1, v_secretaria_id, NULL
    ),
    -- Futuro 2
    (
      v_event_future2_id,
      'Palestra: LGPD na Prática',
      'Evento futuro focado em privacidade e compliance.',
      v_institution_id,
      v_now + interval '21 days',
      v_now + interval '21 days' + interval '2 hours',
      50, true, false, NULL, NULL,
      0, 'FUTU02', 1, v_professor_id, NULL
    ),
    -- Solicitado por aluno (pendente de aprovação do evento)
    (
      v_event_requested_id,
      'Meetup de Desenvolvimento Web',
      'Solicitação de evento criada por aluno aprovado.',
      v_institution_id,
      v_now + interval '30 days',
      v_now + interval '30 days' + interval '3 hours',
      40, true, false, NULL, NULL,
      0, NULL, 3, v_aluno1_id, NULL
    );

  INSERT INTO "EventAllowedCourses" ("EventId", "CourseId", "CreatedAt", "IsActive")
  VALUES
    (v_event_past1_id, v_course_ads_id, v_now - interval '20 days', true),
    (v_event_past1_id, v_course_gpi_id, v_now - interval '20 days', true),
    (v_event_past2_id, v_course_ads_id, v_now - interval '15 days', true),
    (v_event_future1_id, v_course_ads_id, v_now - interval '3 days', true),
    (v_event_future1_id, v_course_gpi_id, v_now - interval '3 days', true),
    (v_event_future2_id, v_course_ads_id, v_now - interval '2 days', true),
    (v_event_requested_id, v_course_ads_id, v_now - interval '1 day', true);

  -- -------------------------------------------------------------------------
  -- Inscrições + presenças
  -- Status: Confirmed=1, Canceled=2, Attended=3
  -- AttendanceMethod: Code=1, Manual=2
  -- -------------------------------------------------------------------------
  INSERT INTO "EventStudents" (
    "EventId", "StudentId", "Status", "RegisteredAt", "CanceledAt",
    "AttendedAt", "AttendanceMethod", "IsActive"
  )
  VALUES
    -- Passado 1: 2 presentes (código/manual) + 1 inscrito sem presença
    (
      v_event_past1_id, v_aluno1_id, 3,
      v_now - interval '20 days', NULL,
      v_now - interval '14 days' + interval '30 minutes', 1, true
    ),
    (
      v_event_past1_id, v_aluno2_id, 3,
      v_now - interval '19 days', NULL,
      v_now - interval '14 days' + interval '40 minutes', 2, true
    ),
    (
      v_event_past1_id, v_aluno3_id, 1,
      v_now - interval '18 days', NULL, NULL, NULL, true
    ),
    -- Passado 2
    (
      v_event_past2_id, v_aluno1_id, 3,
      v_now - interval '12 days', NULL,
      v_now - interval '7 days' + interval '1 hour', 1, true
    ),
    (
      v_event_past2_id, v_aluno2_id, 3,
      v_now - interval '11 days', NULL,
      v_now - interval '7 days' + interval '1 hour', 1, true
    ),
    -- Futuro 1: inscritos confirmados
    (
      v_event_future1_id, v_aluno1_id, 1,
      v_now - interval '2 days', NULL, NULL, NULL, true
    ),
    (
      v_event_future1_id, v_aluno2_id, 1,
      v_now - interval '2 days', NULL, NULL, NULL, true
    ),
    (
      v_event_future1_id, v_aluno3_id, 1,
      v_now - interval '1 day', NULL, NULL, NULL, true
    ),
    -- Futuro 2: um confirmado e um cancelado
    (
      v_event_future2_id, v_aluno1_id, 1,
      v_now - interval '1 day', NULL, NULL, NULL, true
    ),
    (
      v_event_future2_id, v_aluno3_id, 2,
      v_now - interval '1 day', v_now - interval '12 hours', NULL, NULL, true
    );

  -- -------------------------------------------------------------------------
  -- Feedbacks (apenas eventos passados com presença)
  -- -------------------------------------------------------------------------
  INSERT INTO "EventFeedbacks" ("Id", "EventId", "StudentId", "FeedbackText", "Rating", "CreatedAt")
  VALUES
    (
      '3475d010-d407-4ffe-92ce-5bfed884a52d',
      v_event_past1_id, v_aluno1_id,
      'Excelente conteúdo e ótima organização.',
      9, v_now - interval '13 days'
    ),
    (
      '15eee0ff-9dbb-44d3-9a9d-126828372a54',
      v_event_past1_id, v_aluno2_id,
      'Gostei bastante, poderia ter mais exemplos práticos.',
      8, v_now - interval '13 days'
    ),
    (
      '9df52890-2231-478f-87f9-2633fb378145',
      v_event_past2_id, v_aluno1_id,
      'Simpósio muito rico, networking excelente.',
      10, v_now - interval '6 days'
    ),
    (
      '1806718d-2de6-43c5-ada9-6f7da9e74ded',
      v_event_past2_id, v_aluno2_id,
      'Boa programação, mas a sala estava cheia.',
      7, v_now - interval '6 days'
    );

  -- -------------------------------------------------------------------------
  -- Aceite dos termos atuais (LegalDocuments existentes) para usuários não-admin
  -- -------------------------------------------------------------------------
  INSERT INTO "TermAcceptances" (
    "Id", "UserId", "LegalDocumentId", "Version", "AcceptedAtUtc", "IpAddress", "CreatedAt"
  )
  SELECT
    gen_random_uuid(),
    u."Id",
    ld."Id",
    ld."Version",
    v_now,
    '127.0.0.1',
    v_now
  FROM "Users" u
  CROSS JOIN "LegalDocuments" ld
  WHERE ld."IsActive" = true
    AND u."Role" <> 3; -- Administrador é isento no front

END $$;

COMMIT;

-- =============================================================================
-- Conferência rápida (opcional)
-- =============================================================================
-- SELECT "Role", "ApprovalStatus", COUNT(*) FROM "Users" GROUP BY 1, 2 ORDER BY 1, 2;
-- SELECT "Name", "EventStatus", "StartDate" FROM "Events" ORDER BY "StartDate";
-- SELECT COUNT(*) AS feedbacks FROM "EventFeedbacks";
-- SELECT COUNT(*) AS presencas FROM "EventStudents" WHERE "Status" = 3;
-- SELECT COUNT(*) AS legal_docs FROM "LegalDocuments"; -- deve ser o mesmo de antes
