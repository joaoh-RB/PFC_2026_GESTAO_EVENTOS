import React, { useEffect, useState } from "react";
import { api } from "@/services/api";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Loader2, FileText } from "lucide-react";
import type { LegalDocument } from "@/types/legalDocument";
import { Link } from "react-router-dom";

const typeLabels: Record<string, string> = {
  PoliticaPrivacidade: "Política de Privacidade",
  TermosDeUso: "Termos de Uso",
};

interface TermsAcceptanceGateProps {
  children: React.ReactNode;
}

export const TermsAcceptanceGate: React.FC<TermsAcceptanceGateProps> = ({
  children,
}) => {
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState<LegalDocument[]>([]);
  const [checked, setChecked] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isExempt = user?.role === "Administrador";

  useEffect(() => {
    if (isExempt) {
      setLoading(false);
      return;
    }
    api
      .get<LegalDocument[]>("/legal-documents/pending")
      .then((res) => setPending(res.data))
      .catch(() => setPending([]))
      .finally(() => setLoading(false));
  }, [isExempt]);

  const handleAccept = async () => {
    setIsSubmitting(true);
    setError(null);
    try {
      await api.post("/legal-documents/accept", {
        documentIds: pending.map((doc) => doc.id),
      });
      setPending([]);
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          "Não foi possível registrar o aceite. Tente novamente.",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[#f6f7fb]">
        <Loader2 className="h-8 w-8 animate-spin text-indigo-600" />
      </div>
    );
  }

  if (!isExempt && pending.length > 0) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[#f6f7fb] p-4">
        <div className="w-full max-w-2xl rounded-2xl bg-white p-6 shadow-xl">
          <div className="mb-5 flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
              <FileText className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-lg font-bold text-slate-900">
                Atualizamos nossos termos
              </h1>
              <p className="text-sm text-slate-500">
                Para continuar usando o sistema, você precisa ler e concordar
                com o(s) documento(s) abaixo.
              </p>
            </div>
          </div>

          <div className="mb-5 max-h-72 space-y-4 overflow-y-auto rounded-lg border border-slate-200 p-4">
            {pending.map((doc) => (
              <div key={doc.id}>
                <h2 className="mb-1 text-sm font-semibold text-slate-800">
                  {typeLabels[doc.type] ?? doc.type} — versão {doc.version}
                </h2>
                <div className="whitespace-pre-line text-sm text-slate-600">
                  {doc.content}
                </div>
              </div>
            ))}
          </div>

          <p className="mb-4 text-sm text-slate-500">
            Você pode ler a versão completa a qualquer momento em{" "}
            <Link
              to="/termos"
              target="_blank"
              className="text-indigo-600 underline">
              Política de Privacidade e Termos de Uso
            </Link>
            .
          </p>

          {error && (
            <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <label className="mb-4 flex items-start gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={checked}
              onChange={(e) => setChecked(e.target.checked)}
              className="mt-1"
            />
            Li e concordo com o(s) documento(s) acima.
          </label>

          <Button
            onClick={handleAccept}
            disabled={!checked || isSubmitting}
            className="primary-action w-full">
            {isSubmitting ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" /> Enviando...
              </>
            ) : (
              "Concordo e quero continuar"
            )}
          </Button>
        </div>
      </div>
    );
  }

  return <>{children}</>;
};