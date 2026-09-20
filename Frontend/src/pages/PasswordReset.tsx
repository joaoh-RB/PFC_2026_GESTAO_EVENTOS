import {
  resetPasswordSchema,
  type ResetPasswordFormData,
} from "@/schemas/authSchema";
import { api } from "@/services/api";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import React from "react";
import { useNavigate } from "react-router-dom";
import { AlertCircle, CheckCircle2, KeyRound, Loader2 } from "lucide-react";
import { Brand } from "@/components/Brand";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
export const PasswordReset: React.FC = () => {
  const [apiError, setApiError] = React.useState<string | null>(null);
  const [apiMessage, setApiMessage] = React.useState<string | null>(null);
  const navigate = useNavigate();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    mode: "onBlur",
  });
  const onSubmit = async (data: ResetPasswordFormData) => {
    setApiError(null);
    setApiMessage(null);
    try {
      await api.post("/auth/reset-password", {
        email: new URLSearchParams(window.location.search).get("email"),
        token: new URLSearchParams(window.location.search).get("token"),
        newPassword: data.password,
        confirmPassword: data.confirmPassword,
      });
      setApiMessage(
        "Senha redefinida com sucesso! Redirecionando para a página de login...",
      );
      setTimeout(() => navigate("/login"), 3000);
    } catch (error) {
      const responseError = error as {
        response?: { data?: { message?: string } };
      };
      setApiError(
        responseError.response?.data?.message ||
          "Não foi possível redefinir a senha.",
      );
    }
  };
  return (
    <div className="min-h-screen bg-[linear-gradient(135deg,#17104f_0%,#20166b_35%,#f6f7fb_35%,#f6f7fb_100%)] p-4 py-10">
      <div className="mx-auto mb-7 w-full max-w-md">
        <Brand inverse />
      </div>
      <div className="mx-auto w-full max-w-md rounded-2xl border border-[#e2e4e9] bg-white p-8 shadow-[0_18px_50px_rgba(20,15,65,0.14)]">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-cyan-50 text-[#12a7d4]">
            <KeyRound className="h-6 w-6" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">
            Redefinir senha
          </h1>
          <p className="mt-1 text-sm text-slate-600">Defina sua nova senha.</p>
        </div>

        {apiError && (
          <div className="mb-6 flex items-center gap-2 rounded-lg bg-red-50 p-4 text-sm text-red-700">
            <AlertCircle className="h-5 w-5 shrink-0" />
            <span>{apiError}</span>
          </div>
        )}

        {apiMessage && (
          <div className="mb-6 flex items-center gap-2 rounded-lg bg-emerald-50 p-4 text-sm text-emerald-700">
            <CheckCircle2 className="h-5 w-5 shrink-0" />
            <span>{apiMessage}</span>
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <Label className="block text-sm font-medium text-slate-700">
              Nova Senha
            </Label>
            <div className="relative mt-1">
              <Input
                {...register("password")}
                type="password"
                placeholder="Digite sua nova senha"
                disabled={apiMessage !== null}
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
              Confirme a Senha
            </Label>
            <div className="relative mt-1">
              <Input
                {...register("confirmPassword")}
                type="password"
                placeholder="Confirme sua nova senha"
                disabled={apiMessage !== null}
              />
            </div>
            {errors.confirmPassword && (
              <p className="mt-1 text-xs text-red-600">
                {errors.confirmPassword.message}
              </p>
            )}
          </div>
          <Button
            type="submit"
            disabled={isSubmitting || apiMessage !== null}
            className="primary-action mt-6 w-full">
            {isSubmitting ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Enviando...
              </>
            ) : (
              "Redefinir senha"
            )}
          </Button>
        </form>
      </div>
    </div>
  );
};
