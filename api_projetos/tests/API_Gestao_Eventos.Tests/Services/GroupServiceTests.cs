using API_Gestao_Eventos.src.Application.DTO.Group;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using Moq;

namespace API_Gestao_Eventos.Tests.Services
{
    public class GroupServiceTests
    {
        private readonly Mock<IGroupRepository> _groupRepositoryMock;
        private readonly Mock<IEventRepository> _eventRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly GroupService _groupService;

        public GroupServiceTests()
        {
            _groupRepositoryMock = new Mock<IGroupRepository>();
            _eventRepositoryMock = new Mock<IEventRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _groupService = new GroupService(_groupRepositoryMock.Object, _eventRepositoryMock.Object, _userRepositoryMock.Object);
        }

        private static Event SeedEvent(
            bool isActive = true,
            bool allowDocuments = true,
            DateTime? startDate = null,
            int? maxGroupSize = 3,
            Guid? courseId = null) => new()
            {
                Id = Guid.NewGuid(),
                Name = "Evento Teste",
                IsActive = isActive,
                AllowDocuments = allowDocuments,
                StartDate = startDate ?? DateTime.UtcNow.AddDays(2),
                EndDate = DateTime.UtcNow.AddDays(3),
                MaxGroupSize = maxGroupSize,
                AllowedCourses = courseId.HasValue ? new List<Course> { new() { Id = courseId.Value, Name = "ADS" } } : new List<Course>()
            };

        private static Group SeedGroup(Guid eventId, Guid leaderId, bool isActive = true)
        {
            var group = new Group
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                LeaderId = leaderId,
                Number = 1,
                IsActive = isActive,
                Event = new Event { Id = eventId, Name = "Evento Teste" }
            };
            group.Members.Add(new GroupMember
            {
                GroupId = group.Id,
                EventId = eventId,
                StudentId = leaderId,
                Status = GroupMemberStatus.Accepted,
                Student = new Student { Id = leaderId, Name = "Líder" },
                JoinedAt = DateTime.UtcNow.AddMinutes(-10)
            });
            return group;
        }

