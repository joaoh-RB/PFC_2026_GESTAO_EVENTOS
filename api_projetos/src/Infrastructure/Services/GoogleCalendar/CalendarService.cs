using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using EventEntity = API_Gestao_Eventos.src.Domain.Entities.Event;

namespace API_Gestao_Eventos.src.Infrastructure.Services.GoogleCalendar
{
    public class GoogleCalendarService
    {
        private readonly CalendarService _calendarService;
        private readonly string _calendarId;
        private readonly ILogger<GoogleCalendarService> _logger;

        public GoogleCalendarService(IConfiguration configuration, ILogger<GoogleCalendarService> logger)
        {
            _logger = logger;
            var config = configuration.GetSection("GoogleCalendar");

            _calendarId = config["CalendarId"]?.Trim()
                ?? throw new InvalidOperationException("GoogleCalendar:CalendarId não configurado.");
            var clientId = config["ClientId"]?.Trim()
                ?? throw new InvalidOperationException("GoogleCalendar:ClientId não configurado.");

            var clientSecret = config["ClientSecret"]?.Trim()
                ?? throw new InvalidOperationException("GoogleCalendar:ClientSecret não configurado.");

            var refreshToken = config["RefreshToken"]?.Trim()
                ?? throw new InvalidOperationException("GoogleCalendar:RefreshToken não configurado.");

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { CalendarService.Scope.Calendar }
            });

            var tokenResponse = new TokenResponse
            {
                RefreshToken = refreshToken
            };

            var credential = new UserCredential(flow, "gestao-eventos-admin", tokenResponse);

            _calendarService = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "GestaoEventosAcademicos"
            });
        }

        public async Task<string> CreateEventAsync(EventEntity eventEntity)
        {
            var newEvent = new Event
            {
                Summary = eventEntity.Name,
                Description = eventEntity.Description,
                Location = "",
                Start = new EventDateTime
                {
                    DateTimeDateTimeOffset = eventEntity.StartDate,
                    TimeZone = "America/Sao_Paulo"
                },
                End = new EventDateTime
                {
                    DateTimeDateTimeOffset = eventEntity.EndDate,
                    TimeZone = "America/Sao_Paulo"
                },
                GuestsCanInviteOthers = false,
                GuestsCanModify = false,
                GuestsCanSeeOtherGuests = false,
                Attendees = new List<EventAttendee>()
            };

            var request = _calendarService.Events.Insert(newEvent, _calendarId);
            request.SendUpdates = EventsResource.InsertRequest.SendUpdatesEnum.None;

            var createdEvent = await request.ExecuteAsync();
            return createdEvent.Id;
        }

        public async Task UpdateEventAsync(EventEntity eventEntity)
        {
            if (string.IsNullOrEmpty(eventEntity.GoogleCalendarEventId))
                return;
            try
            {
                var calendarEvent = await _calendarService.Events.Get(_calendarId, eventEntity.GoogleCalendarEventId).ExecuteAsync();

                calendarEvent.Summary = eventEntity.Name;
                calendarEvent.Description = eventEntity.Description;
                calendarEvent.Location = "";
                calendarEvent.Status = "confirmed";
                calendarEvent.Start = new EventDateTime
                {
                    DateTimeDateTimeOffset = eventEntity.StartDate,
                    TimeZone = "America/Sao_Paulo"
                };
                calendarEvent.GuestsCanInviteOthers = false;
                calendarEvent.GuestsCanModify = false;
                calendarEvent.GuestsCanSeeOtherGuests = false;
                calendarEvent.End = new EventDateTime
                {
                    DateTimeDateTimeOffset = eventEntity.EndDate,
                    TimeZone = "America/Sao_Paulo"
                };

                var updateRequest = _calendarService.Events.Update(calendarEvent, _calendarId, eventEntity.GoogleCalendarEventId);
                updateRequest.SendUpdates = EventsResource.UpdateRequest.SendUpdatesEnum.All;

                await updateRequest.ExecuteAsync();
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Evento {GoogleEventId} não encontrado no Google Calendar ao atualizar.", eventEntity.GoogleCalendarEventId);
            }
        }

        public async Task AddAttendeeAsync(string googleEventId, string attendeeEmail)
        {
            var calendarEvent = await _calendarService.Events.Get(_calendarId, googleEventId).ExecuteAsync();
            calendarEvent.Attendees ??= new List<EventAttendee>();

            var normalizedEmail = attendeeEmail.Trim().ToLowerInvariant();
            if (calendarEvent.Attendees.Any(a => a.Email.ToLowerInvariant() == normalizedEmail))
                return;
            calendarEvent.Attendees.Add(new EventAttendee
            {
                Email = normalizedEmail,
                ResponseStatus = "accepted"
            });

            var updateRequest = _calendarService.Events.Update(calendarEvent, _calendarId, googleEventId);
            updateRequest.SendUpdates = EventsResource.UpdateRequest.SendUpdatesEnum.All;
            await updateRequest.ExecuteAsync();
        }
        public async Task RemoveAttendeeAsync(string googleEventId, string attendeeEmail)
        {
            var calendarEvent = await _calendarService.Events.Get(_calendarId, googleEventId).ExecuteAsync();
            if (calendarEvent.Attendees == null) return;

            var normalizedEmail = attendeeEmail.Trim().ToLowerInvariant();
            var attendee = calendarEvent.Attendees.FirstOrDefault(a => a.Email.ToLowerInvariant() == normalizedEmail);

            if (attendee != null)
            {
                calendarEvent.Attendees.Remove(attendee);
                var updateRequest = _calendarService.Events.Update(calendarEvent, _calendarId, googleEventId);
                updateRequest.SendUpdates = EventsResource.UpdateRequest.SendUpdatesEnum.All;
                await updateRequest.ExecuteAsync();
            }
        }
        public async Task DeactivateEventAsync(string googleEventId, bool isActive)
        {
            try
            {
                if (googleEventId == null)
                    return;
                var calendarEvent = await _calendarService.Events.Get(_calendarId, googleEventId).ExecuteAsync();
                calendarEvent.Status = isActive ? "confirmed" : "cancelled";
                var updateRequest = _calendarService.Events.Update(calendarEvent, _calendarId, googleEventId);
                updateRequest.SendUpdates = EventsResource.UpdateRequest.SendUpdatesEnum.All;
                await updateRequest.ExecuteAsync();
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Evento {GoogleEventId} não encontrado para desativação no Google Calendar.", googleEventId);
            }
        }
    }
}