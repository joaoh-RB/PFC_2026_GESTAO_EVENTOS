import React, { useEffect, useState } from "react";
import { api } from "@/services/api";
import { AlertCircle, FileText, Loader2 } from "lucide-react";
import type { LegalDocument } from "@/types/legalDocument";
import { Brand } from "@/components/Brand";

const typeLabels: Record<string, string> = {
  PoliticaPrivacidade: "Política de Privacidade",
  TermosDeUso: "Termos de Uso",
};

export const Terms: React.FC = () => {
  const [loading, setLoading] = useState(true);
  const [documents, setDocuments] = useState<LegalDocument[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .get<LegalDocument[]>("/legal-documents/current")
      .then((res) => setDocuments(res.data))
      .catch(() =>
        setError("Não foi possível carregar os documentos no momento."),
      )
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="min-h-screen bg-[linear-gradient(135deg,#17104f_0%,#20166b_35%,#f6f7fb_35%,#f6f7fb_100%)] p-4 py-10">
      <div className="mx-auto mb-7 w-full max-w-3xl">
        <Brand inverse />
      </div>
      <div className="mx-auto w-full max-w-3xl rounded-2xl border border-[#e2e4e9] bg-white p-8 shadow-[0_18px_50px_rgba(20,15,65,0.14)]">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-cyan-50 text-[#12a7d4]">
            <FileText className="h-6 w-6" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">
            Política de Privacidade e Termos de Uso
          </h1>
          <p className="mt-1 text-sm text-slate-600">
            Documentos vigentes do SYMPLOSIO.
          </p>
        </div>

        {error && (
          <div className="mb-6 flex items-center gap-2 rounded-lg bg-red-50 p-4 text-sm text-red-700">
            <AlertCircle className="h-5 w-5 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {loading ? (
          <div className="flex justify-center py-16">
            <Loader2 className="h-8 w-8 animate-spin text-[#12a7d4]" />
          </div>
        ) : (
          !error &&
          (documents.length === 0 ? (
            <p className="text-center text-sm text-slate-500">
              Nenhum documento publicado no momento.
            </p>
          ) : (
            documents.map((doc) => (
              <section key={doc.id} className="mb-10 last:mb-0">
                <div className="mb-3 flex items-center gap-2 border-b border-[#e2e4e9] pb-3">
                  <FileText className="h-5 w-5 shrink-0 text-[#12a7d4]" />
                  <h2 className="text-lg font-semibold text-slate-800">
                    {typeLabels[doc.type] ?? doc.type}
                  </h2>
                  <span className="text-xs text-slate-400">
                    versão {doc.version}
                  </span>
                </div>
                <div className="whitespace-pre-line text-sm leading-6 text-slate-700">
                  {doc.content}
                </div>
              </section>
            ))
          ))
        )}
      </div>
    </div>
  );
};