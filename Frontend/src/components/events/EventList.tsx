import React, { useState, useEffect, useCallback } from "react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import {
  Calendar,
  Clock,
  MapPin,
  Users,
  Loader2,
  FileUp,
  LogIn,
  LogOut,
  ClipboardCheck,
} from "lucide-react";
import { type PagedResult } from "@/types/utils";
import { formatDateForShort } from "@/utils/format";
import type { OptionItem } from "@/types/optionItem";
import { DeleteConfirmation } from "@/components/ui/deleteConfirm";
import { useAuth } from "@/hooks/useAuth";
import { api } from "@/services/api";
import { AddToCalendarButton } from "./AddToCalendarButton";
import { RemoveFromCalendarButton } from "./RemoveFromCalendarButton";
import { SubmitDocumentModal } from "./SubmitDocumentModal";
import { AttendanceConfirm } from "./AttendanceConfirm";
import { AttendanceModal } from "./AttendanceModal";

export interface FetchEventsParams {
  pageNumber?: number;
  pageSize?: number;
  fromDate?: string;
  institutionId?: string;
  isActive?: boolean;
}

const fetchEvents = async (
  appliedFilters: EventFilterState | FetchEventsParams = {},
  pageNumber: number = 1,
  pageSize: number = 6,
): Promise<PagedResult<EventItem>> => {
  const params: Record<string, string | number | boolean> = {
    pageNumber: (appliedFilters as FetchEventsParams)?.pageNumber ?? pageNumber,
    pageSize: (appliedFilters as FetchEventsParams)?.pageSize ?? pageSize,
  };
  if (appliedFilters.fromDate) {
    params.fromDate = new Date(
      `${appliedFilters.fromDate}T00:00:00-03:00`,
    ).toISOString();
  }
  if (appliedFilters.institutionId) {
    params.institutionId = appliedFilters.institutionId;
  }
  if (appliedFilters.isActive !== undefined) {
    params.isActive = appliedFilters.isActive;
  }
  const res = await api.get<PagedResult<EventItem>>("/events", {
    params,
  });
  return res.data;
};

const toggleEventActive = async (
  eventId: string,
  isActive: boolean,
): Promise<void> => {
  await api.patch(`/events/${eventId}/active`, {
    isActive,
  });
};

export interface EventItem {
  id: string;
  name: string;
  description: string;
  institutionId: string;
  institutionName: string;
  startDate: string;
  endDate: string;
  capacity: number;
  confirmedRegistrations: number;
  availableSeats: number;
  isFull: boolean;
  eventType: number;
  allowDocuments: boolean;
  allowedCourseNames: string[];
  allowedCourseIds: string[];
  isActive: boolean;
  isAddedToCalendar: boolean;
  allowsAddToCalendar: boolean;
  isUserRegistered?: boolean;
  isUserAttended?: boolean;
}

export interface EventFilterState {
  institutionId?: string;
  fromDate?: string;
  isActive?: boolean;
}

type StatusFilterValue = "all" | "active" | "inactive";

export interface EventsListProps {
  data?: PagedResult<EventItem>;
  institutions: OptionItem[];
  currentFilters?: EventFilterState;
  userInstitutionId: string | null;
  onFilter?: (filters: EventFilterState) => void;
  onPageChange?: (page: number) => void;
  handleStartEdition?: (eventData: EventItem) => void;
  handleToggleActive?: (eventData: EventItem) => void;
  studentMode?: boolean;
  onJoinEvent?: (eventId: string) => Promise<void>;
  onLeaveEvent?: (eventId: string) => Promise<void>;
  pageSize?: number;
}

