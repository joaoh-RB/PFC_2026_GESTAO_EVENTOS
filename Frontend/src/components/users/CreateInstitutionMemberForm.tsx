import React, { useEffect } from "react";
import { useForm, Controller, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { Loader2, AlertCircle, CheckCircle2, User2 } from "lucide-react";
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
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  createInstitutionMemberSchema,
  type CreateInstitutionMemberFormData,
} from "@/schemas/userSchema";
import type { OptionItem } from "@/types/optionItem";

interface CreateInstitutionMemberFormProps {
  institutions: OptionItem[];
  courses: OptionItem[];
  userRoles: OptionItem[];
  userInstitutionId?: string | null;
  currentUserData?: CreateInstitutionMemberFormData | null;
  onSubmit: (data: CreateInstitutionMemberFormData) => void;
  isSubmitting: boolean;
  isSuccess: boolean;
  apiError: string | null;
  handleInstitutionChange: (institutionId: string) => void;
}

export const CreateInstitutionMemberForm: React.FC<
  CreateInstitutionMemberFormProps
> = ({
  institutions,
  courses,
  userRoles,
  currentUserData,
  userInstitutionId,
  onSubmit,
  isSubmitting,
  isSuccess,
  apiError,
  handleInstitutionChange,
}) => {
  const anchor = useComboboxAnchor();

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
    setValue,
    watch,
  } = useForm<CreateInstitutionMemberFormData>({
    resolver: zodResolver(
      createInstitutionMemberSchema,
    ) as Resolver<CreateInstitutionMemberFormData>,
    mode: "onBlur",
    defaultValues: {
      name: "",
      institutionId: userInstitutionId || "",
      userRole: 0,
      email: "",
      password: "",
    },
  });
  // eslint-disable-next-line react-hooks/incompatible-library
  const userRole = watch("userRole");
  useEffect(() => {
    if (currentUserData) {
      reset({
        id: currentUserData.id || "",
        name: currentUserData.name || "",
        institutionId: currentUserData.institutionId || userInstitutionId || "",
        userRole: currentUserData.userRole,
        email: currentUserData.email,
        password: currentUserData.password,
        courses: currentUserData.courses || [],
      });
    }
  }, [currentUserData, reset, userInstitutionId]);

  return (
    <div>
      <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-indigo-50 text-indigo-600">
        <User2 className="h-6 w-6" />
      </div>
      {currentUserData ? "Atualizar Usuário" : "Criar Novo Usuário"}
      <p className="mt-1 text-sm text-slate-600">
        {currentUserData
          ? "Preencha os dados abaixo para atualizar o usuário"
          : "Preencha os dados abaixo para cadastrar um novo usuário acadêmico"}
      </p>
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
            {currentUserData
              ? "Usuário atualizado com sucesso! Redirecionando..."
              : "Usuário criado com sucesso! Redirecionando..."}
          </span>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label htmlFor="name">Nome do Usuário</Label>
            <Input
              id="name"
              {...register("name")}
              placeholder="Nome completo"
            />
            {errors.name && (
              <p className="text-xs text-red-600">{errors.name.message}</p>
            )}
          </div>

          <div className="space-y-1">
            <Label htmlFor="user-email">Email</Label>
            <Input
              id="user-email"
              {...register("email")}
              className="resize-none"
              placeholder="Email do usuário"
            />
            {errors.email && (
              <p className="text-xs text-red-600">{errors.email.message}</p>
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
                  <Select
                    items={institutions}
                    disabled={!!currentUserData}
                    onValueChange={(institutionId) => {
                      field.onChange(institutionId);
                      setValue("courses", []);
                      handleInstitutionChange(institutionId!);
                    }}
                    value={field.value}>
                    <SelectTrigger className={"w-full"}>
                      <SelectValue placeholder="Selecione uma instituição" />
                    </SelectTrigger>
                    <SelectContent alignItemWithTrigger={true}>
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

          <div className="space-y-1 col-span-2">
            <Label htmlFor="userRole">Cargo</Label>
            <Controller
              name="userRole"
              control={control}
              render={({ field }) => (
                <Select
                  items={userRoles}
                  disabled={!!currentUserData}
                  onValueChange={(userRole) => {
                    field.onChange(userRole);
                  }}
                  value={field.value === 0 ? "" : field.value?.toString()}>
                  <SelectTrigger className={"w-full"}>
                    <SelectValue placeholder="Selecione um cargo" />
                  </SelectTrigger>
                  <SelectContent alignItemWithTrigger={true}>
                    {userRoles.map((inst) => (
                      <SelectItem key={inst.value} value={inst.value}>
                        {inst.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.userRole && (
              <p className="text-xs text-red-600">{errors.userRole.message}</p>
            )}
          </div>

          <div className="space-y-1 col-span-2">
            <Label htmlFor="password">Senha</Label>
            <Input type="password" id="password" {...register("password")} />
            {errors.password && (
              <p className="text-xs text-red-600">{errors.password.message}</p>
            )}
          </div>

          {userRole == 2 && (
            <div className="space-y-1 pt-2 col-span-2">
              <Label>Cursos Permitidos</Label>
              <Controller
                name="courses"
                control={control}
                render={({ field }) => (
                  <Combobox
                    items={courses}
                    multiple
                    value={field.value ? field.value.map((id) => id) : []}
                    onValueChange={(selectedNames) => {
                      field.onChange(selectedNames);
                    }}>
                    <ComboboxChips className={"w-full"} ref={anchor}>
                      <ComboboxValue>
                        {field.value
                          ? field.value.map((id) => {
                              const course = courses.find(
                                (c) => c.value === id,
                              );

                              return (
                                <ComboboxChip key={id}>
                                  {course?.label ?? id}
                                </ComboboxChip>
                              );
                            })
                          : []}
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
              {errors.courses && (
                <p className="text-xs text-red-600">{errors.courses.message}</p>
              )}
            </div>
          )}
        </div>
        <Button
          type="submit"
          disabled={isSubmitting || isSuccess}
          className="w-full text-md h-12 mt-4 bg-indigo-600 hover:bg-indigo-700 text-white">
          {isSubmitting ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" /> Criando
              usuário...
            </>
          ) : currentUserData ? (
            "Atualizar Usuário"
          ) : (
            "Criar Usuário"
          )}
        </Button>
      </form>
    </div>
  );
};
