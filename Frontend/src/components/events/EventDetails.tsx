import React, { useCallback, useEffect, useState } from "react";
import { isAxiosError } from "axios";
import { Loader2, Download } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Button } from "@/components/ui/button";
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { api } from "@/services/api";
import type { PagedResult } from "@/types/utils";
import { GroupMemberStatus } from "@/types/group";
import { formatDateForShort } from "@/utils/format";
import type { EventFeedbackDto } from "./EventFeedbackModal";
import type { EventItem } from "./EventList";
import { DeleteConfirmation } from "@/components/ui/deleteConfirm";

interface EventRegistrationItem {
  studentId: string;
  studentName: string;
  courseName: string;
  registeredAt: string;
}

interface GroupMemberItem {
  studentId: string;
  name: string;
  status: number;
  isLeader: boolean;
}

interface EventGroupItem {
  id: string;
  number: number;
  leaderName: string;
  members: GroupMemberItem[];
}

interface EventDocumentItem {
  id: string;
  groupId: string;
  groupNumber?: number | null;
  senderId: string;
  senderName: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  submittedAt: string;
  status: number;
  feedback?: string | null;
  reviewedById?: string | null;
  reviewedByName?: string | null;
  reviewedAt?: string | null;
}

type DetailsTab = "registrations" | "groups";

const REGISTRATIONS_PAGE_SIZE = 10;

const groupMemberStatusLabel: Record<number, string> = {
  [GroupMemberStatus.Invited]: "Convidado",
  [GroupMemberStatus.Accepted]: "Aceito",
  [GroupMemberStatus.Requested]: "Pedido de entrada pendente",
};

const getApiMessage = (error: unknown, fallback: string): string => {
  const responseData = isAxiosError(error)
    ? (error.response?.data as { message?: string } | undefined)
    : undefined;

  return typeof responseData?.message === "string"
    ? responseData.message
    : fallback;
};

interface EventDetailsProps {
  event: EventItem;
}

