import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { loginSchema, type LoginFormData } from "../schemas/authSchema";
import { Brand } from "../components/Brand";
import {
  AlertCircle,
  ArrowLeft,
  CalendarDays,
  Eye,
  EyeOff,
  Loader2,
  ShieldCheck,
  UsersRound,
} from "lucide-react";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import type { AxiosError } from "axios";
import api from "@/services/api";
interface ChangePasswordFormData {
  newPassword: string;
  confirmPassword: string;
}
export function Login() {
  const [apiError, setApiError] = useState<string | null>(null);
  const [requires2FA, setRequires2FA] = useState(false);
  const [requiresPasswordChange, setRequiresPasswordChange] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [isSubmittingNewPassword, setIsSubmittingNewPassword] = useState(false);
  const [pendingCredentials, setPendingCredentials] = useState<{
    email: string;
    currentPassword: string;
  } | null>(null);
  const { login, syncAuth } = useAuth();
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    mode: "onBlur",
  });
  const {
    register: registerPasswordChange,
    handleSubmit: handleSubmitPasswordChange,
    formState: { errors: passwordChangeErrors },
  } = useForm<ChangePasswordFormData>();

  const onSubmit = async (data: LoginFormData) => {
    setApiError(null);
    try {
      const response = await login(data);
      if (response.requiresPasswordChange) {
        setPendingCredentials({
          email: data.email,
          currentPassword: data.password,
        });
        setRequiresPasswordChange(true);
        return;
      }
      if (response.requiresTwoFactor) {
        setRequires2FA(true);
        return;
      }
      navigate("/dashboard");
    } catch (err: unknown) {
      const error = err as AxiosError<{ message: string }>;

      setApiError(
        error.response?.data?.message ?? "E-mail, senha ou código inválidos.",
      );
    }
  };
  const onSubmitNewPassword = async (data: ChangePasswordFormData) => {
    setApiError(null);

    if (data.newPassword !== data.confirmPassword) {
      setApiError("A confirmação de senha não coincide com a nova senha.");
      return;
    }
    setIsSubmittingNewPassword(true);
    try {
      await api.post("/auth/change-initial-password", {
        email: pendingCredentials?.email,
        currentPassword: pendingCredentials?.currentPassword,
        newPassword: data.newPassword,
        confirmPassword: data.confirmPassword,
      });
      await syncAuth();
      navigate("/dashboard");
    } catch (err: unknown) {
      const error = err as AxiosError<{ message: string }>;
      setApiError(
        error.response?.data?.message ?? "Erro ao redefinir a nova senha.",
      );
    } finally {
      setIsSubmittingNewPassword(false);
    }
  };
  const handleBackToCredentials = () => {
    setRequiresPasswordChange(false);
    setRequires2FA(false);
    setValue("twoFactorCode", "");
    setIsSubmittingNewPassword(false);
    setApiError(null);
  };

  return (
    <div className="grid min-h-screen bg-white lg:grid-cols-[1.08fr_1fr]">
      <section className="relative hidden min-h-screen overflow-hidden bg-[#120b46] lg:block">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_72%_30%,rgba(35,185,224,0.28),transparent_30%),radial-gradient(circle_at_15%_15%,rgba(92,76,193,0.38),transparent_26%),linear-gradient(145deg,#24176d_0%,#120b46_70%)]" />
        <div className="absolute -right-16 top-[17%] size-[440px] rounded-full border border-cyan-300/10" />
        <div className="absolute -right-5 top-[22%] size-[340px] rounded-full border border-cyan-300/15" />
        <div className="absolute right-[12%] top-[29%] grid size-[220px] place-items-center rounded-[42%_58%_58%_42%] border border-white/10 bg-white/[0.04] shadow-[0_0_80px_rgba(25,176,216,0.15)] backdrop-blur-sm">
          <CalendarDays
            className="size-24 text-cyan-300/75"
            strokeWidth={0.9}
          />
        </div>
        <div className="absolute left-[14%] top-[27%] flex items-center gap-3 rounded-2xl border border-white/10 bg-white/[0.06] px-4 py-3 text-white/80 backdrop-blur">
          <UsersRound className="size-5 text-cyan-300" />
          <div>
            <p className="text-xs font-semibold">Comunidade conectada</p>
            <p className="text-[10px] text-white/45">
              Eventos, alunos e instituições
            </p>
          </div>
        </div>
        <div className="absolute right-[17%] top-[58%] flex items-center gap-3 rounded-2xl border border-white/10 bg-white/[0.06] px-4 py-3 text-white/80 backdrop-blur">
          <ShieldCheck className="size-5 text-emerald-300" />
          <div>
            <p className="text-xs font-semibold">Gestão segura</p>
            <p className="text-[10px] text-white/45">
              Controle de acesso integrado
            </p>
          </div>
        </div>

        <div className="relative flex h-full min-h-screen flex-col justify-between p-12 xl:p-16">
          <Brand inverse />
          <div className="max-w-lg pb-4">
            <h1 className="text-4xl font-semibold leading-[1.12] tracking-[-0.04em] text-white xl:text-5xl">
              Eventos acadêmicos
              <br />
              com mais eficiência
            </h1>
            <p className="mt-5 max-w-md text-sm leading-6 text-white/55">
              Uma plataforma integrada para instituições, cursos, membros e
              alunos organizarem experiências que conectam conhecimento.
            </p>
          </div>
        </div>
      </section>

      <section className="flex min-h-screen items-center justify-center bg-[#fbfbfc] px-5 py-10">
        <div className="w-full max-w-[430px]">
          <div className="mb-8 flex justify-center lg:hidden">
            <Brand />
          </div>
          <div className="rounded-2xl border border-[#e2e4e9] bg-white p-7 shadow-[0_16px_50px_rgba(26,22,64,0.08)] sm:p-9">
            <div className="mb-7">
              <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[#139dc7]">
                {requiresPasswordChange
                  ? "Troca de senha"
                  : requires2FA
                    ? "Segunda etapa"
                    : "Bem-vindo"}
              </p>
              <h2 className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-[#1d193d]">
                {requiresPasswordChange
                  ? "Altere sua senha"
                  : requires2FA
                    ? "Confirme sua identidade"
                    : "Acesse sua conta"}
              </h2>
              <p className="mt-1.5 text-sm text-slate-500">
                {requiresPasswordChange
                  ? "Altere sua senha para continuar."
                  : requires2FA
                    ? "Digite o código do seu aplicativo autenticador."
                    : "Informe seu e-mail e senha para continuar."}
              </p>
            </div>

            {apiError && (
              <div className="mb-5 flex items-center gap-2 rounded-lg border border-red-100 bg-red-50 p-3 text-xs text-red-700">
                <AlertCircle className="size-4 shrink-0" />
                {apiError}
              </div>
            )}

            {requiresPasswordChange ? (
              <form
                onSubmit={handleSubmitPasswordChange(onSubmitNewPassword)}
                className="space-y-4">
                <div className="block">
                  <Label htmlFor="newPassword" className="mb-1 ml-1">
                    Nova Senha
                  </Label>
                  <div className="relative mt-1.5 block">
                    <Input
                      {...registerPasswordChange("newPassword", {
                        required: "Informe a nova senha",
                        minLength: {
                          value: 8,
                          message: "A senha deve ter pelo menos 8 caracteres",
                        },
                      })}
                      id="newPassword"
                      type={showNewPassword ? "text" : "password"}
                      placeholder="••••••••"
                      autoFocus
                    />
                    <button
                      type="button"
                      onClick={() => setShowNewPassword((v) => !v)}
                      className="absolute inset-y-0 right-0 flex w-10 items-center justify-center text-slate-400 hover:text-slate-600"
                      aria-label={
                        showNewPassword ? "Ocultar senha" : "Mostrar senha"
                      }>
                      {showNewPassword ? (
                        <EyeOff className="size-4" />
                      ) : (
                        <Eye className="size-4" />
                      )}
                    </button>
                  </div>
                  {passwordChangeErrors.newPassword && (
                    <span className="mt-1 block text-xs text-red-600">
                      {passwordChangeErrors.newPassword.message}
                    </span>
                  )}
                </div>

                <div className="block">
                  <Label htmlFor="confirmPassword" className="mb-1 ml-1">
                    Confirmar Nova Senha
                  </Label>
                  <Input
                    {...registerPasswordChange("confirmPassword", {
                      required: "Confirme a nova senha",
                    })}
                    id="confirmPassword"
                    type="password"
                    placeholder="••••••••"
                  />
                  {passwordChangeErrors.confirmPassword && (
                    <span className="mt-1 block text-xs text-red-600">
                      {passwordChangeErrors.confirmPassword.message}
                    </span>
                  )}
                </div>

                <button
                  type="submit"
                  disabled={isSubmittingNewPassword}
                  className="primary-action mt-2 w-full">
                  {isSubmittingNewPassword && (
                    <Loader2 className="size-4 animate-spin" />
                  )}
                  {isSubmittingNewPassword
                    ? "Atualizando senha..."
                    : "Salvar senha e entrar"}
                </button>

                <button
                  type="button"
                  onClick={handleBackToCredentials}
                  className="flex w-full items-center justify-center gap-1.5 text-xs font-semibold text-slate-500 hover:text-[#17104f]">
                  <ArrowLeft className="size-3.5" />
                  Cancelar e voltar
                </button>
              </form>
            ) : (
              <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                {!requires2FA ? (
                  <>
                    <div className="block">
                      <Label htmlFor={"email"} className="mb-1 ml-1">
                        E-mail
                      </Label>
                      <Input
                        {...register("email")}
                        type="email"
                        name="email"
                        placeholder="seu@email.com"
                      />
                      {errors.email && (
                        <span className="mt-1 block text-xs text-red-600">
                          {errors.email.message}
                        </span>
                      )}
                    </div>
                    <div className="block">
                      <Label htmlFor={"password"} className="mb-1 ml-1">
                        Senha
                      </Label>
                      <span className="relative mt-1.5 block">
                        <Input
                          {...register("password")}
                          type={showPassword ? "text" : "password"}
                          placeholder="Digite sua senha"
                          name="password"
                        />
                        <button
                          type="button"
                          onClick={() => setShowPassword((value) => !value)}
                          className="absolute inset-y-0 right-0 flex w-10 items-center justify-center text-slate-400 hover:text-slate-600"
                          aria-label={
                            showPassword ? "Ocultar senha" : "Mostrar senha"
                          }>
                          {showPassword ? (
                            <EyeOff className="size-4" />
                          ) : (
                            <Eye className="size-4" />
                          )}
                        </button>
                      </span>
                      {errors.password && (
                        <span className="mt-1 block text-xs text-red-600">
                          {errors.password.message}
                        </span>
                      )}
                    </div>
                  </>
                ) : (
                  <div className="block">
                    <Label className="mb-1" htmlFor={"twoFactorCode"}>
                      Código de autenticação
                    </Label>
                    <Input
                      {...register("twoFactorCode")}
                      type="text"
                      maxLength={6}
                      autoFocus
                      placeholder="000000"
                      name="twoFactorCode"
                      className="text-center font-mono text-lg tracking-[0.35em]"
                    />
                  </div>
                )}

                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="primary-action mt-2 w-full">
                  {isSubmitting && <Loader2 className="size-4 animate-spin" />}
                  {isSubmitting
                    ? "Validando..."
                    : requires2FA
                      ? "Confirmar código"
                      : "Entrar"}
                </button>

                {requires2FA && (
                  <button
                    type="button"
                    onClick={handleBackToCredentials}
                    className="flex w-full items-center justify-center gap-1.5 text-xs font-semibold text-slate-500 hover:text-[#17104f]">
                    <ArrowLeft className="size-3.5" />
                    Voltar para as credenciais
                  </button>
                )}
              </form>
            )}
            {!requires2FA && !requiresPasswordChange && (
              <p className="mt-6 border-t border-slate-100 pt-5 text-center text-xs text-slate-500">
                Ainda não possui acesso?{" "}
                <Link
                  to="/register"
                  className="font-semibold text-[#109bc6] hover:underline">
                  Solicitar cadastro
                </Link>
              </p>
            )}
          </div>
          <p className="mt-5 text-center text-[10px] text-slate-400">
            SYMPLOSIO © 2026. Todos os direitos reservados.
          </p>
        </div>
      </section>
    </div>
  );
}
