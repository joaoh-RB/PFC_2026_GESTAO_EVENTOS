import React, { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Link, useNavigate } from "react-router-dom";
import { api } from "../services/api";
import {
  registerStudentSchema,
  type RegisterStudentFormData,
} from "../schemas/authSchema";
import {
  AlertCircle,
  Loader2,
  CheckCircle2,
  GraduationCap,
} from "lucide-react";
import type { OptionItem } from "@/types/optionItem";
import { Brand } from "@/components/Brand";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Button } from "@/components/ui/button";

export const Register: React.FC = () => {
  const [institutions, setInstitutions] = useState<OptionItem[]>([]);
  const [courses, setCourses] = useState<OptionItem[]>([]);
  const [apiError, setApiError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");

  const navigate = useNavigate();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    watch,
    setValue,
    control,
  } = useForm<RegisterStudentFormData>({
    resolver: zodResolver(registerStudentSchema),
    mode: "onBlur",
  });
  const selectedInstitutionId = watch("institutionId");
  useEffect(() => {
    let isMounted = true;

    const loadSelectOptions = async () => {
      try {
        const [instRes] = await Promise.all([
          api.get<OptionItem[]>("/institutions"),
        ]);

        if (isMounted) {
          setInstitutions(instRes.data);
        }
      } catch {
        if (isMounted) {
          setApiError(
            "Não foi possível carregar as instituições e cursos disponíveis.",
          );
        }
      }
    };

    loadSelectOptions();

    return () => {
      isMounted = false;
    };
  }, []);
  useEffect(() => {
    if (!selectedInstitutionId) {
      setCourses([]);
      setValue("courseId", "");
      return;
    }
    const fetchCoursesByInstitution = async () => {
      setValue("courseId", "");
      try {
        const res = await api.get<OptionItem[]>("/courses", {
          params: { institutionId: selectedInstitutionId },
        });
        setCourses(res.data);
      } catch {
        setCourses([]);
        setApiError("Não foi possível carregar os cursos desta instituição.");
      }
    };
    fetchCoursesByInstitution();
  }, [selectedInstitutionId, setValue]);

  const onSubmit = async (data: RegisterStudentFormData) => {
    setApiError(null);
    try {
      await api.post("/auth/register", {
        name: data.name,
        email: data.email,
        password: data.password,
        uniqueIdentifier: data.uniqueIdentifier,
        institutionId: data.institutionId,
        courseId: data.courseId,
      });
      setSuccessMessage(
        "Cadastro realizado com sucesso, seu usuário está pendente de aprovação. Entre em contato com sua instituição em caso de dúvidas. Assim que o acesso for aprovado será enviado um e-mail.",
      );
      setIsSuccess(true);
      setTimeout(() => navigate("/login"), 2000);
    } catch (err: any) {
      setApiError(
        err.response?.data?.message || "Erro ao realizar o cadastro de aluno.",
      );
    }
  };

  return (
    <div className="min-h-screen bg-[linear-gradient(135deg,#17104f_0%,#20166b_35%,#f6f7fb_35%,#f6f7fb_100%)] p-4 py-10">
      <div className="mx-auto mb-7 w-full max-w-2xl">
        <Brand inverse />
      </div>
      <div className="mx-auto w-full max-w-2xl rounded-2xl border border-[#e2e4e9] bg-white p-8 shadow-[0_18px_50px_rgba(20,15,65,0.14)]">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-cyan-50 text-[#12a7d4]">
            <GraduationCap className="h-6 w-6" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">
            Cadastro de Aluno
          </h1>
          <p className="mt-1 text-sm text-slate-600">
            Crie sua conta para participar dos eventos acadêmicos
          </p>
        </div>

        {apiError && (
          <div className="mb-6 flex items-center gap-2 rounded-lg bg-red-50 p-4 text-sm text-red-700">
            <AlertCircle className="h-5 w-5 shrink-0" />
            <span>{apiError}</span>
          </div>
        )}

        {isSuccess && (
          <div className="mb-6 flex items-center gap-2 rounded-lg bg-emerald-50 p-4 text-sm text-emerald-700">
            <CheckCircle2 className="h-5 w-5 shrink-0" />
            <span>{successMessage}</span>
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <Label className="block text-sm font-medium text-slate-700">
                Nome Completo
              </Label>
              <div className="relative mt-1">
                <Input
                  {...register("name")}
                  type="text"
                  placeholder="Nome do Estudante"
                />
              </div>
              {errors.name && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.name.message}
                </p>
              )}
            </div>

            <div>
              <Label className="block text-sm font-medium text-slate-700">
                E-mail
              </Label>
              <div className="relative mt-1">
                <Input
                  {...register("email")}
                  type="email"
                  placeholder="aluno@instituicao.edu.br"
                />
              </div>
              {errors.email && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.email.message}
                </p>
              )}
            </div>

            <div>
              <Label className="block text-sm font-medium text-slate-700">
                RA / Matrícula
              </Label>
              <div className="relative mt-1">
                <Input
                  {...register("uniqueIdentifier")}
                  type="text"
                  placeholder="Ex: 202610098"
                />
              </div>
              {errors.uniqueIdentifier && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.uniqueIdentifier.message}
                </p>
              )}
            </div>

            <div>
              <Label className="block text-sm font-medium text-slate-700">
                Instituição
              </Label>
              <div className="relative mt-1">
                <Controller
                  name="institutionId"
                  control={control}
                  render={({ field }) => (
                    <Select
                      items={institutions}
                      onValueChange={(institutionId) => {
                        field.onChange(institutionId);
                        setValue("courseId", "");
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
              </div>
              {errors.institutionId && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.institutionId.message}
                </p>
              )}
            </div>

            <div>
              <Label className="block text-sm font-medium text-slate-700">
                Curso
              </Label>
              <div className=" mt-1">
                <Controller
                  name="courseId"
                  control={control}
                  render={({ field }) => (
                    <Select
                      items={courses}
                      onValueChange={(courseId) => {
                        field.onChange(courseId);
                      }}
                      value={field.value}>
                      <SelectTrigger className={"w-full"}>
                        <SelectValue placeholder="Selecione um curso" />
                      </SelectTrigger>
                      <SelectContent alignItemWithTrigger={true}>
                        <SelectGroup>
                          {courses.length == 0 && (
                            <SelectItem value="" disabled>
                              {!selectedInstitutionId
                                ? "Selecione uma instituição antes"
                                : "Nenhum curso disponível"}
                            </SelectItem>
                          )}
                          {courses.map((course) => (
                            <SelectItem key={course.value} value={course.value}>
                              {course.label}
                            </SelectItem>
                          ))}
                        </SelectGroup>
                      </SelectContent>
                    </Select>
                  )}
                />
              </div>
              {errors.courseId && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.courseId.message}
                </p>
              )}
            </div>

            <div>
              <Label className="block text-sm font-medium text-slate-700">
                Senha
              </Label>
              <div className="relative mt-1">
                <Input
                  {...register("password")}
                  type="password"
                  placeholder="••••••••"
                />
              </div>
              {errors.password && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.password.message}
                </p>
              )}
            </div>

            <div>
              <Label className="block text-sm font-medium text-slate-700">
                Confirmar Senha
              </Label>
              <div className="relative mt-1">
                <Input
                  {...register("confirmPassword")}
                  type="password"
                  placeholder="••••••••"
                />
              </div>
              {errors.confirmPassword && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.confirmPassword.message}
                </p>
              )}
            </div>
          </div>

          <Button
            type="submit"
            disabled={isSubmitting || isSuccess}
            className="primary-action mt-6 w-full">
            {isSubmitting ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Criando conta...
              </>
            ) : (
              "Finalizar Cadastro"
            )}
          </Button>
        </form>

        <div className="mt-6 text-center text-sm text-slate-600">
          Já tem uma conta?{" "}
          <Link
            to="/login"
            className="font-semibold text-[#109bc6] hover:underline">
            Entrar
          </Link>
        </div>
      </div>
    </div>
  );
};
