import React, { useEffect, useState } from "react";
import { api } from "../services/api";
import {
  AlertCircle,
  Plus,
  ListFilter,
  ClipboardList,
} from "lucide-react";
import { CreateEventForm } from "@/components/events/CreateEventForm";
import {
  EventsList,
  type EventItem,
} from "@/components/events/EventList";
import { RequestedEventsPanel } from "@/components/events/RequestedEventsPanel";
import { Button } from "@/components/ui/button";
import { type CreateEventFormData } from "../schemas/eventSchema";
import { useAuth } from "../hooks/useAuth";
import type { OptionItem } from "@/types/optionItem";
import { formatForDatetimeLocal } from "@/utils/format";
import { InitialLoading } from "@/components/InitialLoading";
import { PageHeader } from "@/components/PageHeader";

export const Events: React.FC = () => {
  const [institutions, setInstitutions] = useState<OptionItem[]>([]);
  const [courses, setCourses] = useState<OptionItem[]>([]);
  const [eventTypes, setEventTypes] = useState<OptionItem[]>([]);
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);
  const [editing, setIsEditing] = useState(false);
  const [viewingRequested, setViewingRequested] = useState(false);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const [currentEventBeingUpdated, setEventBeingUpdated] = useState<
    (CreateEventFormData & { id: string; isActive: boolean }) | null
  >(null);

  const { user } = useAuth();
  const userInstitutionId = user?.institutionId || null;

  const fetchCourses = async (institutionId: string | null) => {
    await api
      .get<OptionItem[]>("/courses", { params: { institutionId } })
      .then((res) => {
        setCourses(res.data);
      });
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
        fetchCourses(userInstitutionId);
        setEventTypes(eventTypesRes.data);
      } catch {
        if (controller.signal.aborted) return;
        setPageError("Não foi possível carregar as informações do sistema.");
      } finally {
        setIsInitialLoading(false);
      }
    };

    loadInitialData();

    return () => {
      controller.abort();
    };
  }, [userInstitutionId]);

  const handleInstitutionChange = (institutionId: string) => {
    fetchCourses(institutionId);
  };

  const handleStartEdition = (eventItem: EventItem) => {
    fetchCourses(eventItem.institutionId);
    setEventBeingUpdated({
      id: eventItem.id,
      name: eventItem.name,
      description: eventItem.description,
      institutionId: eventItem.institutionId,
      startDate: formatForDatetimeLocal(eventItem.startDate),
      endDate: formatForDatetimeLocal(eventItem.endDate),
      capacity: eventItem.capacity,
      eventType: eventItem.eventType,
      allowDocuments: eventItem.allowDocuments,
      allowedCourses: eventItem.allowedCourseIds,
      isActive: eventItem.isActive,
    });
    setViewingRequested(false);
    setIsEditing(true);
  };

  const handleCreateEvent = async (data: CreateEventFormData) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      if (currentEventBeingUpdated) {
        await api.put(`/events/${currentEventBeingUpdated.id}`, data);
        if (!currentEventBeingUpdated.isActive) {
          await api.patch(`/events/${currentEventBeingUpdated.id}/active`, {
            isActive: true,
          });
        }
      } else {
        await api.post("/events", data);
      }
      setIsSuccess(true);
      setTimeout(() => {
        setIsSuccess(false);
        setIsEditing(false);
      }, 1500);
    } catch (err: any) {
      setSubmitError(err.response?.data?.message || "Erro ao criar o evento.");
    } finally {
      setIsSubmitting(false);
    }
  };

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
    <div className="app-page">
      <PageHeader
        title={
          editing
            ? currentEventBeingUpdated
              ? currentEventBeingUpdated.isActive
                ? "Atualizar evento"
                : "Reativar evento"
              : "Novo evento"
            : viewingRequested
              ? "Eventos Solicitados"
              : "Eventos"
        }
        description={
          editing
            ? "Preencha os dados para salvar o evento."
            : viewingRequested
              ? "Avalie e aprove ou reprove os eventos solicitados pelos alunos."
              : "Consulte, filtre e gerencie os eventos acadêmicos."
        }
        action={
          <div className="flex flex-wrap items-center gap-2">
            {editing || viewingRequested ? (
              <Button
                onClick={() => {
                  setIsEditing(false);
                  setViewingRequested(false);
                  setSubmitError(null);
                  setEventBeingUpdated(null);
                }}
                variant="outline"
                className="secondary-action cursor-pointer">
                <ListFilter className="mr-2 h-4 w-4" />
                Ver Lista de Eventos
              </Button>
            ) : (
              <>
                <Button
                  onClick={() => {
                    setIsEditing(false);
                    setViewingRequested(true);
                  }}
                  variant="outline"
                  className="secondary-action cursor-pointer">
                  <ClipboardList className="mr-2 h-4 w-4" />
                  Eventos Solicitados
                </Button>

                <Button
                  onClick={() => {
                    setViewingRequested(false);
                    setIsEditing(true);
                    setSubmitError(null);
                    setEventBeingUpdated(null);
                  }}
                  variant="default"
                  className="primary-action cursor-pointer">
                  <Plus className="mr-2 h-4 w-4" />
                  Criar Evento
                </Button>
              </>
            )}
          </div>
        }
      />

      {editing ? (
        <CreateEventForm
          institutions={institutions}
          courses={courses}
          eventTypes={eventTypes}
          userInstitutionId={userInstitutionId}
          onSubmit={handleCreateEvent}
          isSubmitting={isSubmitting}
          isSuccess={isSuccess}
          apiError={submitError}
          currentEventData={currentEventBeingUpdated}
          handleInstitutionChange={handleInstitutionChange}
        />
      ) : viewingRequested ? (
        <RequestedEventsPanel userInstitutionId={userInstitutionId} />
      ) : (
        <EventsList
          institutions={institutions}
          userInstitutionId={userInstitutionId}
          handleStartEdition={handleStartEdition}
        />
      )}
    </div>
  );
};
