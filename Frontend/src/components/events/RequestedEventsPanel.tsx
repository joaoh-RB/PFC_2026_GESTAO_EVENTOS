import React, { useCallback, useEffect, useState } from "react";
import {
  AlertCircle,
  Calendar,
  Check,
  CheckCircle2,
  Clock,
  Loader2,
  MapPin,
  User as UserIcon,
  X,
} from "lucide-react";
import { api } from "@/services/api";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import type { PagedResult } from "@/types/utils";
import { formatDateForShort } from "@/utils/format";
import type { EventItem } from "./EventList";

export interface RequestedEventsPanelProps {
  userInstitutionId: string | null;
  onApprovalChange?: () => void;
}

export const RequestedEventsPanel: React.FC<RequestedEventsPanelProps> = ({
  userInstitutionId,
  onApprovalChange,
}) => {
  const [requestedEventsData, setRequestedEventsData] = useState<
    PagedResult<EventItem>
  >({
    items: [],
    totalItems: 0,
    pageNumber: 1,
    pageSize: 6,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [processingEventId, setProcessingEventId] = useState<string | null>(
    null,
  );
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const fetchRequestedEvents = useCallback(
    async (page: number = 1) => {
      setIsLoading(true);
      try {
        const res = await api.get<PagedResult<EventItem>>("/events/requested", {
          params: {
            fromDate: new Date().toISOString(),
            institutionId: userInstitutionId || undefined,
            pageNumber: page,
            pageSize: 6,
          },
        });
        setRequestedEventsData(res.data);
      } catch (err) {
        console.error("Erro ao buscar eventos solicitados", err);
        setError("Não foi possível carregar os eventos solicitados.");
      } finally {
        setIsLoading(false);
      }
    },
    [userInstitutionId],
  );

  useEffect(() => {
    void fetchRequestedEvents(1);
  }, [fetchRequestedEvents]);

  const handleSetApproval = async (eventId: string, status: 1 | 2) => {
    setProcessingEventId(eventId);
    setError(null);
    setSuccessMessage(null);

    try {
      await api.patch(`/events/${eventId}/approval`, { status });
      setSuccessMessage(
        status === 1
          ? "Evento aprovado e agendado com sucesso!"
          : "Evento reprovado com sucesso.",
      );
      await fetchRequestedEvents(1);
      onApprovalChange?.();
      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { message?: string } } };
      setError(
        errorObj.response?.data?.message ||
          "Não foi possível processar a aprovação do evento.",
      );
    } finally {
      setProcessingEventId(null);
    }
  };

  const handlePageChange = async (page: number) => {
    if (page < 1 || page > requestedEventsData.totalPages) return;
    await fetchRequestedEvents(page);
  };

  return (
    <div className="space-y-4">
      {error && (
        <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {successMessage && (
        <div className="flex items-center gap-2 rounded-lg border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-700">
          <CheckCircle2 className="h-4 w-4 shrink-0" />
          <span>{successMessage}</span>
        </div>
      )}

      {isLoading ? (
        <div className="flex justify-center items-center py-16">
          <Loader2 className="h-8 w-8 animate-spin text-[#12a7d4]" />
        </div>
      ) : requestedEventsData.items.length === 0 ? (
        <Card className="surface-card bg-white text-center py-12 shadow-none">
          <CardContent>
            <p className="text-slate-500">
              Nenhum evento solicitado aguardando aprovação.
            </p>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {requestedEventsData.items.map((event) => {
              const isProcessing = processingEventId === event.id;
              const requesterDisplay = event.createdByUserName
                ? event.createdByUniqueIdentifier
                  ? `${event.createdByUserName} - ${event.createdByUniqueIdentifier}`
                  : event.createdByUserName
                : "Não informado";

              return (
                <Card
                  key={event.id}
                  className="flex flex-col border-[#e1e4ea] bg-white shadow-none transition hover:-translate-y-0.5 hover:shadow-md">
                  <CardHeader>
                    <div className="flex justify-between items-start gap-2 mb-2 flex-wrap">
                      <Badge
                        variant="outline"
                        className="bg-amber-50 text-amber-800 border-amber-200">
                        Solicitado
                      </Badge>
                      {event.allowDocuments && (
                        <Badge
                          variant="outline"
                          className="text-xs bg-green-100 text-green-800">
                          Permite envio de documentos
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

                  <CardContent className="flex flex-1 flex-col justify-between space-y-3 text-sm text-slate-600">
                    <div className="space-y-3">
                      <div className="flex items-center gap-2">
                        <UserIcon className="h-4 w-4 text-[#12a7d4] shrink-0" />
                        <span className="truncate font-medium text-slate-800">
                          Solicitante: {requesterDisplay}
                        </span>
                      </div>

                      <div className="flex items-center gap-2">
                        <MapPin className="h-4 w-4 text-[#12a7d4] shrink-0" />
                        <span className="truncate">
                          {event.institutionName}
                        </span>
                      </div>

                      <div className="flex items-center gap-2">
                        <Calendar className="h-4 w-4 text-[#12a7d4] shrink-0" />
                        <span>
                          Início: {formatDateForShort(event.startDate)}
                        </span>
                      </div>

                      <div className="flex items-center gap-2">
                        <Clock className="h-4 w-4 text-[#12a7d4] shrink-0" />
                        <span>
                          Término: {formatDateForShort(event.endDate)}
                        </span>
                      </div>

                      {event.allowedCourseNames.length > 0 && (
                        <div>
                          <div>Cursos permitidos:</div>
                          <div className="pt-2 flex flex-wrap gap-1">
                            {event.allowedCourseNames.map((course, idx) => (
                              <span
                                key={idx}
                                className="bg-slate-100 text-slate-700 px-2 py-0.5 rounded text-xs">
                                {course}
                              </span>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>

                    <div className="flex justify-end items-center gap-2 pt-4">
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={isProcessing}
                        onClick={() => void handleSetApproval(event.id, 2)}
                        className="bg-red-50 text-red-600 hover:bg-red-100 hover:text-red-700 border-red-200 cursor-pointer">
                        {isProcessing ? (
                          <Loader2 className="h-4 w-4 animate-spin mr-1" />
                        ) : (
                          <X className="h-4 w-4 mr-1" />
                        )}
                        Reprovar
                      </Button>

                      <Button
                        size="sm"
                        disabled={isProcessing}
                        onClick={() => void handleSetApproval(event.id, 1)}
                        className="bg-emerald-600 text-white hover:bg-emerald-700 cursor-pointer">
                        {isProcessing ? (
                          <Loader2 className="h-4 w-4 animate-spin mr-1" />
                        ) : (
                          <Check className="h-4 w-4 mr-1" />
                        )}
                        Aprovar
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>

          {requestedEventsData.totalPages > 1 && (
            <div className="flex justify-center pt-4">
              <Pagination>
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious
                      onClick={() =>
                        void handlePageChange(
                          requestedEventsData.pageNumber - 1,
                        )
                      }
                      aria-disabled={!requestedEventsData.hasPreviousPage}
                      className={
                        !requestedEventsData.hasPreviousPage
                          ? "pointer-events-none opacity-50"
                          : "cursor-pointer"
                      }
                    />
                  </PaginationItem>

                  {Array.from(
                    { length: requestedEventsData.totalPages },
                    (_, i) => i + 1,
                  ).map((page) => (
                    <PaginationItem key={page}>
                      <PaginationLink
                        onClick={() => void handlePageChange(page)}
                        isActive={page === requestedEventsData.pageNumber}
                        className="cursor-pointer">
                        {page}
                      </PaginationLink>
                    </PaginationItem>
                  ))}

                  <PaginationItem>
                    <PaginationNext
                      onClick={() =>
                        void handlePageChange(
                          requestedEventsData.pageNumber + 1,
                        )
                      }
                      aria-disabled={!requestedEventsData.hasNextPage}
                      className={
                        !requestedEventsData.hasNextPage
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
    </div>
  );
};