export const EventsList: React.FC<EventsListProps> = ({
  institutions,
  currentFilters,
  userInstitutionId,
  onFilter,
  onPageChange,
  handleStartEdition,
  handleToggleActive: externalHandleToggleActive,
  studentMode = false,
  onJoinEvent,
  onLeaveEvent,
  pageSize = 6,
}) => {
  const { user } = useAuth();
  const allInstitutionsName = "Todas as instituições";

  const [currentPage, setCurrentPage] = useState(1);
  const [filters, setFilters] = useState<EventFilterState>(
    currentFilters ?? {
      fromDate: new Date().toISOString().split("T")[0],
    },
  );

  const [data, setData] = useState<PagedResult<EventItem>>({
    items: [],
    totalItems: 0,
    pageNumber: 1,
    pageSize,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [fromDate, setFromDate] = useState<string>(
    filters.fromDate || new Date().toISOString().split("T")[0],
  );
  const [institutionId, setInstitutionId] = useState<string>(
    userInstitutionId || filters.institutionId || allInstitutionsName,
  );
  const [statusFilter, setStatusFilter] = useState<StatusFilterValue>(
    filters.isActive === undefined
      ? "all"
      : filters.isActive
        ? "active"
        : "inactive",
  );
  const [calendarOverrides, setCalendarOverrides] = useState<
    Record<string, boolean>
  >({});
  const [registrationOverrides, setRegistrationOverrides] = useState<
    Record<string, boolean>
  >({});
  const [loadingActionId, setLoadingActionId] = useState<string | null>(null);
  const [documentModalEvent, setDocumentModalEvent] =
    useState<EventItem | null>(null);
  const [isDocumentModalOpen, setIsDocumentModalOpen] = useState(false);
  const [attendanceModalEvent, setAttendanceModalEvent] =
    useState<EventItem | null>(null);
  const [isAttendanceModalOpen, setIsAttendanceModalOpen] = useState(false);

  const loadEvents = useCallback(
    async (appliedFilters = filters, page = currentPage) => {
      setIsLoading(true);
      try {
        const res = await fetchEvents(appliedFilters, page, pageSize);
        setData(res);
      } catch (err) {
        console.error("Erro ao buscar eventos", err);
      } finally {
        setIsLoading(false);
      }
    },
    [filters, currentPage, pageSize],
  );

  useEffect(() => {
    let isMounted = true;
    void fetchEvents(filters, currentPage, pageSize)
      .then((res) => {
        if (isMounted) {
          setData(res);
        }
      })
      .catch((err) => {
        if (isMounted) {
          console.error("Erro ao buscar eventos", err);
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [filters, currentPage, pageSize]);

  const handleFilterChange = (newFilters: EventFilterState) => {
    setIsLoading(true);
    setCurrentPage(1);
    setFilters(newFilters);
    onFilter?.(newFilters);
  };

  const handlePageChange = (page: number) => {
    setIsLoading(true);
    setCurrentPage(page);
    onPageChange?.(page);
  };

  const handleToggleActive = async (eventItem: EventItem) => {
    if (externalHandleToggleActive) {
      externalHandleToggleActive(eventItem);
      return;
    }
    try {
      await toggleEventActive(eventItem.id, !eventItem.isActive);
      setCurrentPage(1);
      await loadEvents(filters, 1);
    } catch (err) {
      console.error("Erro ao alterar status do evento", err);
    }
  };

  const handleJoin = async (eventId: string) => {
    setLoadingActionId(eventId);
    try {
      if (!onJoinEvent) return;
      await onJoinEvent(eventId);
      setRegistrationOverrides((current) => ({
        ...current,
        [eventId]: true,
      }));
    } finally {
      setLoadingActionId(null);
    }
  };

  const handleLeave = async (eventId: string) => {
    setLoadingActionId(eventId);
    try {
      if (!onLeaveEvent) return;
      await onLeaveEvent(eventId);
      setRegistrationOverrides((current) => ({
        ...current,
        [eventId]: false,
      }));
    } finally {
      setLoadingActionId(null);
    }
  };

  const handleOpenDocumentModal = (eventItem: EventItem) => {
    setDocumentModalEvent(eventItem);
    setIsDocumentModalOpen(true);
  };

  const handleOpenAttendanceModal = (eventItem: EventItem) => {
    setAttendanceModalEvent(eventItem);
    setIsAttendanceModalOpen(true);
  };

  const statusOptions: OptionItem[] = [
    { value: "all", label: "Todos" },
    { value: "active", label: "Ativos" },
    { value: "inactive", label: "Inativos" },
  ];

  const handleApplyFilter = () => {
    handleFilterChange({
      fromDate: fromDate || undefined,
      institutionId:
        institutionId !== allInstitutionsName
          ? institutionId
          : (userInstitutionId ?? undefined),
      isActive: statusFilter === "all" ? undefined : statusFilter === "active",
    });
  };

  return (
    <div className="space-y-6">
      <Card className="surface-card bg-white shadow-none">
        <CardContent className="p-4">
          <div className="grid grid-cols-1 sm:grid-cols-8 gap-2 items-end">
            <div className="space-y-2 col-span-2">
              <Label htmlFor="filter-from-date">A partir de</Label>
              <Input
                id="filter-from-date"
                type="date"
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
                className="w-full h-10"
              />
            </div>
            {!userInstitutionId && (
              <div className={"col-span-2"}>
                <Label htmlFor="filter-institution" className={"mb-2 "}>
                  Instituição
                </Label>
                <Select
                  id="filter-institution"
                  value={institutionId}
                  items={institutions}
                  onValueChange={(value) =>
                    setInstitutionId(value ?? allInstitutionsName)
                  }>
                  <SelectTrigger className={"w-full !h-10"}>
                    <SelectValue placeholder={allInstitutionsName} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectGroup>
                      <SelectItem value={allInstitutionsName}>
                        Todas as instituições
                      </SelectItem>
                      {institutions.map((inst) => (
                        <SelectItem key={inst.value} value={inst.value}>
                          {inst.label}
                        </SelectItem>
                      ))}
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </div>
            )}
            {user?.role !== "Aluno" && (
              <div className="col-span-2">
                <Label htmlFor="filter-status" className={"mb-2"}>
                  Status
                </Label>
                <Select
                  id="filter-status"
                  value={statusFilter}
                  items={statusOptions}
                  onValueChange={(value) =>
                    setStatusFilter((value as StatusFilterValue) ?? "all")
                  }>
                  <SelectTrigger className={"w-full !h-10"}>
                    <SelectValue placeholder="Todos" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectGroup>
                      {statusOptions.map((status) => (
                        <SelectItem key={status.value} value={status.value}>
                          {status.label}
                        </SelectItem>
                      ))}
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="flex gap-2 sm:col-span-2">
              <Button
                onClick={handleApplyFilter}
                disabled={isLoading}
                className="primary-action w-full">
                Filtrar
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {isLoading ? (
        <div className="flex justify-center items-center py-16">
          <Loader2 className="h-8 w-8 animate-spin text-[#12a7d4]" />
        </div>
      ) : data.items.length === 0 ? (
        <Card className="text-center py-12">
          <CardContent>
            <p className="text-slate-500">
              Nenhum evento encontrado para os filtros selecionados.
            </p>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {data.items.map((event) => {
              const availableSeats = event.availableSeats;
              const isFull = event.isFull;
              const isUpcoming = event.startDate > new Date().toISOString();
              const isOngoing =
                !isUpcoming && event.endDate > new Date().toISOString();
              const isAddedToCalendar =
                calendarOverrides[event.id] ?? event.isAddedToCalendar;
              const isRegistered =
                registrationOverrides[event.id] ?? event.isUserRegistered ?? false;

              return (
                <Card
                  key={event.id}
                  className="flex flex-col border-[#e1e4ea] shadow-none transition hover:-translate-y-0.5 hover:shadow-md">
                  <CardHeader>
                    <div className="flex justify-between items-start gap-2 mb-2 flex-wrap">
                      <Badge variant={isFull ? "destructive" : "secondary"}>
                        {isFull
                          ? "Esgotado"
                          : `${availableSeats} vagas restantes`}
                      </Badge>
                      {event.allowDocuments && (
                        <Badge
                          variant="outline"
                          className="text-xs bg-green-100 text-green-800">
                          Permite envio de documentos
                        </Badge>
                      )}
                      {!event.isActive && (
                        <Badge
                          variant="outline"
                          className="text-xs bg-slate-100 text-slate-600">
                          Inativo
                        </Badge>
                      )}
                    </div>
                    <CardTitle className="text-xl line-clamp-1">
                      {event.name}
                    </CardTitle>
                    <CardDescription className="line-clamp-2">
                      {event.description}
                    </CardDescription>
                  </CardHeader>

                  <CardContent className="space-y-3 text-sm text-slate-600">
                    <div className="flex items-center gap-2">
                      <MapPin className="h-4 w-4 text-[#12a7d4] shrink-0" />
                      <span className="truncate">{event.institutionName}</span>
                    </div>

                    <div className="flex items-center gap-2">
                      <Calendar className="h-4 w-4 text-[#12a7d4] shrink-0" />
                      <span>Início: {formatDateForShort(event.startDate)}</span>
                    </div>

                    <div className="flex items-center gap-2">
                      <Clock className="h-4 w-4 text-[#12a7d4] shrink-0" />
                      <span>Término: {formatDateForShort(event.endDate)}</span>
                    </div>

                    <div className="flex items-center gap-2">
                      <Users className="h-4 w-4 text-[#12a7d4] shrink-0" />
                      <span>
                        Inscritos: {event.confirmedRegistrations} /{" "}
                        {event.capacity}
                      </span>
                    </div>

                    {event.allowedCourseNames.length > 0 && (
                      <div>
                        <div>Cursos permitidos:</div>
                        <div className="pt-2 flex flex-wrap gap-1">
                          {event.allowedCourseNames
                            .slice(0, 3)
                            .map((course, idx) => (
                              <span
                                key={idx}
                                className="bg-slate-100 text-slate-700 px-2 py-0.5 rounded text-xs">
                                {course}
                              </span>
                            ))}
                          {event.allowedCourseNames.length > 3 && (
                            <span className="text-xs text-slate-400 self-center">
                              +{event.allowedCourseNames.length - 3}
                            </span>
                          )}
                        </div>
                      </div>
                    )}
                    {event.isActive ? (
                      studentMode ? (
                        <div className="pt-4 space-y-3">
                          {isRegistered && event.allowsAddToCalendar && (
                            <div className="flex justify-end">
                              {isAddedToCalendar ? (
                                <RemoveFromCalendarButton
                                  eventId={event.id}
                                  onRemoved={() =>
                                    setCalendarOverrides((current) => ({
                                      ...current,
                                      [event.id]: false,
                                    }))
                                  }
                                />
                              ) : (
                                <AddToCalendarButton
                                  eventId={event.id}
                                  onAdded={() =>
                                    setCalendarOverrides((current) => ({
                                      ...current,
                                      [event.id]: true,
                                    }))
                                  }
                                />
                              )}
                            </div>
                          )}

                          {isRegistered &&
                            (isOngoing || event.isUserAttended) && (
                              <AttendanceConfirm
                                eventId={event.id}
                                attended={event.isUserAttended ?? false}
                              />
                            )}

                          <div className="flex flex-wrap items-start justify-end gap-2">
                            {event.allowDocuments &&
                              isUpcoming &&
                              isRegistered && (
                                <Button
                                  variant="outline"
                                  size="sm"
                                  className="gap-2 cursor-pointer"
                                  onClick={() =>
                                    handleOpenDocumentModal(event)
                                  }>
                                  <FileUp className="h-4 w-4 text-[#109bc6]" />
                                  Gerenciar documentos
                                </Button>
                              )}

                            {isRegistered ? (
                              <Button
                                variant="outline"
                                size="sm"
                                disabled={loadingActionId === event.id}
                                className="bg-red-50 text-red-600 hover:bg-red-100 hover:text-red-700 border-red-200 cursor-pointer"
                                onClick={() => handleLeave(event.id)}>
                                {loadingActionId === event.id ? (
                                  <Loader2 className="h-4 w-4 animate-spin mr-1" />
                                ) : (
                                  <LogOut className="h-4 w-4 mr-1" />
                                )}
                                Sair do evento
                              </Button>
                            ) : (
                              <div className="flex flex-col items-end gap-1">
                                <Button
                                  size="sm"
                                  disabled={
                                    isFull ||
                                    !isUpcoming ||
                                    loadingActionId === event.id
                                  }
                                  className="primary-action cursor-pointer"
                                  onClick={() => handleJoin(event.id)}>
                                  {loadingActionId === event.id ? (
                                    <Loader2 className="h-4 w-4 animate-spin mr-1" />
                                  ) : (
                                    <LogIn className="h-4 w-4 mr-1" />
                                  )}
                                  {isFull ? "Esgotado" : "Entrar no evento"}
                                </Button>
                                {isFull && (
                                  <p className="text-right text-xs text-red-600">
                                    Inscritos máximos atingidos.
                                  </p>
                                )}
                              </div>
                            )}
                          </div>
                        </div>
                      ) : (
                        isUpcoming ? (
                          <div className="flex justify-end items-center gap-2 pt-4 flex-wrap">
                            {event.allowsAddToCalendar &&
                              (isAddedToCalendar ? (
                                <RemoveFromCalendarButton
                                  eventId={event.id}
                                  onRemoved={() =>
                                    setCalendarOverrides((current) => ({
                                      ...current,
                                      [event.id]: false,
                                    }))
                                  }
                                />
                              ) : (
                                <AddToCalendarButton
                                  eventId={event.id}
                                  onAdded={() =>
                                    setCalendarOverrides((current) => ({
                                      ...current,
                                      [event.id]: true,
                                    }))
                                  }
                                />
                              ))}

                            <Button
                              className={"secondary-action cursor-pointer"}
                              onClick={() => handleStartEdition?.(event)}>
                              Detalhes do evento
                            </Button>

                            <DeleteConfirmation
                              children={
                                <Button
                                  className={
                                    "bg-red-50 text-red-600 hover:bg-red-100 cursor-pointer"
                                  }>
                                  Inativar
                                </Button>
                              }
                              onDelete={() => handleToggleActive(event)}
                              descriptionText={
                                "Tem certeza que deseja inativar este evento?"
                              }
                              confirmationText={
                                "Inativar"
                              }></DeleteConfirmation>
                          </div>
                        ) : (
                          <div className="flex justify-end pt-4">
                            <Button
                              variant="outline"
                              size="sm"
                              className="gap-2 cursor-pointer"
                              onClick={() => handleOpenAttendanceModal(event)}>
                              <ClipboardCheck className="h-4 w-4 text-[#109bc6]" />
                              Presença
                            </Button>
                          </div>
                        )
                      )
                    ) : (
                      /* Quando !event.isActive: botão Reativar não aparece se for studentMode */
                      !studentMode && (
                        <div className="flex justify-end pt-4">
                          <Button
                            className="bg-emerald-50 text-emerald-700 hover:bg-emerald-100 cursor-pointer"
                            onClick={() => handleStartEdition?.(event)}>
                            Reativar
                          </Button>
                        </div>
                      )
                    )}
                  </CardContent>
                </Card>
              );
            })}
          </div>

          {data.totalPages > 1 && (
            <div className="flex justify-center pt-4">
              <Pagination>
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious
                      onClick={() => handlePageChange(data.pageNumber - 1)}
                      className={
                        !data.hasPreviousPage
                          ? "pointer-events-none opacity-50"
                          : "cursor-pointer"
                      }
                    />
                  </PaginationItem>

                  {Array.from({ length: data.totalPages }, (_, index) => {
                    const pageNum = index + 1;
                    return (
                      <PaginationItem key={pageNum}>
                        <PaginationLink
                          isActive={pageNum === data.pageNumber}
                          onClick={() => handlePageChange(pageNum)}
                          className="cursor-pointer">
                          {pageNum}
                        </PaginationLink>
                      </PaginationItem>
                    );
                  })}

                  <PaginationItem>
                    <PaginationNext
                      onClick={() => handlePageChange(data.pageNumber + 1)}
                      className={
                        !data.hasNextPage
                          ? "pointer-events-none opacity-50"
                          : "cursor-pointer"
                      }
                    />
                  </PaginationItem>
                </PaginationContent>
              </Pagination>
            </div>
          )}
        </>
      )}

      <SubmitDocumentModal
        isOpen={isDocumentModalOpen}
        onOpenChange={setIsDocumentModalOpen}
        event={documentModalEvent}
      />

      <AttendanceModal
        isOpen={isAttendanceModalOpen}
        onOpenChange={setIsAttendanceModalOpen}
        event={attendanceModalEvent}
      />
    </div>
  );
};

export { EventsList as EventList };