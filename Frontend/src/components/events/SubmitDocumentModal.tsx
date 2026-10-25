import React, {
  useState,
  useRef,
  useMemo,
  useEffect,
  useCallback,
} from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Attachment,
  AttachmentAction,
  AttachmentActions,
  AttachmentContent,
  AttachmentDescription,
  AttachmentMedia,
  AttachmentTitle,
} from "@/components/ui/attachment";
import {
  FileUp,
  Loader2,
  CheckCircle2,
  AlertCircle,
  FileText,
  UploadCloud,
  X,
  Download,
  Trash2,
} from "lucide-react";
import type { EventItem } from "./EventList";
import { EventGroupPanel } from "./EventGroupPanel";
import api from "@/services/api";
import { DeleteConfirmation } from "@/components/ui/deleteConfirm";

export type DocumentStatus = number | string;

export interface EventDocumentDto {
  id: string;
  groupId: string;
  senderId: string;
  senderName: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  submittedAt: string;
  status: DocumentStatus;
  feedback?: string | null;
  reviewedByName?: string | null;
  reviewedAt?: string | null;
}

const submitDocument = async (eventId: string, file: File): Promise<void> => {
  const formData = new FormData();
  formData.append("file", file);
  await api.post(`/eventdocuments/${eventId}`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
};

const fetchEventDocuments = async (
  eventId: string,
): Promise<EventDocumentDto[]> => {
  const response = await api.get<EventDocumentDto[]>(
    `/eventdocuments/${eventId}`,
  );
  return response.data;
};

const downloadDocument = async (
  documentId: string,
  fileName: string,
): Promise<void> => {
  const response = await api.get(`/eventdocuments/${documentId}/download`, {
    responseType: "blob",
  });
  const url = window.URL.createObjectURL(new Blob([response.data]));
  const link = document.createElement("a");
  link.href = url;
  link.setAttribute("download", fileName);
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
};

const deleteDocument = async (documentId: string): Promise<void> => {
  await api.delete(`/eventdocuments/${documentId}`);
};

export interface SubmitDocumentModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  event: EventItem | null;
}

