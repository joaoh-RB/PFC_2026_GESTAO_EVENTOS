using API_Gestao_Eventos.src.Domain;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using API_Gestao_Eventos.src.Infrastructure.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seed local de demonstração.
    /// Preserva LegalDocuments (versionamento atual) e o administrador do sistema.
    /// Usa <see cref="IHasher"/> (BCrypt enhanced + salt) para as senhas.
    /// Respeita regras: presença só para inscritos; feedback só com presença.
    /// </summary>
    public static class DemoDataSeeder
    {
        public const string DefaultAdminEmail = "contapfcgestaoeventos@gmail.com";
        public const string DefaultTestPassword = "SenhaAdmin.1";

        // Mesmas regras do AttendanceService.GenerateCode()
        private const int AttendanceCodeLength = 6;
        private const string AttendanceCodeChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        public static async Task RunIfEnabledAsync(IHost app, CancellationToken cancellationToken = default)
        {
            var env = app.Services.GetRequiredService<IHostEnvironment>();
            if (!env.IsDevelopment())
                return;

            using var scope = app.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var config = sp.GetRequiredService<IConfiguration>();

            if (!config.GetValue("Seed:DemoDataOnStartup", false))
                return;

            var db = sp.GetService<AppDbContext>();
            if (db is null)
                return;

            var hasher = sp.GetRequiredService<IHasher>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");
            var forceReseed = config.GetValue("Seed:ForceReseed", false);

            // Evita limpar o banco a cada hot-reload: só roda se forçado ou se ainda não há instituições.
            var hasInstitutions = await db.Institutions.AnyAsync(cancellationToken);
            if (hasInstitutions && !forceReseed)
            {
                logger.LogInformation(
                    "Demo seed ignorado (já existem instituições). Defina Seed:ForceReseed=true para apagar e recriar a massa de teste.");
                return;
            }

            var adminEmail = config["Seed:AdminEmail"] ?? DefaultAdminEmail;
            var password = config["Seed:TestPassword"] ?? DefaultTestPassword;

            await SeedAsync(db, hasher, adminEmail, password, logger, cancellationToken);
        }

        public static async Task SeedAsync(
            AppDbContext db,
            IHasher hasher,
            string adminEmail,
            string testPassword,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            var admin = await db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == adminEmail.ToLower(), cancellationToken);

            if (admin is null)
                throw new InvalidOperationException(
                    $"Administrador '{adminEmail}' não encontrado. O seed não recria esse usuário.");

            logger.LogInformation("Demo seed: limpando dados (preservando admin {Email} e LegalDocuments)...", admin.Email);
            await WipeBusinessDataAsync(db, admin.Id, cancellationToken);

            // Recarrega admin após limpeza de FKs
            admin = await db.Users.SingleAsync(u => u.Id == admin.Id, cancellationToken);
            admin.Role = UserRole.Administrador;
            admin.ApprovalStatus = UserApprovalStatus.Aprovado;
            admin.IsActive = true;
            admin.IsPasswordChangeRequired = false;
            admin.InstitutionId = null;
            admin.ApprovedByUserId = null;

            var now = DateTime.UtcNow;

            logger.LogInformation("Demo seed: criando instituições, cursos, usuários, eventos e vínculos...");

            var umc = NewInstitution("Universidade de Mogi das Cruzes (UMC)", "04547152000160",
                "Av. Dr. Cândido Xavier de Almeida e Souza, 200 - Centro Cívico, Mogi das Cruzes/SP", "1147970600", now.AddDays(-120));
            var ubc = NewInstitution("Universidade Braz Cubas (UBC)", "60746948000112",
                "Av. Francisco Rodrigues Filho, 1233 - Mogilar, Mogi das Cruzes/SP", "1147944000", now.AddDays(-110));
            var fatec = NewInstitution("Fatec Mogi das Cruzes", "62803960000147",
                "Rua Natal, 301 - Parque das Laranjeiras, Mogi das Cruzes/SP", "1147228900", now.AddDays(-100));
            var ung = NewInstitution("Universidade Guarulhos (UNG)", "50321372000198",
                "Praça Tereza Cristina, 01 - Centro, Guarulhos/SP", "1124241500", now.AddDays(-90));

            db.Institutions.AddRange(umc, ubc, fatec, ung);

            var umcCourses = AddCourses(db, umc, now, "Ciência da Computação", "Administração", "Engenharia de Software", "Psicologia");
            var ubcCourses = AddCourses(db, ubc, now, "Sistemas de Informação", "Direito", "Engenharia Civil", "Pedagogia");
            var fatecCourses = AddCourses(db, fatec, now, "Análise e Desenvolvimento de Sistemas", "Gestão da Produção Industrial", "Desenvolvimento de Software Multiplataforma", "Logística");
            var ungCourses = AddCourses(db, ung, now, "Ciência da Computação", "Enfermagem", "Farmácia", "Educação Física");

            var packs = new[]
            {
                new InstitutionPack(umc, umcCourses, "umc", "umc.edu.br",
                    "Mariana Figueiredo Costa", "Prof. Carlos Eduardo Barros", "Profa. Renata Moura Silveira"),
                new InstitutionPack(ubc, ubcCourses, "ubc", "ubc.edu.br",
                    "Ricardo Mendes Azevedo", "Profa. Juliana Pires Monteiro", "Prof. Henrique Tavares Lopes"),
                new InstitutionPack(fatec, fatecCourses, "fatec", "fatec.sp.gov.br",
                    "Patrícia Nogueira Ramos", "Prof. André Luiz Cardoso", "Profa. Sabrina Melo Castro"),
                new InstitutionPack(ung, ungCourses, "ung", "ung.br",
                    "Fernanda Almeida Prado", "Profa. Camila Ribeiro Santos", "Prof. Diego Nunes Albuquerque"),
            };

            var legalDocs = await db.LegalDocuments.AsNoTracking()
                .Where(d => d.IsActive)
                .ToListAsync(cancellationToken);

            var usedAttendanceCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pack in packs)
            {
                // Hash por usuário: cada chamada do IHasher gera salt BCrypt distinto.
                SeedInstitutionScenario(db, pack, admin, hasher, testPassword, now, legalDocs, usedAttendanceCodes);
            }

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Demo seed concluído. Senha dos usuários de teste: {Password}. Admin preservado: {AdminEmail}.",
                testPassword,
                admin.Email);
        }

        private static async Task WipeBusinessDataAsync(AppDbContext db, Guid adminId, CancellationToken ct)
        {
            await db.Set<DocumentApprovalHistory>().ExecuteDeleteAsync(ct);
            await db.Set<Document>().ExecuteDeleteAsync(ct);
            await db.Set<GroupMember>().ExecuteDeleteAsync(ct);
            await db.Set<Group>().ExecuteDeleteAsync(ct);
            await db.Set<EventFeedback>().ExecuteDeleteAsync(ct);
            await db.Set<EventStudent>().ExecuteDeleteAsync(ct);
            await db.Set<EventAllowedCourse>().ExecuteDeleteAsync(ct);
            await db.Set<EventAddedToCalendar>().ExecuteDeleteAsync(ct);
            await db.Set<Event>().ExecuteDeleteAsync(ct);
            await db.Database.ExecuteSqlRawAsync("""DELETE FROM "TeacherCourses";""", ct);
            await db.Set<TermAcceptance>().ExecuteDeleteAsync(ct);
            await db.Set<AuditLog>().ExecuteDeleteAsync(ct);

            await db.Users
                .Where(u => u.Id != adminId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.ApprovedByUserId, (Guid?)null)
                    .SetProperty(u => u.InstitutionId, (Guid?)null), ct);

            // CourseId só existe em Student (TPH); limpa via SQL para liberar DELETE de Courses.
            await db.Database.ExecuteSqlRawAsync(
                """UPDATE "Users" SET "CourseId" = NULL WHERE "Id" <> {0};""",
                adminId);

            await db.Users.Where(u => u.Id != adminId).ExecuteDeleteAsync(ct);
            await db.Courses.ExecuteDeleteAsync(ct);
            await db.Institutions.ExecuteDeleteAsync(ct);
        }

        private static void SeedInstitutionScenario(
            AppDbContext db,
            InstitutionPack pack,
            User admin,
            IHasher hasher,
            string testPassword,
            DateTime now,
            IReadOnlyList<LegalDocument> legalDocs,
            HashSet<string> usedAttendanceCodes)
        {
            var secretaria = new AcademicDepartment
            {
                Name = pack.SecretaryName,
                Email = $"secretaria.{pack.Slug}@teste.com",
                PasswordHash = hasher.HashPassword(testPassword),
                InstitutionId = pack.Institution.Id,
                ApprovalStatus = UserApprovalStatus.Aprovado,
                ApprovedByUserId = admin.Id,
                ApprovedDate = now.AddDays(-70),
                CreatedAt = now.AddDays(-70),
                IsActive = true
            };

            var professor1 = new Teacher
            {
                Name = pack.Teacher1Name,
                Email = $"professor.{pack.Slug}@teste.com",
                PasswordHash = hasher.HashPassword(testPassword),
                InstitutionId = pack.Institution.Id,
                ApprovalStatus = UserApprovalStatus.Aprovado,
                ApprovedByUserId = secretaria.Id,
                ApprovedDate = now.AddDays(-65),
                CreatedAt = now.AddDays(-65),
                IsActive = true
            };
            professor1.Courses = pack.Courses.Take(2).ToList();

            var professor2 = new Teacher
            {
                Name = pack.Teacher2Name,
                Email = $"professor2.{pack.Slug}@teste.com",
                PasswordHash = hasher.HashPassword(testPassword),
                InstitutionId = pack.Institution.Id,
                ApprovalStatus = UserApprovalStatus.Aprovado,
                ApprovedByUserId = secretaria.Id,
                ApprovedDate = now.AddDays(-64),
                CreatedAt = now.AddDays(-64),
                IsActive = true
            };
            professor2.Courses = pack.Courses.Skip(2).ToList();

            var teste = NewStudent(
                $"Aluno Teste {pack.Slug.ToUpperInvariant()}",
                $"aluno.{pack.Slug}@teste.com",
                hasher.HashPassword(testPassword),
                pack.Institution.Id,
                pack.Courses[0].Id,
                $"{pack.Slug.ToUpperInvariant()}-TESTE",
                UserApprovalStatus.Aprovado,
                secretaria.Id,
                now.AddDays(-40),
                now.AddDays(-45));

            db.Users.AddRange(secretaria, professor1, professor2, teste);

            var approved = new List<Student> { teste };
            var namePool = StudentNames.For(pack.Slug);
            var nameIndex = 0;

            for (var i = 0; i < pack.Courses.Count; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    var name = namePool[nameIndex++];
                    var student = NewStudent(
                        name,
                        EmailFromName(name, j + 1, pack.EmailDomain),
                        hasher.HashPassword(testPassword),
                        pack.Institution.Id,
                        pack.Courses[i].Id,
                        $"{pack.Slug.ToUpperInvariant()[..3]}2026{1000 + i * 10 + j + 1}",
                        UserApprovalStatus.Aprovado,
                        secretaria.Id,
                        now.AddDays(-30),
                        now.AddDays(-35));
                    approved.Add(student);
                    db.Users.Add(student);
                }
            }

            // Pendentes e reprovado (não entram em presença/feedback)
            for (var p = 0; p < 2; p++)
            {
                var name = namePool[nameIndex++];
                db.Users.Add(NewStudent(
                    name,
                    EmailFromName(name, $"pendente{p + 1}", pack.EmailDomain),
                    hasher.HashPassword(testPassword),
                    pack.Institution.Id,
                    pack.Courses[0].Id,
                    $"{pack.Slug.ToUpperInvariant()[..3]}2026{9001 + p}",
                    UserApprovalStatus.Pendente,
                    null,
                    null,
                    now.AddDays(-2 - p)));
            }

            var rejectedName = namePool[nameIndex++];
            db.Users.Add(NewStudent(
                rejectedName,
                EmailFromName(rejectedName, "reprovado", pack.EmailDomain),
                hasher.HashPassword(testPassword),
                pack.Institution.Id,
                pack.Courses[0].Id,
                $"{pack.Slug.ToUpperInvariant()[..3]}20269999",
                UserApprovalStatus.Reprovado,
                secretaria.Id,
                now.AddDays(-10),
                now.AddDays(-20)));

            AcceptTerms(db, legalDocs, secretaria, professor1, professor2);
            foreach (var s in approved)
                AcceptTerms(db, legalDocs, s);

            // Eventos passados: código aleatório único (mesma regra do AttendanceService).
            // Eventos futuros/solicitados: sem código (só é gerado após o início do evento).
            var pastDocs = NewEvent(
                pack.Institution.Id,
                $"Simulação acadêmica {pack.Institution.Name.Split('(')[0].Trim()}",
                "Evento passado com inscrição, presença, feedback, grupos e documentos.",
                now.AddDays(-10),
                now.AddDays(-10).AddHours(4),
                capacity: 80,
                allowDocuments: true,
                maxGroupSize: 4,
                submissionDeadline: now.AddDays(-12),
                EventType.Simpósio,
                CreateUniqueAttendanceCode(usedAttendanceCodes),
                EventStatus.Agendado,
                professor1.Id,
                pack.Courses);

            var pastSimple = NewEvent(
                pack.Institution.Id,
                $"Palestra aberta — {pack.Slug.ToUpperInvariant()}",
                "Evento passado sem envio de documentos.",
                now.AddDays(-18),
                now.AddDays(-18).AddHours(2),
                capacity: 60,
                allowDocuments: false,
                maxGroupSize: null,
                submissionDeadline: null,
                EventType.Palestra,
                CreateUniqueAttendanceCode(usedAttendanceCodes),
                EventStatus.Agendado,
                professor2.Id,
                pack.Courses);

            var future = NewEvent(
                pack.Institution.Id,
                $"Congresso futuro — {pack.Slug.ToUpperInvariant()}",
                "Evento futuro com vagas e inscrições confirmadas.",
                now.AddDays(14),
                now.AddDays(14).AddHours(6),
                capacity: 100,
                allowDocuments: true,
                maxGroupSize: 5,
                submissionDeadline: now.AddDays(12),
                EventType.Congresso,
                null,
                EventStatus.Agendado,
                secretaria.Id,
                pack.Courses);

            var requested = NewEvent(
                pack.Institution.Id,
                $"Meetup solicitado — {pack.Slug.ToUpperInvariant()}",
                "Solicitação de evento criada por aluno aprovado.",
                now.AddDays(28),
                now.AddDays(28).AddHours(3),
                capacity: 40,
                allowDocuments: false,
                maxGroupSize: null,
                submissionDeadline: null,
                EventType.Palestra,
                null,
                EventStatus.Solicitado,
                approved[1].Id,
                pack.Courses.Take(1).ToList());

            db.Events.AddRange(pastDocs, pastSimple, future, requested);

            // Inscrições: só alunos aprovados do curso permitido
            // 5 inscritos; 4 presentes (subconjunto). Nunca presença sem inscrição.
            var pastDocsRegs = RegisterStudents(pastDocs, approved.Take(5).ToList(), now.AddDays(-20));
            MarkAttendanceSubset(pastDocsRegs, count: 3, pastDocs.StartDate.AddMinutes(30), AttendanceMethod.Code);
            MarkAttendanceSubset(pastDocsRegs, count: 1, pastDocs.StartDate.AddMinutes(45), AttendanceMethod.Manual);
            AddFeedbacksForAttendees(db, pastDocs, pastDocsRegs, now.AddDays(-9));

            var pastSimpleRegs = RegisterStudents(pastSimple, approved.Skip(1).Take(4).ToList(), now.AddDays(-22));
            MarkAttendanceSubset(pastSimpleRegs, count: 2, pastSimple.StartDate.AddMinutes(20), AttendanceMethod.Code);
            AddFeedbacksForAttendees(db, pastSimple, pastSimpleRegs, now.AddDays(-17));

            var futureRegs = RegisterStudents(future, approved.Take(5).ToList(), now.AddDays(-3));
            // Um cancelamento válido (inscrito antes)
            var toCancel = futureRegs[^1];
            toCancel.Status = RegistrationStatus.Canceled;
            toCancel.CanceledAt = now.AddDays(-1);
            toCancel.AttendedAt = null;
            toCancel.AttendanceMethod = null;

            db.Set<EventAddedToCalendar>().Add(new EventAddedToCalendar
            {
                EventId = future.Id,
                UserId = teste.Id,
                AddedAt = now.AddDays(-1),
                IsActive = true
            });

            // Grupo apenas com inscritos presentes/confirmados do pastDocs
            var groupMembers = pastDocsRegs
                .Where(r => r.Status == RegistrationStatus.Confirmed)
                .Take(3)
                .Select(r => r.Student!)
                .ToList();

            if (groupMembers.Count >= 2)
            {
                var group = new Group
                {
                    Id = Guid.NewGuid(),
                    EventId = pastDocs.Id,
                    LeaderId = groupMembers[0].Id,
                    Number = 1,
                    IsActive = true,
                    CreatedAt = now.AddDays(-15)
                };
                db.Groups.Add(group);

                foreach (var member in groupMembers)
                {
                    db.Set<GroupMember>().Add(new GroupMember
                    {
                        GroupId = group.Id,
                        StudentId = member.Id,
                        EventId = pastDocs.Id,
                        JoinedAt = now.AddDays(-14),
                        Status = GroupMemberStatus.Accepted
                    });
                }

                var approvedDoc = new Document
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    SenderId = groupMembers[0].Id,
                    OriginalFileName = "artigo-final.pdf",
                    StorageKey = $"seed/{pack.Slug}/{pastDocs.Id}/artigo-final.pdf",
                    ContentType = "application/pdf",
                    FileSizeBytes = 245_760,
                    SubmittedAt = now.AddDays(-13),
                    Status = DocumentStatus.Approved
                };
                var pendingDoc = new Document
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    SenderId = groupMembers[0].Id,
                    OriginalFileName = "slides.pdf",
                    StorageKey = $"seed/{pack.Slug}/{pastDocs.Id}/slides.pdf",
                    ContentType = "application/pdf",
                    FileSizeBytes = 512_000,
                    SubmittedAt = now.AddDays(-12),
                    Status = DocumentStatus.Pending
                };
                var rejectedDoc = new Document
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    SenderId = groupMembers[1].Id,
                    OriginalFileName = "rascunho-v1.pdf",
                    StorageKey = $"seed/{pack.Slug}/{pastDocs.Id}/rascunho-v1.pdf",
                    ContentType = "application/pdf",
                    FileSizeBytes = 102_400,
                    SubmittedAt = now.AddDays(-13),
                    Status = DocumentStatus.Rejected
                };
                db.Documents.AddRange(approvedDoc, pendingDoc, rejectedDoc);
                db.DocumentApprovalHistories.AddRange(
                    new DocumentApprovalHistory
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = approvedDoc.Id,
                        ReviewerId = professor1.Id,
                        Status = DocumentStatus.Approved,
                        Feedback = "Documento aprovado. Boa fundamentação.",
                        ReviewedAt = now.AddDays(-12)
                    },
                    new DocumentApprovalHistory
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = rejectedDoc.Id,
                        ReviewerId = professor1.Id,
                        Status = DocumentStatus.Rejected,
                        Feedback = "Reprovado: faltam referências. Reenvie a versão corrigida.",
                        ReviewedAt = now.AddDays(-12)
                    });
            }
        }

        private static List<EventStudent> RegisterStudents(Event evt, List<Student> students, DateTime registeredAt)
        {
            if (students.Count > evt.Capacity)
                throw new InvalidOperationException("Seed inválido: inscritos acima da capacidade.");

            var regs = new List<EventStudent>();
            foreach (var student in students)
            {
                var reg = new EventStudent
                {
                    EventId = evt.Id,
                    StudentId = student.Id,
                    Student = student,
                    Status = RegistrationStatus.Confirmed,
                    RegisteredAt = registeredAt,
                    IsActive = true
                };
                evt.EventStudents.Add(reg);
                regs.Add(reg);
            }
            return regs;
        }

        private static void MarkAttendanceSubset(
            IReadOnlyList<EventStudent> registrations,
            int count,
            DateTime attendedAt,
            AttendanceMethod method)
        {
            // Regra de negócio: presença apenas para quem está inscrito (Confirmed).
            var targets = registrations
                .Where(r => r.Status == RegistrationStatus.Confirmed && r.AttendedAt == null)
                .Take(count)
                .ToList();

            if (count > registrations.Count(r => r.Status == RegistrationStatus.Confirmed))
                throw new InvalidOperationException("Seed inválido: tentativa de marcar mais presentes do que inscritos.");

            foreach (var reg in targets)
            {
                reg.AttendedAt = attendedAt;
                reg.AttendanceMethod = method;
                // Mantém Confirmed + AttendedAt, alinhado ao AttendanceRepository/AttendanceService.
            }
        }

        private static void AddFeedbacksForAttendees(
            AppDbContext db,
            Event evt,
            IEnumerable<EventStudent> registrations,
            DateTime createdAt)
        {
            var texts = new[]
            {
                "Excelente organização e conteúdo atual.",
                "Gostei das discussões práticas.",
                "Boa mediação; poderia ter mais networking.",
                "Aprendizado aplicável à graduação."
            };

            var i = 0;
            foreach (var reg in registrations.Where(r => r.AttendedAt != null))
            {
                db.EventFeedbacks.Add(new EventFeedback
                {
                    Id = Guid.NewGuid(),
                    EventId = evt.Id,
                    StudentId = reg.StudentId,
                    Rating = 7 + (i % 4),
                    FeedbackText = texts[i % texts.Length],
                    CreatedAt = createdAt
                });
                i++;
            }
        }

        private static void AcceptTerms(AppDbContext db, IReadOnlyList<LegalDocument> docs, params User[] users)
        {
            foreach (var user in users)
            {
                if (user.Role == UserRole.Administrador)
                    continue;

                foreach (var doc in docs)
                {
                    db.TermAcceptances.Add(new TermAcceptance
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        LegalDocumentId = doc.Id,
                        Version = doc.Version,
                        AcceptedAtUtc = DateTime.UtcNow,
                        IpAddress = "127.0.0.1",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        private static Institution NewInstitution(string name, string cnpj, string address, string phone, DateTime createdAt) =>
            new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                Cnpj = Cnpj.Create(cnpj),
                Address = address,
                Phone = phone,
                IsActive = true,
                CreatedAt = createdAt
            };

        private static List<Course> AddCourses(AppDbContext db, Institution institution, DateTime now, params string[] names)
        {
            var list = names.Select(n => new Course
            {
                Id = Guid.NewGuid(),
                Name = n,
                InstitutionId = institution.Id,
                IsActive = true,
                CreatedAt = now.AddDays(-100)
            }).ToList();
            db.Courses.AddRange(list);
            return list;
        }

        private static Student NewStudent(
            string name,
            string email,
            string passwordHash,
            Guid institutionId,
            Guid courseId,
            string ra,
            UserApprovalStatus status,
            Guid? approvedBy,
            DateTime? approvedDate,
            DateTime createdAt) =>
            new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                PasswordHash = passwordHash,
                InstitutionId = institutionId,
                CourseId = courseId,
                UniqueIdentifier = ra,
                ApprovalStatus = status,
                ApprovedByUserId = approvedBy,
                ApprovedDate = approvedDate,
                CreatedAt = createdAt,
                IsActive = true,
                IsPasswordChangeRequired = false
            };

        /// <summary>
        /// Gera código de presença com as mesmas regras do AttendanceService:
        /// 6 chars de A-Z/2-9 (sem I, L, O, 0, 1), via RandomNumberGenerator, único no seed.
        /// </summary>
        private static string CreateUniqueAttendanceCode(HashSet<string> usedCodes)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var chars = new char[AttendanceCodeLength];
                for (var i = 0; i < AttendanceCodeLength; i++)
                    chars[i] = AttendanceCodeChars[RandomNumberGenerator.GetInt32(AttendanceCodeChars.Length)];

                var code = new string(chars);
                if (usedCodes.Add(code))
                    return code;
            }

            throw new InvalidOperationException("Não foi possível gerar um código de presença único para o seed.");
        }

        private static Event NewEvent(
            Guid institutionId,
            string name,
            string description,
            DateTime start,
            DateTime end,
            int capacity,
            bool allowDocuments,
            int? maxGroupSize,
            DateTime? submissionDeadline,
            EventType type,
            string? attendanceCode,
            EventStatus status,
            Guid createdBy,
            IReadOnlyList<Course> allowedCourses)
        {
            var evt = new Event
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                InstitutionId = institutionId,
                StartDate = start,
                EndDate = end,
                Capacity = capacity,
                IsActive = true,
                AllowDocuments = allowDocuments,
                MaxGroupSize = maxGroupSize,
                SubmissionDeadline = submissionDeadline,
                EventType = type,
                AttendanceCode = attendanceCode,
                EventStatus = status,
                CreatedByUserId = createdBy
            };

            foreach (var course in allowedCourses)
            {
                evt.EventAllowedCourses.Add(new EventAllowedCourse
                {
                    EventId = evt.Id,
                    CourseId = course.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    IsActive = true
                });
            }

            return evt;
        }

        private static string EmailFromName(string name, object suffix, string domain)
        {
            var parts = name.ToLowerInvariant()
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray();
            var ascii = new string(parts);
            var tokens = ascii.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var local = $"{tokens[0]}.{tokens[^1]}{suffix}".Replace(" ", "");
            return $"{local}@{domain}";
        }

        private sealed record InstitutionPack(
            Institution Institution,
            List<Course> Courses,
            string Slug,
            string EmailDomain,
            string SecretaryName,
            string Teacher1Name,
            string Teacher2Name);

        private static class StudentNames
        {
            public static List<string> For(string slug) => slug switch
            {
                "umc" =>
                [
                    "Gabriel Henrique Oliveira", "Larissa Fernanda Martins", "Beatriz Souza Carvalho",
                    "Amanda Cristina Lopes", "Rafael Moreira Pinto", "Isabela Duarte Freitas",
                    "Matheus Vinícius Barbosa", "Camila Aparecida Vieira", "João Pedro Almeida",
                    "Ana Clara Mendes", "Thiago Augusto Nascimento"
                ],
                "ubc" =>
                [
                    "Lucas Gabriel Ferreira", "Mariana Luiza Correia", "Bruna Cristina Araujo",
                    "Pedro Henrique Santos", "Júlia Beatriz Ramos", "Renata Oliveira Dias",
                    "Gustavo Lima Pereira", "Natália Souza Ribeiro", "Felipe Rocha Teixeira",
                    "Letícia Mendes Carvalho", "Diego Henrique Campos"
                ],
                "fatec" =>
                [
                    "Bruno César Antunes", "Sofia Ribeiro Campos", "Helena Cristina Duarte",
                    "Vinícius Andrade Costa", "Paula Regina Monteiro", "Caio Augusto Barbosa",
                    "Yasmin Fernanda Lopes", "Rodrigo Alves Pimentel", "Carolina Dias Azevedo",
                    "Marcelo Henrique Prado", "Eduardo Farias Melo"
                ],
                _ =>
                [
                    "Aline Souza Nogueira", "Daniel Ribeiro Fontes", "Pedro Henrique Santos",
                    "Júlia Beatriz Ramos", "Renata Oliveira Dias", "Gustavo Lima Pereira",
                    "Natália Souza Ribeiro", "Patrícia Gomes Vieira", "Felipe Rocha Teixeira",
                    "Letícia Mendes Carvalho", "Bruna Cristina Araujo"
                ]
            };
        }
    }
}
