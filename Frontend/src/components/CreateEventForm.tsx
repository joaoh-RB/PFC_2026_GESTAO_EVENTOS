import React from "react";
import { useForm, Controller, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createEventSchema,
  type CreateEventFormData,
} from "../schemas/eventSchema";
import { Loader2, AlertCircle, CheckCircle2, CalendarPlus } from "lucide-react";

// Componentes shadcn (Ajuste os caminhos conforme a sua estrutura)
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface OptionItem {
  value: string;
  label: string;
}

interface CreateEventFormProps {
  institutions: OptionItem[];
  courses: OptionItem[];
  eventTypes: OptionItem[];
  userInstitutionId?: string | null;
  onSubmit: (data: CreateEventFormData) => void;
  isSubmitting: boolean;
  isSuccess: boolean;
  apiError: string | null;
}

export const CreateEventForm: React.FC<CreateEventFormProps> = ({
  institutions,
  courses,
  eventTypes,
  userInstitutionId,
  onSubmit,
  isSubmitting,
  isSuccess,
  apiError,
}) => {
  const anchor = useComboboxAnchor();

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<CreateEventFormData>({
    resolver: zodResolver(createEventSchema) as Resolver<CreateEventFormData>,
    mode: "onBlur",
    defaultValues: {
      institutionId: userInstitutionId || "",
      eventType: undefined,
      allowDocuments: false,
      allowedCourses: [],
    },
  });

  return (
    <Card className="max-w-4xl mx-auto shadow-xl border-slate-100">
      <CardHeader className="text-center pb-8 pt-6">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-indigo-50 text-indigo-600">
          <CalendarPlus className="h-6 w-6" />
        </div>
        <CardTitle className="text-2xl font-bold text-slate-900">
          Criar Novo Evento
        </CardTitle>
        <p className="mt-1 text-sm text-slate-600">
          Preencha os dados abaixo para cadastrar um novo evento acadêmico
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
            <span>Evento criado com sucesso! Redirecionando...</span>
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className="space-y-4">
            <div className="space-y-1">
              <Label htmlFor="name">Nome do Evento</Label>
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
              <Label htmlFor="description">Descrição</Label>
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
                <Label>Instituição</Label>
                <Controller
                  name="institutionId"
                  control={control}
                  render={({ field }) => (
                    <Select onValueChange={field.onChange} value={field.value}>
                      <SelectTrigger className={"w-full"}>
                        <SelectValue placeholder="Selecione uma instituição" />
                      </SelectTrigger>
                      <SelectContent>
                        {institutions.map((inst) => (
                          <SelectItem key={inst.value} value={inst.value}>
                            {inst.label}
                          </SelectItem>
                        ))}
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
              <Label htmlFor="startDate">Data de Início</Label>
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
              <Label htmlFor="endDate">Data de Término</Label>
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
              <Label htmlFor="capacity">Capacidade Máxima</Label>
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
              <Label>Tipo de Evento</Label>
              <Controller
                name="eventType"
                control={control}
                render={({ field }) => (
                  <Select
                    items={eventTypes}
                    onValueChange={(val) => field.onChange(Number(val))}
                    value={field.value?.toString()}>
                    <SelectTrigger className={"w-full"}>
                      <SelectValue placeholder="Selecione o tipo" />
                    </SelectTrigger>
                    <SelectContent>
                      {eventTypes.map((type) => (
                        <SelectItem
                          key={type.value}
                          value={type.value.toString()}>
                          {type.label}
                        </SelectItem>
                      ))}
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
            <Label>Cursos Permitidos</Label>
            <Controller
              name="allowedCourses"
              control={control}
              render={({ field }) => (
                <Combobox
                  items={courses}
                  multiple
                  value={field.value.map(
                    (id) => courses.find((c) => c.value === id)?.label || id,
                  )}
                  onValueChange={(selectedNames) => {
                    field.onChange(selectedNames);
                  }}>
                  <ComboboxChips className={"w-full"} ref={anchor}>
                    <ComboboxValue>
                      {field.value.map((id) => (
                        <ComboboxChip key={id}>
                          {courses.find((c) => c.value === id)?.label || id}
                        </ComboboxChip>
                      ))}
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
            className="w-full text-md h-12 mt-4 bg-indigo-600 hover:bg-indigo-700 text-white">
            {isSubmitting ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" /> Criando
                evento...
              </>
            ) : (
              "Finalizar Criação do Evento"
            )}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
};
