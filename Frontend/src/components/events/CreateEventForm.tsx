import React, { useEffect, useState } from "react";
import { isAxiosError } from "axios";
import { useForm, Controller, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createEventSchema,
  type CreateEventFormData,
} from "@/schemas/eventSchema";
import {
  Loader2,
  AlertCircle,
  CheckCircle2,
  CalendarPlus,
  Download,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Combobox,
  ComboboxChips,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxItem,
  ComboboxList,
  ComboboxValue,
  ComboboxChip,
  useComboboxAnchor,
} from "@/components/ui/combobox";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
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
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { api } from "@/services/api";
import type { OptionItem } from "@/types/optionItem";
import type { PagedResult } from "@/types/utils";
import { formatDateForShort } from "@/utils/format";
import type { EventFeedbackDto } from "./EventFeedbackModal";
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

type CreateEventTab = "details" | "registrations" | "groups";

const groupMemberStatusLabel: Record<number, string> = {
  1: "Convidado",
  2: "Aceito",
  3: "Recusado",
};

interface CreateEventFormProps {
  institutions: OptionItem[];
  courses: OptionItem[];
  eventTypes: OptionItem[];
  currentEventData?: (CreateEventFormData & { id: string }) | null;
  userInstitutionId?: string | null;
  onSubmit: (data: CreateEventFormData) => void;
  isSubmitting: boolean;
  isSuccess: boolean;
  apiError: string | null;
  handleInstitutionChange: (institutionId: string) => void;
}