        [Fact]
        public async Task CreateGroupAsync_QuandoEventoInativo_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(isActive: false);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _groupService.CreateGroupAsync(Guid.NewGuid(), new CreateGroupRequestDto { EventId = evt.Id }));

            Assert.Equal("Não é possível criar grupo em um evento inativo.", exception.Message);
        }

        [Fact]
        public async Task CreateGroupAsync_QuandoEventoNaoPermiteDocumentos_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(allowDocuments: false);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _groupService.CreateGroupAsync(Guid.NewGuid(), new CreateGroupRequestDto { EventId = evt.Id }));

            Assert.Equal("Este evento não permite envio de documentos.", exception.Message);
        }

        [Fact]
        public async Task CreateGroupAsync_QuandoPrazoEncerrado_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(startDate: DateTime.UtcNow.AddHours(-1));
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _groupService.CreateGroupAsync(Guid.NewGuid(), new CreateGroupRequestDto { EventId = evt.Id }));

            Assert.Equal("O prazo para montar grupos já terminou.", exception.Message);
        }

        [Fact]
        public async Task CreateGroupAsync_QuandoLiderNaoInscrito_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var leaderId = Guid.NewGuid();
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _groupRepositoryMock.Setup(r => r.IsStudentRegisteredAsync(leaderId, evt.Id)).ReturnsAsync(false);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _groupService.CreateGroupAsync(leaderId, new CreateGroupRequestDto { EventId = evt.Id }));

            Assert.Equal("Você precisa estar inscrito no evento para criar um grupo.", exception.Message);
        }

        [Fact]
        public async Task CreateGroupAsync_QuandoLiderJaParticiparDeGrupo_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var leaderId = Guid.NewGuid();
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _groupRepositoryMock.Setup(r => r.IsStudentRegisteredAsync(leaderId, evt.Id)).ReturnsAsync(true);
            _groupRepositoryMock.Setup(r => r.HasAcceptedGroupAsync(leaderId, evt.Id)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _groupService.CreateGroupAsync(leaderId, new CreateGroupRequestDto { EventId = evt.Id }));

            Assert.Equal("Você já participa de um grupo neste evento.", exception.Message);
        }

        [Fact]
        public async Task CreateGroupAsync_QuandoExcederLimiteMaximoDoGrupo_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent(maxGroupSize: 2);
            var leaderId = Guid.NewGuid();
            var guest1 = Guid.NewGuid();
            var guest2 = Guid.NewGuid();
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _groupRepositoryMock.Setup(r => r.IsStudentRegisteredAsync(leaderId, evt.Id)).ReturnsAsync(true);
            _groupRepositoryMock.Setup(r => r.HasAcceptedGroupAsync(leaderId, evt.Id)).ReturnsAsync(false);
            _groupRepositoryMock.Setup(r => r.GetStudentsAvailableForGroupAsync(evt.Id))
                .ReturnsAsync(new[] { new Student { Id = guest1 }, new Student { Id = guest2 } });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _groupService.CreateGroupAsync(leaderId, new CreateGroupRequestDto
                {
                    EventId = evt.Id,
                    StudentIds = new List<Guid> { guest1, guest2 }
                }));

            Assert.Equal("O grupo pode ter no máximo 2 alunos.", exception.Message);
        }

        [Fact]
        public async Task CreateGroupAsync_QuandoDadosValidos_DeveCriarGrupoERecusarConvitesPendentes()
        {
            var evt = SeedEvent(maxGroupSize: 3);
            var leaderId = Guid.NewGuid();
            var guestId = Guid.NewGuid();
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _groupRepositoryMock.Setup(r => r.IsStudentRegisteredAsync(leaderId, evt.Id)).ReturnsAsync(true);
            _groupRepositoryMock.Setup(r => r.HasAcceptedGroupAsync(leaderId, evt.Id)).ReturnsAsync(false);
            _groupRepositoryMock.Setup(r => r.GetStudentsAvailableForGroupAsync(evt.Id))
                .ReturnsAsync(new[] { new Student { Id = guestId } });
            _groupRepositoryMock.Setup(r => r.GetNextNumberAsync(evt.Id)).ReturnsAsync(1);

            var groupId = await _groupService.CreateGroupAsync(leaderId, new CreateGroupRequestDto
            {
                EventId = evt.Id,
                StudentIds = new List<Guid> { guestId }
            });

            Assert.NotEqual(Guid.Empty, groupId);
            _groupRepositoryMock.Verify(r => r.AddAsync(It.Is<Group>(g => g.LeaderId == leaderId && g.Members.Count == 2)), Times.Once);
            _groupRepositoryMock.Verify(r => r.DeclinePendingInvitesAsync(leaderId, evt.Id, groupId), Times.Once);
        }

        [Fact]
        public async Task InviteStudentsAsync_QuandoUsuarioNaoForLider_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var group = SeedGroup(evt.Id, Guid.NewGuid());
            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _groupService.InviteStudentsAsync(group.Id, Guid.NewGuid(), new InviteGroupMembersRequestDto { StudentIds = new List<Guid>() }));

            Assert.Equal("Somente o líder pode alterar o grupo.", exception.Message);
        }

        [Fact]
        public async Task InviteStudentsAsync_QuandoDadosValidos_DeveAdicionarNovoMembroEReativarRecusado()
        {
            var evt = SeedEvent(maxGroupSize: 5);
            var leaderId = Guid.NewGuid();
            var newGuestId = Guid.NewGuid();
            var declinedGuestId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, leaderId);
            var declinedMember = new GroupMember { GroupId = group.Id, StudentId = declinedGuestId, EventId = evt.Id, Status = GroupMemberStatus.Declined };
            group.Members.Add(declinedMember);

            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _groupRepositoryMock.Setup(r => r.GetStudentsAvailableForGroupAsync(evt.Id))
                .ReturnsAsync(new[] { new Student { Id = newGuestId }, new Student { Id = declinedGuestId } });

            await _groupService.InviteStudentsAsync(group.Id, leaderId, new InviteGroupMembersRequestDto
            {
                StudentIds = new List<Guid> { newGuestId, declinedGuestId }
            });

            _groupRepositoryMock.Verify(r => r.AddMemberAsync(It.Is<GroupMember>(m => m.StudentId == newGuestId)), Times.Once);
            _groupRepositoryMock.Verify(r => r.SetMemberStatusAsync(declinedMember, GroupMemberStatus.Invited), Times.Once);
        }

        [Fact]
        public async Task RemoveMemberAsync_QuandoTentarRemoverOLider_DeveLancarInvalidOperationException()
        {
            var evt = SeedEvent();
            var leaderId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, leaderId);
            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _groupService.RemoveMemberAsync(group.Id, leaderId, leaderId));

            Assert.Equal("O líder não pode ser removido do grupo.", exception.Message);
        }

        [Fact]
        public async Task RemoveMemberAsync_QuandoMembroExistir_DeveRemoverMembro()
        {
            var evt = SeedEvent();
            var leaderId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, leaderId);
            var member = new GroupMember { GroupId = group.Id, StudentId = memberId, Status = GroupMemberStatus.Accepted };
            group.Members.Add(member);
            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);

            await _groupService.RemoveMemberAsync(group.Id, leaderId, memberId);

            _groupRepositoryMock.Verify(r => r.RemoveMemberAsync(member), Times.Once);
        }

        [Fact]
        public async Task AcceptInviteAsync_QuandoConviteValido_DeveAceitarConviteERecusarPendentes()
        {
            var evt = SeedEvent(maxGroupSize: 3);
            var leaderId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, leaderId);
            var invite = new GroupMember { GroupId = group.Id, StudentId = studentId, EventId = evt.Id, Status = GroupMemberStatus.Invited };
            group.Members.Add(invite);

            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);
            _groupRepositoryMock.Setup(r => r.IsStudentRegisteredAsync(studentId, evt.Id)).ReturnsAsync(true);
            _groupRepositoryMock.Setup(r => r.HasAcceptedGroupAsync(studentId, evt.Id)).ReturnsAsync(false);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);

            await _groupService.AcceptInviteAsync(group.Id, studentId);

            _groupRepositoryMock.Verify(r => r.SetMemberStatusAsync(invite, GroupMemberStatus.Accepted), Times.Once);
            _groupRepositoryMock.Verify(r => r.DeclinePendingInvitesAsync(studentId, evt.Id, group.Id), Times.Once);
        }

        [Fact]
        public async Task DeclineInviteAsync_QuandoConviteExistir_DeveMarcarComoRecusado()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, Guid.NewGuid());
            var invite = new GroupMember { GroupId = group.Id, StudentId = studentId, EventId = evt.Id, Status = GroupMemberStatus.Invited };
            group.Members.Add(invite);
            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);

            await _groupService.DeclineInviteAsync(group.Id, studentId);

            _groupRepositoryMock.Verify(r => r.SetMemberStatusAsync(invite, GroupMemberStatus.Declined), Times.Once);
        }

        [Fact]
        public async Task LeaveGroupAsync_QuandoLiderSairSemOutrosMembrosAceitos_DeveExcluirGrupo()
        {
            var evt = SeedEvent();
            var leaderId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, leaderId);
            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);

            await _groupService.LeaveGroupAsync(group.Id, leaderId);

            _groupRepositoryMock.Verify(r => r.RemoveMemberAsync(It.Is<GroupMember>(m => m.StudentId == leaderId)), Times.Once);
            _groupRepositoryMock.Verify(r => r.DeleteAsync(group.Id), Times.Once);
        }

        [Fact]
        public async Task LeaveGroupAsync_QuandoLiderSairComOutroMembroAceito_DeveTransferirLideranca()
        {
            var evt = SeedEvent();
            var leaderId = Guid.NewGuid();
            var nextLeaderId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, leaderId);
            group.Members.Add(new GroupMember
            {
                GroupId = group.Id,
                StudentId = nextLeaderId,
                EventId = evt.Id,
                Status = GroupMemberStatus.Accepted,
                JoinedAt = DateTime.UtcNow
            });
            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);

            await _groupService.LeaveGroupAsync(group.Id, leaderId);

            Assert.Equal(nextLeaderId, group.LeaderId);
            _groupRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetOpenGroupsAsync_QuandoAlunoElegivel_DeveRetornarGruposComVaga()
        {
            var evt = SeedEvent(maxGroupSize: 3);
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, Guid.NewGuid());
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _groupRepositoryMock.Setup(r => r.IsStudentRegisteredAsync(studentId, evt.Id)).ReturnsAsync(true);
            _groupRepositoryMock.Setup(r => r.HasAcceptedGroupAsync(studentId, evt.Id)).ReturnsAsync(false);
            _groupRepositoryMock.Setup(r => r.GetActiveByEventAsync(evt.Id)).ReturnsAsync(new[] { group });

            var result = (await _groupService.GetOpenGroupsAsync(evt.Id, studentId)).ToList();

            Assert.Single(result);
            Assert.Equal(group.Id, result[0].Id);
            Assert.Equal(2, result[0].AvailableSlots);
        }

        [Fact]
        public async Task RequestToJoinAsync_QuandoAlunoElegivelSemMembroAnterior_DeveAdicionarPedido()
        {
            var evt = SeedEvent(maxGroupSize: 3);
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, Guid.NewGuid());
            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _groupRepositoryMock.Setup(r => r.IsStudentRegisteredAsync(studentId, evt.Id)).ReturnsAsync(true);
            _groupRepositoryMock.Setup(r => r.HasAcceptedGroupAsync(studentId, evt.Id)).ReturnsAsync(false);

            await _groupService.RequestToJoinAsync(group.Id, studentId);

            _groupRepositoryMock.Verify(r => r.AddMemberAsync(It.Is<GroupMember>(m =>
                m.StudentId == studentId && m.Status == GroupMemberStatus.Requested)), Times.Once);
        }

        [Fact]
        public async Task CancelRequestAsync_QuandoPedidoExistir_DeveRemoverMembro()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, Guid.NewGuid());
            var requestMember = new GroupMember { GroupId = group.Id, StudentId = studentId, Status = GroupMemberStatus.Requested };
            group.Members.Add(requestMember);
            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);

            await _groupService.CancelRequestAsync(group.Id, studentId);

            _groupRepositoryMock.Verify(r => r.RemoveMemberAsync(requestMember), Times.Once);
        }

        [Fact]
        public async Task ApproveRequestAsync_QuandoPedidoValido_DeveAceitarMembroERecusarPendentes()
        {
            var evt = SeedEvent(maxGroupSize: 3);
            var leaderId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, leaderId);
            var requestMember = new GroupMember { GroupId = group.Id, StudentId = studentId, EventId = evt.Id, Status = GroupMemberStatus.Requested };
            group.Members.Add(requestMember);

            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _groupRepositoryMock.Setup(r => r.HasAcceptedGroupAsync(studentId, evt.Id)).ReturnsAsync(false);

            await _groupService.ApproveRequestAsync(group.Id, leaderId, studentId);

            _groupRepositoryMock.Verify(r => r.SetMemberStatusAsync(requestMember, GroupMemberStatus.Accepted), Times.Once);
            _groupRepositoryMock.Verify(r => r.DeclinePendingInvitesAsync(studentId, evt.Id, group.Id), Times.Once);
        }

        [Fact]
        public async Task RejectRequestAsync_QuandoPedidoExistir_DeveRemoverPedido()
        {
            var evt = SeedEvent();
            var leaderId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, leaderId);
            var requestMember = new GroupMember { GroupId = group.Id, StudentId = studentId, Status = GroupMemberStatus.Requested };
            group.Members.Add(requestMember);
            _groupRepositoryMock.Setup(r => r.GetByIdAsync(group.Id)).ReturnsAsync(group);

            await _groupService.RejectRequestAsync(group.Id, leaderId, studentId);

            _groupRepositoryMock.Verify(r => r.RemoveMemberAsync(requestMember), Times.Once);
        }

        [Fact]
        public async Task GetMyGroupAsync_EConsultasDeConvitesEGruposDoEvento_DevemRetornarDtosMapeados()
        {
            var courseId = Guid.NewGuid();
            var evt = SeedEvent(courseId: courseId);
            var leaderId = Guid.NewGuid();
            var group = SeedGroup(evt.Id, leaderId);
            var teacher = new Teacher { Id = Guid.NewGuid(), Courses = new List<Course> { new() { Id = courseId } } };

            _groupRepositoryMock.Setup(r => r.GetGroupForStudentInEventAsync(leaderId, evt.Id)).ReturnsAsync(group);
            _groupRepositoryMock.Setup(r => r.GetInvitesForStudentAsync(leaderId)).ReturnsAsync(new[] { group });
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(teacher.Id)).ReturnsAsync(teacher);
            _groupRepositoryMock.Setup(r => r.GetByEventAsync(evt.Id)).ReturnsAsync(new[] { group });
            _groupRepositoryMock.Setup(r => r.GetStudentsAvailableForGroupAsync(evt.Id))
                .ReturnsAsync(new[] { new Student { Id = leaderId, Name = "Líder" }, new Student { Id = Guid.NewGuid(), Name = "Convidável" } });

            var myGroup = await _groupService.GetMyGroupAsync(evt.Id, leaderId);
            var myInvites = (await _groupService.GetMyInvitesAsync(leaderId)).ToList();
            var eventGroups = (await _groupService.GetEventGroupsAsync(evt.Id, teacher.Id)).ToList();
            var invitable = (await _groupService.GetInvitableStudentsAsync(evt.Id, leaderId)).ToList();

            Assert.NotNull(myGroup);
            Assert.Equal(group.Id, myGroup.Id);
            Assert.Single(myInvites);
            Assert.Single(eventGroups);
            Assert.Single(invitable);
            Assert.Equal("Convidável", invitable[0].Label);
        }
    }
}

