import React, { useEffect, useState } from "react";
import { isAxiosError } from "axios";
import { useForm, Controller, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createEventSchema,
  type CreateEventFormData,
} from "@/schemas/eventSchema";
import { Loader2, AlertCircle, CheckCircle2, CalendarPlus } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
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

interface EventRegistrationItem {
  studentId: string;
  studentName: string;
  courseName: string;
  registeredAt: string;
}

type CreateEventTab = "details" | "registrations";

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
  const [registrationsError, setRegistrationsError] = useState<string | null>(null);
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
      const responseData = isAxiosError(error)
        ? (error.response?.data as { message?: string } | undefined)
        : undefined;
      setRegistrationsError(
        typeof responseData?.message === "string"
          ? responseData.message
          : "Não foi possível carregar os inscritos deste evento.",
      );
    } finally {
      setIsRegistrationsLoading(false);
    }
  };

  const handleTabChange = async (tab: CreateEventTab) => {
    setActiveTab(tab);

    if (tab === "registrations") {
      await loadRegistrations(1);
    }
  };

  const handleRegistrationsPageChange = async (page: number) => {
    if (page < 1 || page > registrationsData.totalPages) return;
    await loadRegistrations(page);
  };

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
            <div className="grid grid-cols-2 gap-1">
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
            </div>
          </div>
        )}

        {activeTab === "details" || !currentEventData?.id ? (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className="space-y-4">
            <div className="space-y-1">
              <Label htmlFor="name" required>Nome do Evento</Label>
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
              <Label htmlFor="description" required>Descrição</Label>
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
              <Label htmlFor="startDate" required>Data de Início</Label>
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
              <Label htmlFor="endDate" required>Data de Término</Label>
              <Input
                type="datetime-local"
                id="endDate"
                {...register("endDate")}
              />
              {errors.endDate && (
                <p className="text-xs text-red-600">{errors.endDate.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <Label htmlFor="capacity" required>Capacidade Máxima</Label>
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
        ) : (
          <div className="space-y-4">
            <div>
              <h3 className="text-lg font-semibold text-slate-900">
                Inscritos no evento
              </h3>
              <p className="text-sm text-slate-500">
                {currentEventData?.name || "Lista de pessoas inscritas no evento."}
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
                          <TableCell>{formatDateForShort(item.registeredAt)}</TableCell>
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
        )}
      </CardContent>
    </Card>
  );
};