export const CreateEventForm: React.FC<CreateEventFormProps> = ({
  institutions,
  courses,
  eventTypes,
  currentEventData,
  userInstitutionId,
  onSubmit,
  isSubmitting,
  isSuccess,
  apiError,
  handleInstitutionChange,
}) => {
  const anchor = useComboboxAnchor();
  const [activeTab, setActiveTab] = useState<CreateEventTab>("details");
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
    pageSize: 10,
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

  const getApiMessage = (error: unknown, fallback: string): string => {
    const responseData = isAxiosError(error)
      ? (error.response?.data as { message?: string } | undefined)
      : undefined;

    return typeof responseData?.message === "string"
      ? responseData.message
      : fallback;
  };

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
    setValue,
  } = useForm<CreateEventFormData>({
    resolver: zodResolver(createEventSchema) as Resolver<CreateEventFormData>,
    mode: "onBlur",
    defaultValues: {
      capacity: 0,
      name: "",
      description: "",
      startDate: "",
      endDate: "",
      institutionId: userInstitutionId || "",
      eventType: 0,
      allowDocuments: false,
      allowedCourses: [],
    },
  });

  useEffect(() => {
    if (currentEventData) {
      reset({
        name: currentEventData.name || "",
        description: currentEventData.description || "",
        institutionId:
          currentEventData.institutionId || userInstitutionId || "",
        startDate: currentEventData.startDate,
        endDate: currentEventData.endDate,
        capacity: currentEventData.capacity,
        eventType: currentEventData.eventType,
        allowDocuments: currentEventData.allowDocuments ?? false,
        allowedCourses: currentEventData.allowedCourses || [],
      });
    }
  }, [currentEventData, reset, userInstitutionId]);

  const loadRegistrations = async (page: number = 1) => {
    if (!currentEventData?.id) return;

    setIsRegistrationsLoading(true);
    setRegistrationsError(null);

    try {
      const response = await api.get<PagedResult<EventRegistrationItem>>(
        `/events/${currentEventData.id}/registrations`,
        {
          params: {
            pageNumber: page,
            pageSize: registrationsData.pageSize,
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
  };

  const loadGroups = async () => {
    if (!currentEventData?.id) return;

    setIsGroupsLoading(true);
    setGroupsError(null);

    try {
      const response = await api.get<EventGroupItem[]>(
        `/groups/event/${currentEventData.id}`,
      );

      setGroupsData(response.data ?? []);
    } catch (error: unknown) {
      setGroupsError(
        getApiMessage(error, "Não foi possível carregar os grupos do evento."),
      );
    } finally {
      setIsGroupsLoading(false);
    }
  };

  const loadDocuments = async () => {
    if (!currentEventData?.id) return;

    setIsDocumentsLoading(true);
    setDocumentsError(null);

    try {
      const pendingRes = await api.get<EventDocumentItem[]>(
        `/eventdocuments/${currentEventData.id}/pending-approvals`,
      );

      let reviewedItems: EventDocumentItem[] = [];
      try {
        const reviewedRes = await api.get<EventDocumentItem[]>(
          `/eventdocuments/${currentEventData.id}/reviewed`,
        );
        reviewedItems = reviewedRes.data ?? [];
      } catch (reviewedError: unknown) {
        if (
          isAxiosError(reviewedError) &&
          reviewedError.response?.status === 404
        ) {
          const approvedRes = await api.get<EventDocumentItem[]>(
            `/eventdocuments/${currentEventData.id}/approved`,
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
  };

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
    if (!currentEventData?.id) return;
    setIsDownloadingCompiledZip(true);
    setDocumentsError(null);

    try {
      const response = await api.get(
        `/eventdocuments/${currentEventData.id}/compiled-zip`,
        {
          responseType: "blob",
        },
      );
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute(
        "download",
        `documentos-aprovados-${currentEventData.id}.zip`,
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

  const loadFeedbacks = async () => {
    if (!currentEventData?.id) return;

    setIsFeedbacksLoading(true);
    setFeedbacksError(null);

    try {
      const response = await api.get<EventFeedbackDto[]>(
        `/events/${currentEventData.id}/feedbacks`,
      );
      setFeedbacks(Array.isArray(response.data) ? response.data : []);
    } catch (error: unknown) {
      const responseData = isAxiosError(error)
        ? (error.response?.data as { message?: string } | undefined)
        : undefined;
      setFeedbacksError(
        typeof responseData?.message === "string"
          ? responseData.message
          : "Não foi possível carregar os feedbacks deste evento.",
      );
    } finally {
      setIsFeedbacksLoading(false);
    }
  };

  const handleTabChange = async (tab: CreateEventTab) => {
    setActiveTab(tab);

    if (tab === "registrations") {
      await Promise.all([loadRegistrations(1), loadFeedbacks()]);
      return;
    }

    if (tab === "groups") {
      await Promise.all([loadGroups(), loadDocuments()]);
    }
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

  const isEventEnded = Boolean(
    currentEventData?.endDate &&
      new Date(currentEventData.endDate).getTime() <= Date.now(),
  );

  return (
    <Card className="surface-card max-w-4xl mx-auto shadow-none">
      <CardHeader className="text-center pb-8 pt-6">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-cyan-50 text-[#12a7d4]">
          <CalendarPlus className="h-6 w-6" />
        </div>
        <CardTitle className="text-2xl font-bold text-slate-900">
          {currentEventData ? "Atualizar Evento" : "Criar Novo Evento"}
        </CardTitle>
        <p className="mt-1 text-sm text-slate-600">
          {currentEventData
            ? "Preencha os dados abaixo para atualizar o evento"
            : "Preencha os dados abaixo para cadastrar um novo evento acadêmico"}
        </p>
      </CardHeader>

      <CardContent>
        {apiError && (
          <div className="mb-6 flex items-center gap-2 rounded-lg bg-red-50 p-4 text-sm text-red-700">
            <AlertCircle className="h-5 w-5 shrink-0" />
            <span>{apiError}</span>
          </div>
        )}

        {isSuccess && (
          <div className="mb-6 flex items-center gap-2 rounded-lg bg-emerald-50 p-4 text-sm text-emerald-700">
            <CheckCircle2 className="h-5 w-5 shrink-0" />
            <span>
              {currentEventData
                ? "Evento atualizado com sucesso! Redirecionando..."
                : "Evento criado com sucesso! Redirecionando..."}
            </span>
          </div>
        )}

        {currentEventData?.id && (
          <div className="mb-6 rounded-xl border border-slate-200 bg-slate-50 p-1">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-1">
              <button
                type="button"
                className={`rounded-lg px-4 py-2 text-sm font-semibold transition ${
                  activeTab === "details"
                    ? "bg-white text-slate-900 shadow-sm"
                    : "text-slate-500 hover:text-slate-700"
                }`}
                onClick={() => void handleTabChange("details")}>
                Dados do evento
              </button>
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

        {activeTab === "details" || !currentEventData?.id ? (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
            <div className="space-y-4">
              <div className="space-y-1">
                <Label htmlFor="name" required>
                  Nome do Evento
                </Label>
                <Input
                  id="name"
                  {...register("name")}
                  placeholder="Ex: Semana da Computação"
                />
                {errors.name && (
                  <p className="text-xs text-red-600">{errors.name.message}</p>
                )}
              </div>

              <div className="space-y-1">
                <Label htmlFor="description" required>
                  Descrição
                </Label>
                <Textarea
                  id="description"
                  {...register("description")}
                  rows={3}
                  className="resize-none"
                  placeholder="Detalhes sobre o evento..."
                />
                {errors.description && (
                  <p className="text-xs text-red-600">
                    {errors.description.message}
                  </p>
                )}
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
              {!userInstitutionId && (
                <div className="sm:col-span-2 space-y-1">
                  <Label required>Instituição</Label>
                  <Controller
                    name="institutionId"
                    control={control}
                    render={({ field }) => (
                      <Select
                        items={institutions}
                        onValueChange={(institutionId) => {
                          field.onChange(institutionId);
                          setValue("allowedCourses", []);
                          handleInstitutionChange(institutionId!);
                        }}
                        value={field.value}>
                        <SelectTrigger className={"w-full"}>
                          <SelectValue placeholder="Selecione uma instituição" />
                        </SelectTrigger>
                        <SelectContent alignItemWithTrigger={true}>
                          <SelectGroup>
                            {institutions.map((inst) => (
                              <SelectItem key={inst.value} value={inst.value}>
                                {inst.label}
                              </SelectItem>
                            ))}
                          </SelectGroup>
                        </SelectContent>
                      </Select>
                    )}
                  />
                  {errors.institutionId && (
                    <p className="text-xs text-red-600">
                      {errors.institutionId.message}
                    </p>
                  )}
                </div>
              )}

              <div className="space-y-1">
                <Label htmlFor="startDate" required>
                  Data de Início
                </Label>
                <Input
                  type="datetime-local"
                  id="startDate"
                  {...register("startDate")}
                />
                {errors.startDate && (
                  <p className="text-xs text-red-600">
                    {errors.startDate.message}
                  </p>
                )}
              </div>

              <div className="space-y-1">
                <Label htmlFor="endDate" required>
                  Data de Término
                </Label>
                <Input
                  type="datetime-local"
                  id="endDate"
                  {...register("endDate")}
                />
                {errors.endDate && (
                  <p className="text-xs text-red-600">
                    {errors.endDate.message}
                  </p>
                )}
              </div>

              <div className="space-y-1">
                <Label htmlFor="capacity" required>
                  Capacidade Máxima
                </Label>
                <Input
                  type="number"
                  id="capacity"
                  min="1"
                  {...register("capacity")}
                />
                {errors.capacity && (
                  <p className="text-xs text-red-600">
                    {errors.capacity.message}
                  </p>
                )}
              </div>

              <div className="space-y-1">
                <Label required>Tipo de Evento</Label>
                <Controller
                  name="eventType"
                  control={control}
                  render={({ field }) => (
                    <Select
                      items={eventTypes}
                      onValueChange={(val) => field.onChange(Number(val))}
                      value={field.value?.toString()}>
                      <SelectTrigger className={"w-full !h-10"}>
                        <SelectValue placeholder="Selecione o tipo" />
                      </SelectTrigger>
                      <SelectContent alignItemWithTrigger={true}>
                        <SelectGroup>
                          {eventTypes.map((type) => (
                            <SelectItem
                              key={type.value}
                              value={type.value.toString()}>
                              {type.label}
                            </SelectItem>
                          ))}
                        </SelectGroup>
                      </SelectContent>
                    </Select>
                  )}
                />
                {errors.eventType && (
                  <p className="text-xs text-red-600">
                    {errors.eventType.message}
                  </p>
                )}
              </div>
            </div>

            <div className="space-y-1 pt-2">
              <Label required>Cursos Permitidos</Label>
              <Controller
                name="allowedCourses"
                control={control}
                render={({ field }) => (
                  <Combobox
                    items={courses}
                    multiple
                    value={field.value.map((id) => id)}
                    onValueChange={(selectedNames) => {
                      field.onChange(selectedNames);
                    }}>
                    <ComboboxChips className={"w-full"} ref={anchor}>
                      <ComboboxValue>
                        {field.value.map((id) => {
                          const course = courses.find((c) => c.value === id);

                          return (
                            <ComboboxChip key={id}>
                              {course?.label ?? id}
                            </ComboboxChip>
                          );
                        })}
                      </ComboboxValue>
                    </ComboboxChips>
                    <ComboboxContent anchor={anchor}>
                      <ComboboxEmpty>Nenhum curso disponível</ComboboxEmpty>
                      <ComboboxList>
                        {courses.map((course) => (
                          <ComboboxItem key={course.value} value={course.value}>
                            {course.label}
                          </ComboboxItem>
                        ))}
                      </ComboboxList>
                    </ComboboxContent>
                  </Combobox>
                )}
              />
              {errors.allowedCourses && (
                <p className="text-xs text-red-600">
                  {errors.allowedCourses.message}
                </p>
              )}
            </div>

            <div className="flex items-center space-x-3 py-4 border-b border-slate-100">
              <Controller
                name="allowDocuments"
                control={control}
                render={({ field }) => (
                  <Switch
                    id="allow-docs"
                    checked={field.value}
                    onCheckedChange={field.onChange}
                  />
                )}
              />
              <Label
                htmlFor="allow-docs"
                className="cursor-pointer font-medium text-slate-700">
                Permitir envio de documentos para a apresentação de trabalhos
              </Label>
            </div>

            <Button
              type="submit"
              disabled={isSubmitting || isSuccess}
              className="primary-action mt-4 h-11 w-full">
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" /> Criando
                  evento...
                </>
              ) : currentEventData ? (
                "Atualizar Evento"
              ) : (
                "Criar Evento"
              )}
            </Button>
          </form>
        ) : activeTab === "registrations" ? (
          <div className="space-y-8">
            <div className="space-y-4">
              <div>
                <h3 className="text-lg font-semibold text-slate-900">
                  Inscritos no evento
                </h3>
                <p className="text-sm text-slate-500">
                  {currentEventData?.name ||
                    "Lista de pessoas inscritas no evento."}
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
              {currentEventData?.allowDocuments && isEventEnded && approvedDocuments.some((doc) => Number(doc.status) === 1) && (
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

                  return (
                    <div
                      key={group.id}
                      className="rounded-xl border border-slate-200 p-4 space-y-4">
                      <div>
                        <div className="mb-2 flex items-center justify-between gap-2">
                          <p className="font-semibold text-slate-900">
                            Grupo {group.number}
                          </p>
                          <span className="text-xs text-slate-500">
                            Líder: {group.leaderName}
                          </span>
                        </div>
                        <ul className="space-y-2">
                          {group.members.map((member) => (
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
                                </span >
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