export const SubmitDocumentModal: React.FC<SubmitDocumentModalProps> = ({
  isOpen,
  onOpenChange,
  event,
}) => {
  const [file, setFile] = useState<File | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const [documents, setDocuments] = useState<EventDocumentDto[]>([]);
  const [isLoadingDocuments, setIsLoadingDocuments] = useState(false);
  const [documentsError, setDocumentsError] = useState<string | null>(null);
  const [isDownloadingId, setIsDownloadingId] = useState<string | null>(null);
  const [isDeletingId, setIsDeletingId] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [hasGroup, setHasGroup] = useState(false);

  const previewUrl = useMemo(() => {
    if (!file || !file.type.startsWith("image/")) return null;
    return URL.createObjectURL(file);
  }, [file]);

  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  const formatFileSizeMB = (bytes: number): string => {
    if (!bytes || bytes <= 0) return "0.00 MB";
    const mb = bytes / (1024 * 1024);
    if (mb < 0.01) {
      return `${mb.toFixed(3)} MB`;
    }
    return `${mb.toFixed(2)} MB`;
  };

  const getDocId = (doc: EventDocumentDto): string =>
    doc.id || (doc as unknown as { Id?: string }).Id || "";

  const getDocFileName = (doc: EventDocumentDto): string =>
    doc.originalFileName ||
    (doc as unknown as { OriginalFileName?: string }).OriginalFileName ||
    "Documento";

  const getDocSizeBytes = (doc: EventDocumentDto): number =>
    doc.fileSizeBytes ??
    (doc as unknown as { FileSizeBytes?: number }).FileSizeBytes ??
    0;

  const getStatusLabel = (status: DocumentStatus): string => {
    const numericStatus = Number(status);
    if (numericStatus === 0) return "Pendente";
    if (numericStatus === 1) return "Aprovado";
    if (numericStatus === 2) return "Reprovado";
    if (status === "Pending") return "Pendente";
    if (status === "Approved") return "Aprovado";
    if (status === "Rejected") return "Reprovado";
    return "Status não informado";
  };

  const getStatusBadgeClassName = (status: DocumentStatus): string => {
    const label = getStatusLabel(status);
    if (label === "Pendente") return "bg-amber-100 text-amber-700 border-amber-200";
    if (label === "Aprovado") return "bg-emerald-100 text-emerald-700 border-emerald-200";
    if (label === "Reprovado") return "bg-red-100 text-red-700 border-red-200";
    return "bg-slate-100 text-slate-700 border-slate-200";
  };

  const getFileFormatLabel = (
    fileName?: string,
    contentType?: string,
  ): string => {
    if (fileName && fileName.includes(".")) {
      const ext = fileName.split(".").pop();
      if (ext && ext.trim().length > 0 && ext.length <= 10) {
        return ext.toUpperCase();
      }
    }

    if (contentType) {
      const mime = contentType.toLowerCase();
      if (mime.includes("wordprocessingml") || mime.includes("msword"))
        return "DOCX";
      if (mime.includes("spreadsheetml") || mime.includes("ms-excel"))
        return "XLSX";
      if (mime.includes("presentationml") || mime.includes("ms-powerpoint"))
        return "PPTX";
      if (mime.includes("pdf")) return "PDF";
      if (mime.includes("jpeg") || mime.includes("jpg")) return "JPG";
      if (mime.includes("png")) return "PNG";
      if (mime.includes("webp")) return "WEBP";
      if (mime.includes("csv")) return "CSV";
      if (mime.includes("plain")) return "TXT";
      if (mime.includes("zip")) return "ZIP";

      const sub = mime.split("/")[1];
      if (sub && sub.length <= 6 && !sub.includes("vnd.")) {
        return sub.toUpperCase();
      }
    }

    return "ARQUIVO";
  };

  const loadDocuments = useCallback(async () => {
    if (!event?.id) return;
    setIsLoadingDocuments(true);
    setDocumentsError(null);
    try {
      const data = await fetchEventDocuments(event.id);
      setDocuments(Array.isArray(data) ? data : []);
    } catch (err: unknown) {
      const errorObj = err as {
        response?: { data?: { message?: string }; status?: number };
        message?: string;
      };
      if (errorObj?.response?.status === 404) {
        setDocuments([]);
      } else {
        setDocumentsError(
          errorObj?.response?.data?.message ||
            errorObj?.message ||
            "Não foi possível carregar os documentos cadastrados.",
        );
      }
    } finally {
      setIsLoadingDocuments(false);
    }
  }, [event]);

  useEffect(() => {
    if (isOpen && event?.id) {
      queueMicrotask(() => {
        void loadDocuments();
      });
    }
  }, [isOpen, event?.id, loadDocuments]);

  useEffect(() => {
    if (hasGroup) {
      queueMicrotask(() => {
        void loadDocuments();
      });
    }
  }, [hasGroup, loadDocuments]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      setFile(e.target.files[0]);
      setError(null);
    } else {
      setFile(null);
    }
  };

  const handleRemoveFile = () => {
    setFile(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const handleClose = () => {
    if (!isSubmitting && !isDeletingId && !isDownloadingId) {
      setFile(null);
      setError(null);
      setIsSuccess(false);
      setDocumentsError(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
      onOpenChange(false);
    }
  };

  const handleDownload = async (doc: EventDocumentDto) => {
    const docId = getDocId(doc);
    const fileName = getDocFileName(doc);
    if (!docId) return;

    setIsDownloadingId(docId);
    setError(null);
    try {
      await downloadDocument(docId, fileName);
    } catch (err: unknown) {
      const errorObj = err as {
        response?: { data?: { message?: string } };
        message?: string;
      };
      setError(
        errorObj?.response?.data?.message ||
          errorObj?.message ||
          "Erro ao baixar o documento.",
      );
    } finally {
      setIsDownloadingId(null);
    }
  };

  const handleDelete = async (doc: EventDocumentDto) => {
    const docId = getDocId(doc);
    if (!docId) return;

    setIsDeletingId(docId);
    setError(null);
    try {
      await deleteDocument(docId);
      await loadDocuments();
    } catch (err: unknown) {
      const errorObj = err as {
        response?: { data?: { message?: string } };
        message?: string;
      };
      setError(
        errorObj?.response?.data?.message ||
          errorObj?.message ||
          "Erro ao excluir o documento.",
      );
    } finally {
      setIsDeletingId(null);
    }
  };

  const getApiErrorMessage = (
    err: unknown,
    fallbackMessage: string,
  ): string => {
    const errorObj = err as {
      response?: {
        data?:
          | { message?: string; error?: string; errors?: Record<string, string | string[]> }
          | string;
      };
      message?: string;
    };

    const data = errorObj?.response?.data;
    if (typeof data === "string" && data.trim().length > 0) {
      return data;
    }

    if (data && typeof data === "object") {
      if (data.message && data.message.trim().length > 0) {
        return data.message;
      }
      if (data.error && data.error.trim().length > 0) {
        return data.error;
      }
      if (data.errors && typeof data.errors === "object") {
        const firstError = Object.values(data.errors)
          .flatMap((value) => (Array.isArray(value) ? value : [value]))
          .find((value) => typeof value === "string" && value.trim().length > 0);
        if (firstError) {
          return firstError;
        }
      }
    }

    if (errorObj?.message && !errorObj.message.toLowerCase().includes("status code")) {
      return errorObj.message;
    }

    return fallbackMessage;
  };

  const handleSubmit = async (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!event || !file) {
      setError("Selecione um arquivo para enviar.");
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      await submitDocument(event.id, file);
      setIsSuccess(true);
      setFile(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
      await loadDocuments();
      setTimeout(() => {
        setIsSuccess(false);
      }, 3000);
    } catch (err: unknown) {
      setError(getApiErrorMessage(err, "Erro ao enviar o documento."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent className="w-[calc(100vw-1rem)] sm:w-full sm:max-w-[760px] max-h-[92vh] flex flex-col p-0 overflow-hidden gap-0">
        <DialogHeader className="px-6 pt-6 pb-4 pr-12 min-w-0 shrink-0 border-b border-slate-100">
          <DialogTitle className="truncate text-lg font-semibold text-slate-800">
            Documentos do Evento
          </DialogTitle>
          <DialogDescription className="break-words text-sm text-slate-500">
            {event
              ? `Envio e consulta de documentos para o evento: ${event.name}`
              : "Selecione ou consulte os arquivos solicitados para o evento."}
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit}
          className="flex flex-col flex-1 min-h-0 w-full min-w-0 max-w-full m-0 p-0 overflow-hidden">
          <div className="flex-1 overflow-y-auto px-6 py-4 space-y-5 w-full min-w-0 max-w-full">
            {isSuccess && (
              <div className="flex items-center gap-2 p-3 text-sm text-emerald-700 bg-emerald-50 rounded-lg border border-emerald-200 w-full min-w-0">
                <CheckCircle2 className="h-4 w-4 shrink-0 text-emerald-600" />
                <span className="min-w-0 flex-1 font-medium">
                  Documento enviado com sucesso!
                </span>
              </div>
            )}

            {event && (
              <EventGroupPanel
                key={event.id}
                eventId={event.id}
                onGroupChange={setHasGroup}
              />
            )}

            {hasGroup && (
              <>
                <div className="space-y-2.5 w-full min-w-0">
                  <div className="flex items-center justify-between">
                    <Label className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
                      Documentos Cadastrados
                      {documents.length > 0 && (
                        <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-700">
                          {documents.length}
                        </span>
                      )}
                    </Label>
                    {isLoadingDocuments && (
                      <span className="flex items-center text-xs text-slate-400 gap-1">
                        <Loader2 className="h-3 w-3 animate-spin text-[#109bc6]" />
                        Carregando...
                      </span>
                    )}
                  </div>

                  {documentsError && (
                    <div className="flex items-center gap-2 p-2.5 text-xs text-amber-700 bg-amber-50 rounded-lg border border-amber-200">
                      <AlertCircle className="h-4 w-4 shrink-0" />
                      <span className="flex-1">{documentsError}</span>
                    </div>
                  )}

                  {isLoadingDocuments && documents.length === 0 ? (
                    <div className="flex items-center justify-center p-6 text-slate-400 gap-2 border rounded-xl bg-slate-50/50">
                      <Loader2 className="h-4 w-4 animate-spin text-[#109bc6]" />
                      <span className="text-xs">
                        Carregando documentos cadastrados...
                      </span>
                    </div>
                  ) : documents.length === 0 ? (
                    <div className="flex flex-col items-center justify-center py-5 px-4 border border-dashed rounded-xl bg-slate-50/40 text-slate-400 text-center gap-1">
                      <FileText className="h-5 w-5 text-slate-300" />
                      <p className="text-xs font-medium text-slate-500">
                        Nenhum documento cadastrado
                      </p>
                      <p className="text-[11px] text-slate-400">
                        Selecione um arquivo abaixo para enviar.
                      </p>
                    </div>
                  ) : (
                    <div className="space-y-2 max-h-48 overflow-y-auto pr-1">
                      {documents.map((doc) => {
                        const docId = getDocId(doc);
                        const fileName = getDocFileName(doc);
                        const sizeBytes = getDocSizeBytes(doc);
                        const ext = getFileFormatLabel(
                          fileName,
                          doc.contentType,
                        );

                        return (
                          <Attachment
                            key={docId}
                            className="w-full max-w-full min-w-0 shrink flex-nowrap items-center justify-between border rounded-xl bg-slate-50/50 p-2.5 overflow-hidden hover:bg-slate-50 transition-colors">
                            <div className="flex items-center gap-3 min-w-0 flex-1 overflow-hidden">
                              <AttachmentMedia className="w-9 h-9 rounded-lg shrink-0 bg-white border border-slate-200 text-[#109bc6]">
                                <FileText className="h-4.5 w-4.5" />
                              </AttachmentMedia>
                              <AttachmentContent className="min-w-0 flex-1 overflow-hidden">
                                <AttachmentTitle
                                  className="text-xs sm:text-sm font-medium text-slate-800 truncate block w-full max-w-full"
                                  title={fileName}>
                                  {fileName}
                                </AttachmentTitle>
                                <AttachmentDescription className="text-[11px] text-slate-500 truncate block w-full max-w-full">
                                  {ext} • {formatFileSizeMB(sizeBytes)}
                                  {doc.senderName ? ` • ${doc.senderName}` : ""}
                                </AttachmentDescription>
                                <div className="mt-1">
                                  <span
                                    className={`inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-semibold ${getStatusBadgeClassName(doc.status)}`}>
                                    {getStatusLabel(doc.status) === "Pendente"
                                      ? "Pendente de aprovação"
                                      : getStatusLabel(doc.status)}
                                  </span>
                                </div>
                                {getStatusLabel(doc.status) !== "Pendente" && (
                                  <div className="mt-1.5 rounded-md border border-slate-200 bg-white/80 px-2 py-1.5 text-[11px] space-y-1">
                                    <p className="text-slate-700 break-words">
                                      <span className="font-semibold">Motivo:</span>{" "}
                                      {doc.feedback && doc.feedback.trim().length > 0
                                        ? doc.feedback
                                        : "Sem feedback informado."}
                                    </p>
                                    {(doc.reviewedByName || doc.reviewedAt) && (
                                      <p className="text-slate-500 break-words">
                                        {doc.reviewedByName
                                          ? `Avaliado por ${doc.reviewedByName}`
                                          : "Avaliação registrada"}
                                        {doc.reviewedAt
                                          ? ` em ${new Date(doc.reviewedAt).toLocaleString("pt-BR")}`
                                          : ""}
                                      </p>
                                    )}
                                  </div>
                                )}
                              </AttachmentContent>
                            </div>
                            <AttachmentActions className="shrink-0 ml-2 flex items-center gap-1">
                              <AttachmentAction
                                type="button"
                                variant="ghost"
                                size="icon-xs"
                                aria-label="Baixar documento"
                                title="Baixar documento"
                                className="h-8 w-8 text-slate-500 hover:text-[#109bc6] hover:bg-sky-50 rounded-lg cursor-pointer"
                                disabled={
                                  isDownloadingId === docId || isSubmitting
                                }
                                onClick={() => handleDownload(doc)}>
                                {isDownloadingId === docId ? (
                                  <Loader2 className="h-4 w-4 animate-spin text-[#109bc6]" />
                                ) : (
                                  <Download className="h-4 w-4" />
                                )}
                              </AttachmentAction>
                              <DeleteConfirmation
                                descriptionText={`Tem certeza que deseja excluir o documento "${fileName}"?`}
                                confirmationText="Excluir"
                                onDelete={() => handleDelete(doc)}>
                                <AttachmentAction
                                  type="button"
                                  variant="ghost"
                                  size="icon-xs"
                                  aria-label="Excluir documento"
                                  title="Excluir documento"
                                  className="h-8 w-8 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg cursor-pointer"
                                  disabled={
                                    isDeletingId === docId || isSubmitting
                                  }>
                                  {isDeletingId === docId ? (
                                    <Loader2 className="h-4 w-4 animate-spin text-red-500" />
                                  ) : (
                                    <Trash2 className="h-4 w-4" />
                                  )}
                                </AttachmentAction>
                              </DeleteConfirmation>
                            </AttachmentActions>
                          </Attachment>
                        );
                      })}
                    </div>
                  )}
                </div>

                <div className="space-y-2 w-full min-w-0 border-t border-slate-100 pt-4">
                  {error && (
                    <div className="flex items-start gap-2 p-3 text-sm text-red-700 bg-red-50 rounded-lg border border-red-200">
                      <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" />
                      <span className="flex-1 break-words">{error}</span>
                    </div>
                  )}
                  <Label
                    htmlFor="document-upload"
                    className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
                    Enviar Novo Documento
                  </Label>
                  <input
                    ref={fileInputRef}
                    id="document-upload"
                    type="file"
                    onChange={handleFileChange}
                    disabled={isSubmitting}
                    className="hidden"
                  />

                  {!file ? (
                    <Attachment
                      state="idle"
                      className="w-full max-w-full min-w-0 shrink cursor-pointer hover:bg-slate-50 transition-colors p-5 justify-center items-center flex-col text-center border-dashed border-2 rounded-xl overflow-hidden"
                      onClick={() => fileInputRef.current?.click()}
                      onDragOver={(e) => e.preventDefault()}
                      onDrop={(e) => {
                        e.preventDefault();
                        if (isSubmitting) return;
                        if (
                          e.dataTransfer.files &&
                          e.dataTransfer.files.length > 0
                        ) {
                          setFile(e.dataTransfer.files[0]);
                          setError(null);
                        }
                      }}>
                      <AttachmentMedia className="w-10 h-10 rounded-full bg-slate-100 text-[#109bc6] mb-2 shrink-0">
                        <UploadCloud className="h-5 w-5" />
                      </AttachmentMedia>
                      <AttachmentContent className="space-y-1 w-full min-w-0 max-w-full overflow-hidden">
                        <AttachmentTitle className="text-xs sm:text-sm font-semibold text-slate-800 truncate block w-full max-w-full">
                          Clique para selecionar ou arraste o arquivo
                        </AttachmentTitle>
                        <AttachmentDescription className="text-xs text-slate-500 truncate block w-full max-w-full">
                          Formatos aceitos: PDF, DOCX, PNG, JPG, etc.
                        </AttachmentDescription>
                      </AttachmentContent>
                    </Attachment>
                  ) : (
                    <Attachment
                      state={
                        isSubmitting ? "uploading" : error ? "error" : "done"
                      }
                      className="w-full max-w-full min-w-0 shrink flex-nowrap items-center justify-between border rounded-xl bg-slate-50/50 p-3 overflow-hidden">
                      <div className="flex items-center gap-3 min-w-0 flex-1 overflow-hidden">
                        <AttachmentMedia
                          variant={
                            file.type.startsWith("image/") ? "image" : "icon"
                          }
                          className="w-10 h-10 rounded-lg shrink-0 bg-white border">
                          {previewUrl ? (
                            <img
                              src={previewUrl}
                              alt={file.name}
                              className="h-full w-full object-cover"
                            />
                          ) : (
                            <FileText className="h-5 w-5 text-[#109bc6]" />
                          )}
                        </AttachmentMedia>
                        <AttachmentContent className="min-w-0 flex-1 overflow-hidden">
                          <AttachmentTitle
                            className="text-sm font-medium text-slate-800 truncate block w-full max-w-full"
                            title={file.name}>
                            {file.name}
                          </AttachmentTitle>
                          <AttachmentDescription className="text-xs text-slate-500 truncate block w-full max-w-full">
                            {getFileFormatLabel(file.name, file.type)} •{" "}
                            {formatFileSizeMB(file.size)}
                          </AttachmentDescription>
                        </AttachmentContent>
                      </div>
                      <AttachmentActions className="shrink-0 ml-2">
                        <AttachmentAction
                          type="button"
                          variant="ghost"
                          size="icon-xs"
                          aria-label="Remover arquivo"
                          className="h-8 w-8 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg cursor-pointer"
                          disabled={isSubmitting}
                          onClick={handleRemoveFile}>
                          <X className="h-4 w-4" />
                        </AttachmentAction>
                      </AttachmentActions>
                    </Attachment>
                  )}
                </div>
              </>
            )}
          </div>

          <DialogFooter className="m-0 shrink-0 border-t border-slate-100 bg-slate-50/80 px-6 py-4 flex flex-col-reverse sm:flex-row sm:justify-end gap-3 w-full rounded-b-xl">
            <Button
              type="button"
              variant="outline"
              onClick={handleClose}
              disabled={isSubmitting || !!isDeletingId || !!isDownloadingId}
              className="w-full sm:w-auto cursor-pointer">
              Cancelar
            </Button>
            <Button
              type="submit"
              className="w-full sm:w-auto primary-action cursor-pointer"
              disabled={!file || isSubmitting || !hasGroup}>
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Enviando...
                </>
              ) : (
                <>
                  <FileUp className="mr-2 h-4 w-4" />
                  Enviar documento
                </>
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};