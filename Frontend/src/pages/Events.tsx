import React, { useEffect, useState, useCallback } from "react";
import { api } from "../services/api";
import { AlertCircle, Plus, ListFilter } from "lucide-react";
import { CreateEventForm } from "../components/CreateEventForm";
import {
  EventsList,
  type EventItem,
  type EventFilterState,
} from "@/components/EventList";
import { Button } from "@/components/ui/button";
import { type CreateEventFormData } from "../schemas/eventSchema";
import { useAuth } from "../hooks/useAuth";
import type { OptionItem } from "@/types/optionItem";
import { formatForDatetimeLocal } from "@/utils/format";
import { type PagedResult } from "@/types/utils";
import { InitialLoading } from "@/components/InitialLoading";

export const Events: React.FC = () => {
  const pageSize = 6;
  const [institutions, setInstitutions] = useState<OptionItem[]>([]);
  const [courses, setCourses] = useState<OptionItem[]>([]);
  const [eventTypes, setEventTypes] = useState<OptionItem[]>([]);
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);
  const [editing, setIsEditing] = useState(false);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const [eventsData, setEventsData] = useState<PagedResult<EventItem>>({
    items: [],
    totalItems: 0,
    pageNumber: 1,
    pageSize: pageSize,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  });
  const [currentEventBeingUpdated, setEventBeingUpdated] =
    useState<CreateEventFormData | null>(null);

  const [isEventsLoading, setIsEventsLoading] = useState(false);
  const [filters, setFilters] = useState<EventFilterState>({
    fromDate: new Date().toISOString().split("T")[0],
  });
  const [currentPage, setCurrentPage] = useState(1);

  const { user } = useAuth();
  const userInstitutionId = user?.institutionId || null;

  const fetchCourses = async (institutionId: string | null) => {
    await api.get<OptionItem[]>("/courses", { params: { institutionId } }).then((res) => {
      setCourses(res.data);
    })
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
        fetchCourses(null);
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
  }, []);


  const fetchEvents = useCallback(
    async (appliedFilters = filters) => {
      setIsEventsLoading(true);
      try {
        const params: Record<string, string | number> = {
          pageNumber: currentPage,
          pageSize: pageSize,
        };
        if (appliedFilters.fromDate) {
          params.fromDate = new Date(
            `${appliedFilters.fromDate}T00:00:00-03:00`,
          ).toISOString();
        }
        if (appliedFilters.institutionId) {
          params.institutionId = appliedFilters.institutionId;
        }

        const res = await api.get<PagedResult<EventItem>>("/events", {
          params,
        });

        setEventsData(res.data);
      } catch (err) {
        console.error("Erro ao buscar eventos", err);
      } finally {
        setIsEventsLoading(false);
      }
    },
    [filters, currentPage],
  );

  useEffect(() => {
    if (!editing) {
      fetchEvents(filters);
    }
  }, [editing, currentPage, fetchEvents, filters]);

  //handle dos outros copmonentes
  const handleFilterChange = (newFilters: EventFilterState) => {
    setCurrentPage(1);
    setFilters(newFilters);
  };

  const handleInstitutionChange = (institutionId: string) => {
    fetchCourses(institutionId);
  };
  const handlePageChange = (page: number) => {
    setCurrentPage(page);
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
    });
    setIsEditing(true);
  };

  //chamadas api evento
  const handleCreateEvent = async (data: CreateEventFormData) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      if (currentEventBeingUpdated) {
        await api.put(`/events/${currentEventBeingUpdated.id}`, data);
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
  const handleDelete = async (eventId: string) => {
    await api.delete(`/events/${eventId}`);
    setCurrentPage(1);
    fetchEvents();
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
    <div className="min-h-screen bg-slate-50 py-10">
      <div className="max-w-7xl mx-auto space-y-6">
        <div className="flex justify-between items-center">
          <h1 className="text-2xl font-bold text-slate-900">
            {editing
              ? currentEventBeingUpdated
                ? "Atualizar evento"
                : "Novo Evento"
              : "Eventos Disponíveis"}
          </h1>

          <Button
            onClick={() => {
              setIsEditing(!editing);
              setSubmitError(null);
              setEventBeingUpdated(null);
            }}
            variant={editing ? "outline" : "default"}
            className={editing ? "" : "bg-indigo-600 hover:bg-indigo-700"}>
            {editing ? (
              <>
                <ListFilter className="mr-2 h-4 w-4" />
                Ver Lista de Eventos
              </>
            ) : (
              <>
                <Plus className="mr-2 h-4 w-4" />
                Criar Evento
              </>
            )}
          </Button>
        </div>

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
        ) : (
          <EventsList
            data={eventsData}
            institutions={institutions}
            isLoading={isEventsLoading}
            currentFilters={filters}
            onFilter={handleFilterChange}
            onPageChange={handlePageChange}
            handleStartEdition={handleStartEdition}
            handleDelete={handleDelete}
          />
        )}
      </div>
    </div>
  );
};