export const EventDetails: React.FC<EventDetailsProps> = ({ event }) => {
  const [activeTab, setActiveTab] = useState<DetailsTab>("registrations");
  const [isRegistrationsLoading, setIsRegistrationsLoading] = useState(false);
  const [registrationsError, setRegistrationsError] = useState<string | null>(
    null,
  );
  const [registrationsData, setRegistrationsData] = useState<
    PagedResult<EventRegistrationItem>
  >({
    items: [],
    totalItems: 0,
    pageNumber: 1,
    pageSize: REGISTRATIONS_PAGE_SIZE,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  });
  const [isGroupsLoading, setIsGroupsLoading] = useState(false);
  const [groupsError, setGroupsError] = useState<string | null>(null);
  const [groupsData, setGroupsData] = useState<EventGroupItem[]>([]);
  const [isDocumentsLoading, setIsDocumentsLoading] = useState(false);
  const [documentsError, setDocumentsError] = useState<string | null>(null);
  const [pendingDocuments, setPendingDocuments] = useState<EventDocumentItem[]>(
    [],
  );
  const [approvedDocuments, setApprovedDocuments] = useState<
    EventDocumentItem[]
  >([]);
  const [feedbackByDocument, setFeedbackByDocument] = useState<
    Record<string, string>
  >({});
  const [reviewingDocumentId, setReviewingDocumentId] = useState<string | null>(
    null,
  );
  const [downloadingDocumentId, setDownloadingDocumentId] = useState<
    string | null
  >(null);
  const [isDownloadingCompiledZip, setIsDownloadingCompiledZip] =
    useState(false);
  const [isFeedbacksLoading, setIsFeedbacksLoading] = useState(false);
  const [feedbacksError, setFeedbacksError] = useState<string | null>(null);
  const [feedbacks, setFeedbacks] = useState<EventFeedbackDto[]>([]);

  const loadRegistrations = useCallback(
    async (page: number = 1) => {
      setIsRegistrationsLoading(true);
      setRegistrationsError(null);

      try {
        const response = await api.get<PagedResult<EventRegistrationItem>>(
          `/events/${event.id}/registrations`,
          {
            params: {
              pageNumber: page,
              pageSize: REGISTRATIONS_PAGE_SIZE,
            },
          },
        );

        setRegistrationsData(response.data);
      } catch (error: unknown) {
        setRegistrationsError(
          getApiMessage(
            error,
            "Não foi possível carregar os inscritos deste evento.",
          ),
        );
      } finally {
        setIsRegistrationsLoading(false);
      }
    },
    [event.id],
  );

  const loadFeedbacks = useCallback(async () => {
    setIsFeedbacksLoading(true);
    setFeedbacksError(null);

    try {
      const response = await api.get<EventFeedbackDto[]>(
        `/events/${event.id}/feedbacks`,
      );
      setFeedbacks(Array.isArray(response.data) ? response.data : []);
    } catch (error: unknown) {
      setFeedbacksError(
        getApiMessage(
          error,
          "Não foi possível carregar os feedbacks deste evento.",
        ),
      );
    } finally {
      setIsFeedbacksLoading(false);
    }
  }, [event.id]);

  const loadGroups = useCallback(async () => {
    setIsGroupsLoading(true);
    setGroupsError(null);

    try {
      const response = await api.get<EventGroupItem[]>(
        `/groups/event/${event.id}`,
      );

      setGroupsData(response.data ?? []);
    } catch (error: unknown) {
      setGroupsError(
        getApiMessage(error, "Não foi possível carregar os grupos do evento."),
      );
    } finally {
      setIsGroupsLoading(false);
    }
  }, [event.id]);

  const loadDocuments = useCallback(async () => {
    setIsDocumentsLoading(true);
    setDocumentsError(null);

    try {
      const pendingRes = await api.get<EventDocumentItem[]>(
        `/eventdocuments/${event.id}/pending-approvals`,
      );

      let reviewedItems: EventDocumentItem[] = [];
      try {
        const reviewedRes = await api.get<EventDocumentItem[]>(
          `/eventdocuments/${event.id}/reviewed`,
        );
        reviewedItems = reviewedRes.data ?? [];
      } catch (reviewedError: unknown) {
        if (
          isAxiosError(reviewedError) &&
          reviewedError.response?.status === 404
        ) {
          const approvedRes = await api.get<EventDocumentItem[]>(
            `/eventdocuments/${event.id}/approved`,
          );
          reviewedItems = approvedRes.data ?? [];
        } else {
          throw reviewedError;
        }
      }

      setPendingDocuments(pendingRes.data ?? []);
      setApprovedDocuments(reviewedItems);
    } catch (error: unknown) {
      setDocumentsError(
        getApiMessage(
          error,
          "Não foi possível carregar os documentos do evento.",
        ),
      );
    } finally {
      setIsDocumentsLoading(false);
    }
  }, [event.id]);

  useEffect(() => {
    void loadRegistrations(1);
    void loadFeedbacks();
  }, [loadRegistrations, loadFeedbacks]);

  const handleReviewDocument = async (documentId: string, approve: boolean) => {
    setReviewingDocumentId(documentId);
    setDocumentsError(null);

    try {
      await api.post(`/eventdocuments/${documentId}/review`, {
        approve,
        feedback: feedbackByDocument[documentId] || null,
      });

      setFeedbackByDocument((current) => {
        const updated = { ...current };
        delete updated[documentId];
        return updated;
      });

      await loadDocuments();
    } catch (error: unknown) {
      setDocumentsError(
        getApiMessage(error, "Não foi possível avaliar o documento."),
      );
    } finally {
      setReviewingDocumentId(null);
    }
  };

  const handleDownloadDocument = async (doc: EventDocumentItem) => {
    setDownloadingDocumentId(doc.id);

    try {
      const response = await api.get(`/eventdocuments/${doc.id}/download`, {
        responseType: "blob",
      });

      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", doc.originalFileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (error: unknown) {
      setDocumentsError(
        getApiMessage(error, "Não foi possível baixar o documento."),
      );
    } finally {
      setDownloadingDocumentId(null);
    }
  };

  const handleDownloadCompiledZip = async () => {
    setIsDownloadingCompiledZip(true);
    setDocumentsError(null);

    try {
      const response = await api.get(
        `/eventdocuments/${event.id}/compiled-zip`,
        {
          responseType: "blob",
        },
      );
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute(
        "download",
        `documentos-aprovados-${event.id}.zip`,
      );
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (error: unknown) {
      let message = "Não foi possível baixar o compilado de documentos.";
      if (isAxiosError(error) && error.response?.data instanceof Blob) {
        try {
          const text = await error.response.data.text();
          const parsed = JSON.parse(text) as { message?: string };
          if (parsed?.message) message = parsed.message;
        } catch {
          // Mantém fallback
        }
      } else {
        message = getApiMessage(error, message);
      }
      setDocumentsError(message);
    } finally {
      setIsDownloadingCompiledZip(false);
    }
  };

  const handleTabChange = async (tab: DetailsTab) => {
    setActiveTab(tab);

    if (tab === "registrations") {
      await Promise.all([loadRegistrations(1), loadFeedbacks()]);
      return;
    }

    await Promise.all([loadGroups(), loadDocuments()]);
  };

  const handleRegistrationsPageChange = async (page: number) => {
    if (page < 1 || page > registrationsData.totalPages) return;
    await loadRegistrations(page);
  };

  const averageRating =
    feedbacks.length > 0
      ? (
          feedbacks.reduce((sum, item) => sum + item.rating, 0) /
          feedbacks.length
        ).toFixed(1)
      : null;

  const isEventEnded = new Date(event.endDate).getTime() <= Date.now();

  return (
    <Card className="surface-card max-w-4xl mx-auto shadow-none">
      <CardHeader className="pb-4 pt-6">
        <CardTitle className="text-2xl font-bold text-slate-900">
          {event.name}
        </CardTitle>
        <p className="text-sm text-slate-600">
          {event.institutionName} · {formatDateForShort(event.startDate)} até{" "}
          {formatDateForShort(event.endDate)}
        </p>
      </CardHeader>

      <CardContent>
        {event.allowDocuments && (
          <div className="mb-6 rounded-xl border border-slate-200 bg-slate-50 p-1">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-1">
              <button
                type="button"
                className={`rounded-lg px-4 py-2 text-sm font-semibold transition ${
                  activeTab === "registrations"
                    ? "bg-white text-slate-900 shadow-sm"
                    : "text-slate-500 hover:text-slate-700"
                }`}
                onClick={() => void handleTabChange("registrations")}>
                Inscritos
              </button>
              <button
                type="button"
                className={`rounded-lg px-4 py-2 text-sm font-semibold transition ${
                  activeTab === "groups"
                    ? "bg-white text-slate-900 shadow-sm"
                    : "text-slate-500 hover:text-slate-700"
                }`}
                onClick={() => void handleTabChange("groups")}>
                Grupos
              </button>
            </div>
          </div>
        )}

        {activeTab === "registrations" || !event.allowDocuments ? (
          <div className="space-y-8">
            <div className="space-y-4">
              <div>
                <h3 className="text-lg font-semibold text-slate-900">
                  Inscritos no evento
                </h3>
                <p className="text-sm text-slate-500">
                  {event.name}
                </p>
              </div>

              {isRegistrationsLoading ? (
                <div className="py-8 text-center text-slate-500">
                  Carregando inscritos...
                </div>
              ) : registrationsError ? (
                <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                  {registrationsError}
                </div>
              ) : registrationsData.items.length === 0 ? (
                <div className="rounded-lg border border-dashed border-slate-200 p-6 text-center text-slate-500">
                  Ainda não há inscritos confirmados neste evento.
                </div>
              ) : (
                <>
                  <div className="overflow-hidden rounded-xl border border-slate-200">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>Aluno</TableHead>
                          <TableHead>Curso</TableHead>
                          <TableHead>Data e hora da inscrição</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {registrationsData.items.map((item) => (
                          <TableRow key={item.studentId}>
                            <TableCell className="font-medium text-slate-900">
                              {item.studentName}
                            </TableCell>
                            <TableCell>{item.courseName}</TableCell>
                            <TableCell>
                              {formatDateForShort(item.registeredAt)}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>

                  {registrationsData.totalPages > 1 && (
                    <div className="flex justify-center pt-2">
                      <Pagination>
                        <PaginationContent>
                          <PaginationItem>
                            <PaginationPrevious
                              href="#"
                              onClick={(e) => {
                                e.preventDefault();
                                void handleRegistrationsPageChange(
                                  registrationsData.pageNumber - 1,
                                );
                              }}
                              className={
                                !registrationsData.hasPreviousPage
                                  ? "pointer-events-none opacity-50"
                                  : "cursor-pointer"
                              }
                            />
                          </PaginationItem>

                          {Array.from(
                            { length: registrationsData.totalPages },
                            (_, index) => index + 1,
                          ).map((page) => (
                            <PaginationItem key={page}>
                              <PaginationLink
                                href="#"
                                isActive={page === registrationsData.pageNumber}
                                onClick={(e) => {
                                  e.preventDefault();
                                  void handleRegistrationsPageChange(page);
                                }}>
                                {page}
                              </PaginationLink>
                            </PaginationItem>
                          ))}

                          <PaginationItem>
                            <PaginationNext
                              href="#"
                              onClick={(e) => {
                                e.preventDefault();
                                void handleRegistrationsPageChange(
                                  registrationsData.pageNumber + 1,
                                );
                              }}
                              className={
                                !registrationsData.hasNextPage
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

            <div className="space-y-4 border-t border-slate-100 pt-6">
              <div className="flex items-center justify-between gap-2 flex-wrap">
                <div>
                  <h3 className="text-lg font-semibold text-slate-900">
                    Feedbacks do evento
                  </h3>
                  <p className="text-sm text-slate-500">
                    Avaliações enviadas pelos alunos que participaram do evento.
                  </p>
                </div>
                {averageRating && (
                  <Badge variant="secondary" className="text-sm font-semibold">
                    Média: {averageRating} / 10 ({feedbacks.length})
                  </Badge>
                )}
              </div>

              {isFeedbacksLoading ? (
                <div className="py-8 text-center text-slate-500">
                  Carregando feedbacks...
                </div>
              ) : feedbacksError ? (
                <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                  {feedbacksError}
                </div>
              ) : feedbacks.length === 0 ? (
                <div className="rounded-lg border border-dashed border-slate-200 p-6 text-center text-slate-500">
                  Ainda não há feedbacks registrados para este evento.
                </div>
              ) : (
                <div className="overflow-hidden rounded-xl border border-slate-200">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Aluno</TableHead>
                        <TableHead>Curso</TableHead>
                        <TableHead>Nota</TableHead>
                        <TableHead>Comentário</TableHead>
                        <TableHead>Data</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {feedbacks.map((item) => (
                        <TableRow key={item.id}>
                          <TableCell className="font-medium text-slate-900">
                            {item.studentName}
                          </TableCell>
                          <TableCell>{item.courseName}</TableCell>
                          <TableCell>
                            <Badge variant="secondary">
                              {item.rating} / 10
                            </Badge>
                          </TableCell>
                          <TableCell className="max-w-xs whitespace-normal break-words text-slate-700">
                            {item.feedbackText || (
                              <span className="italic text-slate-400">
                                Sem comentário
                              </span>
                            )}
                          </TableCell>
                          <TableCell>
                            {formatDateForShort(item.createdAt)}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="flex items-start justify-between gap-3 flex-wrap">
              <div>
                <h3 className="text-lg font-semibold text-slate-900">
                  Grupos e documentos
                </h3>
                <p className="text-sm text-slate-500">
                  Visualize os grupos com todos os participantes e
                  aprove/reprove os documentos do grupo.
                </p>
              </div>
              {event.allowDocuments && isEventEnded && approvedDocuments.some((doc) => Number(doc.status) === 1) && (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={isDownloadingCompiledZip}
                  onClick={() => void handleDownloadCompiledZip()}
                  className="gap-2 cursor-pointer">
                  {isDownloadingCompiledZip ? (
                    <Loader2 className="h-4 w-4 animate-spin text-[#109bc6]" />
                  ) : (
                    <Download className="h-4 w-4 text-[#109bc6]" />
                  )}
                  Baixar documentos (.zip)
                </Button>
              )}
            </div>

            {isGroupsLoading || isDocumentsLoading ? (
              <div className="py-8 text-center text-slate-500">
                Carregando grupos e documentos...
              </div>
            ) : groupsError || documentsError ? (
              <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                {groupsError || documentsError}
              </div>
            ) : groupsData.length === 0 ? (
              <div className="rounded-lg border border-dashed border-slate-200 p-6 text-center text-slate-500">
                Ainda não há grupos cadastrados neste evento.
              </div>
            ) : (
              <div className="space-y-4">
                {groupsData.map((group) => {
                  const groupPendingDocuments = pendingDocuments.filter(
                    (doc) => doc.groupId === group.id,
                  );
                  const groupReviewedDocuments = approvedDocuments.filter(
                    (doc) => doc.groupId === group.id,
                  );
                  const groupApprovedDocuments = groupReviewedDocuments.filter(
                    (doc) => Number(doc.status) === 1,
                  );
                  const groupRejectedDocuments = groupReviewedDocuments.filter(
                    (doc) => Number(doc.status) === 2,
                  );
                  const acceptedCount = group.members.filter(
                    (member) => member.status === GroupMemberStatus.Accepted,
                  ).length;
                  const maxSize = event.maxGroupSize ?? null;
                  const missingCount =
                    maxSize === null ? null : Math.max(maxSize - acceptedCount, 0);
                  const vacancyLabel =
                    maxSize === null
                      ? `${acceptedCount} ${acceptedCount === 1 ? "aluno" : "alunos"} · sem limite`
                      : missingCount === 0
                        ? `${acceptedCount}/${maxSize} alunos · grupo completo`
                        : `${acceptedCount}/${maxSize} alunos · faltam ${missingCount}`;

                  return (
                    <div
                      key={group.id}
                      className="rounded-xl border border-slate-200 p-4 space-y-4">
                      <div>
                        <div className="mb-2 flex items-center justify-between gap-2">
                          <p className="font-semibold text-slate-900">
                            Grupo {group.number}
                          </p>
                          <Badge
                            variant={missingCount === 0 ? "destructive" : "secondary"}>
                            {vacancyLabel}
                          </Badge>
                        </div>
                        <p className="mb-2 text-xs text-slate-500">
                          Líder: {group.leaderName}
                        </p>
                        <ul className="space-y-2">
                          {group.members
                            .filter(
                              (member) =>
                                member.status !== GroupMemberStatus.Declined,
                            )
                            .map((member) => (
                              <li
                                key={member.studentId}
                                className="flex items-center justify-between rounded-lg border border-slate-100 px-3 py-2 gap-3">
                                <div className="min-w-0">
                                  <p className="truncate text-sm font-medium text-slate-800">
                                    {member.name}
                                  </p>
                                  <p className="text-xs text-slate-500">
                                    {groupMemberStatusLabel[member.status] ??
                                      "Não informado"}
                                  </p>
                                </div>
                                {member.isLeader && (
                                  <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600 shrink-0">
                                    Líder
                                  </span>
                                )}
                              </li>
                            ))}
                        </ul>
                      </div>

                      <div className="space-y-2">
                        <div className="flex items-center justify-between">
                          <Label className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
                            Documentos pendentes do grupo
                          </Label>
                          <span className="text-xs text-slate-500">
                            {groupPendingDocuments.length} pendente(s)
                          </span>
                        </div>

                        {groupPendingDocuments.length === 0 ? (
                          <div className="rounded-lg border border-dashed border-slate-200 p-4 text-center text-sm text-slate-500">
                            Sem documentos pendentes neste grupo.
                          </div>
                        ) : (
                          <div className="space-y-3">
                            {groupPendingDocuments.map((doc) => (
                              <div
                                key={doc.id}
                                className="rounded-xl border border-slate-200 p-4 space-y-3 bg-white">
                                <div className="flex items-start justify-between gap-2">
                                  <div className="min-w-0">
                                    <p className="truncate text-sm font-semibold text-slate-900">
                                      {doc.originalFileName}
                                    </p>
                                    <p className="text-xs text-slate-500">
                                      Enviado por {doc.senderName}
                                    </p>
                                    <p className="text-xs text-slate-500">
                                      Em {formatDateForShort(doc.submittedAt)}
                                    </p>
                                  </div>
                                  <Button
                                    type="button"
                                    variant="outline"
                                    size="sm"
                                    className="gap-2 cursor-pointer shrink-0"
                                    disabled={downloadingDocumentId === doc.id}
                                    onClick={() =>
                                      void handleDownloadDocument(doc)
                                    }>
                                    {downloadingDocumentId === doc.id ? (
                                      <Loader2 className="h-4 w-4 animate-spin" />
                                    ) : (
                                      <Download className="h-4 w-4" />
                                    )}
                                    Baixar
                                  </Button>
                                </div>

                                <Textarea
                                  value={feedbackByDocument[doc.id] ?? ""}
                                  onChange={(e) =>
                                    setFeedbackByDocument((current) => ({
                                      ...current,
                                      [doc.id]: e.target.value,
                                    }))
                                  }
                                  placeholder="Feedback da avaliação (opcional)."
                                  className="min-h-[78px]"
                                />

                                <div className="flex items-center justify-end gap-2">
                                  <DeleteConfirmation
                                    descriptionText={`Deseja reprovar o documento "${doc.originalFileName}"?`}
                                    confirmationText="Confirmar reprovação"
                                    onDelete={() => {
                                      void handleReviewDocument(doc.id, false);
                                    }}>
                                    <Button
                                      type="button"
                                      variant="outline"
                                      className="border-red-200 text-red-600 hover:bg-red-50 cursor-pointer"
                                      disabled={reviewingDocumentId === doc.id}>
                                      {reviewingDocumentId === doc.id ? (
                                        <Loader2 className="h-4 w-4 animate-spin" />
                                      ) : (
                                        "Reprovar"
                                      )}
                                    </Button>
                                  </DeleteConfirmation>
                                  <DeleteConfirmation
                                    descriptionText={`Deseja aprovar o documento "${doc.originalFileName}"?`}
                                    confirmationText="Confirmar aprovação"
                                    deleteButtonClassName="bg-emerald-600 hover:bg-emerald-700"
                                    onDelete={() => {
                                      void handleReviewDocument(doc.id, true);
                                    }}>
                                    <Button
                                      type="button"
                                      className="primary-action cursor-pointer"
                                      disabled={reviewingDocumentId === doc.id}>
                                      {reviewingDocumentId === doc.id ? (
                                        <Loader2 className="h-4 w-4 animate-spin" />
                                      ) : (
                                        "Aprovar"
                                      )}
                                    </Button>
                                  </DeleteConfirmation>
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>

                      <div className="space-y-2">
                        <div className="flex items-center justify-between">
                          <Label className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
                            Histórico de documentos do grupo
                          </Label>
                          <span className="text-xs text-slate-500">
                            {groupReviewedDocuments.length} registro(s)
                          </span>
                        </div>

                        {groupReviewedDocuments.length === 0 ? (
                          <div className="rounded-lg border border-dashed border-slate-200 p-4 text-center text-sm text-slate-500">
                            Sem histórico de avaliações neste grupo.
                          </div>
                        ) : (
                          <div className="space-y-2">
                            {groupReviewedDocuments.map((doc) => {
                              const isRejected = Number(doc.status) === 2;
                              const statusLabel = isRejected
                                ? "Reprovado"
                                : "Aprovado";
                              const statusClass = isRejected
                                ? "text-red-700"
                                : "text-emerald-700";

                              return (
                                <div
                                  key={doc.id}
                                  className="rounded-xl border border-slate-200 px-4 py-3 flex items-start justify-between gap-3">
                                  <div className="min-w-0">
                                    <p className="truncate text-sm font-semibold text-slate-900">
                                      {doc.originalFileName}
                                    </p>
                                    <p className="text-xs text-slate-500">
                                      Enviado por {doc.senderName}
                                    </p>
                                    {doc.reviewedByName && doc.reviewedAt && (
                                      <p className={`text-xs ${statusClass}`}>
                                        {statusLabel} por {doc.reviewedByName}{" "}
                                        em {formatDateForShort(doc.reviewedAt)}
                                      </p>
                                    )}
                                    {doc.feedback && (
                                      <p className="text-xs text-slate-600 break-words">
                                        Feedback: {doc.feedback}
                                      </p>
                                    )}
                                  </div>
                                  <Button
                                    type="button"
                                    variant="outline"
                                    size="sm"
                                    className="gap-2 cursor-pointer shrink-0"
                                    disabled={downloadingDocumentId === doc.id}
                                    onClick={() =>
                                      void handleDownloadDocument(doc)
                                    }>
                                    {downloadingDocumentId === doc.id ? (
                                      <Loader2 className="h-4 w-4 animate-spin" />
                                    ) : (
                                      <Download className="h-4 w-4" />
                                    )}
                                    Baixar
                                  </Button>
                                </div>
                              );
                            })}
                          </div>
                        )}

                        <div className="flex gap-4 text-xs text-slate-500">
                          <span>
                            {groupApprovedDocuments.length} aprovado(s)
                          </span>
                          <span>
                            {groupRejectedDocuments.length} reprovado(s)
                          </span>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
};