import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { AlertCircle, ListFilter, Plus } from "lucide-react";
import { useEffect, useState } from "react";
import { PageHeader } from "@/components/PageHeader";
import { EventsList } from "@/components/events/EventList";
import { CreateEventForm } from "@/components/events/CreateEventForm";
import { type CreateEventFormData } from "@/schemas/eventSchema";
import api from "@/services/api";
import type { OptionItem } from "@/types/optionItem";
import { InitialLoading } from "@/components/InitialLoading";

export const EventsStudent: React.FC = () => {
  const [pageError, setPageError] = useState<string | null>(null);
  const { user } = useAuth();
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [institutions, setInstitutions] = useState<OptionItem[]>([]);
  const [courses, setCourses] = useState<OptionItem[]>([]);
  const [eventTypes, setEventTypes] = useState<OptionItem[]>([]);

  const [isCreating, setIsCreating] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const userInstitutionId = user?.institutionId || null;

  const fetchCourses = async (institutionId: string | null) => {
    const res = await api.get<OptionItem[]>("/courses", {
      params: { institutionId },
    });
    setCourses(res.data);
  };

  const handleInstitutionChange = (institutionId: string) => {
    void fetchCourses(institutionId);
  };

  const handleCreateEvent = async (data: CreateEventFormData) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      await api.post("/events", data);
      setIsSuccess(true);
      setTimeout(() => {
        setIsSuccess(false);
        setIsCreating(false);
      }, 1500);
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { message?: string } } };
      setSubmitError(
        errorObj.response?.data?.message || "Erro ao solicitar o evento.",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleJoinEvent = async (eventId: string) => {
    setPageError(null);
    try {
      await api.post(`/events/${eventId}/join`);
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { message?: string } } };
      setPageError(
        errorObj.response?.data?.message || "Erro ao entrar no evento.",
      );
    }
  };

  const handleLeaveEvent = async (eventId: string) => {
    setPageError(null);
    try {
      await api.post(`/events/${eventId}/leave`);
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { message?: string } } };
      setPageError(errorObj.response?.data?.message || "Erro ao sair do evento.");
    }
  };

  useEffect(() => {
    const controller = new AbortController();

    const loadInitialData = async () => {
      try {
        const [instRes, eventTypesRes] = await Promise.all([
          api.get<OptionItem[]>("/institutions"),
          api.get<OptionItem[]>("/events/types"),
        ]);
        setInstitutions(instRes.data);
        setEventTypes(eventTypesRes.data);
        await fetchCourses(userInstitutionId);
      } catch {
        if (controller.signal.aborted) return;
        setPageError("Não foi possível carregar as informações do sistema.");
      } finally {
        setIsInitialLoading(false);
      }
    };

    void loadInitialData();

    return () => {
      controller.abort();
    };
  }, [userInstitutionId]);

  if (isInitialLoading) {
    return <InitialLoading></InitialLoading>;
  }
  if (pageError) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-4">
        <div className="flex max-w-md flex-col items-center gap-4 rounded-xl bg-white p-8 text-center shadow-lg">
          <AlertCircle className="h-12 w-12 text-red-500" />
          <p className="text-slate-800 font-medium">{pageError}</p>
          <Button
            onClick={() => window.location.reload()}
            variant="outline"
            className="mt-2">
            Tentar novamente
          </Button>
        </div>
      </div>
    );
  }
  return (
    <div className={"app-page"}>
      <PageHeader
        title={isCreating ? "Solicitar evento" : "Eventos"}
        description={
          isCreating
            ? "Preencha os dados para solicitar um novo evento acadêmico."
            : "Inscreva-se em eventos disponíveis ou solicite a criação de um novo evento"
        }
        action={
          <Button
            onClick={() => {
              setIsCreating(!isCreating);
              setSubmitError(null);
            }}
            variant={isCreating ? "outline" : "default"}
            className={isCreating ? "secondary-action" : "primary-action"}>
            {isCreating ? (
              <>
                <ListFilter className="mr-2 h-4 w-4" />
                Ver Lista de Eventos
              </>
            ) : (
              <>
                <Plus className="mr-2 h-4 w-4" />
                Solicitar Evento
              </>
            )}
          </Button>
        }></PageHeader>

      {isCreating ? (
        <CreateEventForm
          institutions={institutions}
          courses={courses}
          eventTypes={eventTypes}
          userInstitutionId={userInstitutionId}
          onSubmit={handleCreateEvent}
          isSubmitting={isSubmitting}
          isSuccess={isSuccess}
          apiError={submitError}
          handleInstitutionChange={handleInstitutionChange}
        />
      ) : (
        <EventsList
          userInstitutionId={userInstitutionId}
          studentMode={true}
          institutions={institutions}
          onJoinEvent={handleJoinEvent}
          onLeaveEvent={handleLeaveEvent}></EventsList>
      )}
    </div>
  );
};
