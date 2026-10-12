import React, { useState, useRef, useMemo, useEffect } from "react";
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
} from "lucide-react";
import type { EventItem } from "./EventList";
import api from "@/services/api";

const submitDocument = async (eventId: string, file: File): Promise<void> => {
  const formData = new FormData();
  formData.append("file", file);
  await api.post(`/eventdocuments/${eventId}`, formData, {headers: { "Content-Type": "multipart/form-data" }});
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
  const fileInputRef = useRef<HTMLInputElement>(null);

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

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const getFileFormatLabel = (file: File): string => {
    if (file.name && file.name.includes(".")) {
      const ext = file.name.split(".").pop();
      if (ext && ext.trim().length > 0 && ext.length <= 10) {
        return ext.toUpperCase();
      }
    }

    if (file.type) {
      const mime = file.type.toLowerCase();
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
    if (!isSubmitting) {
      setFile(null);
      setError(null);
      setIsSuccess(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
      onOpenChange(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
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
      setTimeout(() => {
        handleClose();
      }, 1200);
    } catch (err: unknown) {
      const errorObj = err as {
        response?: { data?: { message?: string } };
        message?: string;
      };
      setError(
        errorObj?.response?.data?.message ||
          errorObj?.message ||
          "Erro ao enviar o documento.",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent className="sm:max-w-[480px] p-0 overflow-hidden gap-0">
        <DialogHeader className="px-6 pt-6 pb-3 pr-10 min-w-0">
          <DialogTitle className="truncate text-lg font-semibold text-slate-800">
            Submeter Documento
          </DialogTitle>
          <DialogDescription className="break-words text-sm text-slate-500">
            {event
              ? `Envio de documentos para o evento: ${event.name}`
              : "Selecione o arquivo solicitado para o evento."}
          </DialogDescription>
        </DialogHeader>

        {isSuccess ? (
          <div className="flex flex-col items-center justify-center p-8 text-center text-emerald-600 gap-2">
            <CheckCircle2 className="h-10 w-10 text-emerald-600" />
            <p className="font-medium">Documento enviado com sucesso!</p>
          </div>
        ) : (
          <form
            onSubmit={handleSubmit}
            className="flex flex-col w-full min-w-0 max-w-full m-0 p-0 overflow-hidden">
            <div className="px-6 pb-6 space-y-4 w-full min-w-0 max-w-full">
              {error && (
                <div className="flex items-center gap-2 p-3 text-sm text-red-600 bg-red-50 rounded-lg border border-red-200 w-full min-w-0">
                  <AlertCircle className="h-4 w-4 shrink-0" />
                  <span className="min-w-0 flex-1 break-words">{error}</span>
                </div>
              )}

              <div className="space-y-2 w-full min-w-0 max-w-full">
                <Label htmlFor="document-upload">Documento</Label>
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
                    className="w-full max-w-full min-w-0 shrink cursor-pointer hover:bg-slate-50 transition-colors p-6 justify-center items-center flex-col text-center border-dashed border-2 rounded-xl overflow-hidden"
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
                    <AttachmentMedia className="w-12 h-12 rounded-full bg-slate-100 text-[#109bc6] mb-2 shrink-0">
                      <UploadCloud className="h-6 w-6" />
                    </AttachmentMedia>
                    <AttachmentContent className="space-y-1 w-full min-w-0 max-w-full overflow-hidden">
                      <AttachmentTitle className="text-sm font-semibold text-slate-800 truncate block w-full max-w-full">
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
                          {getFileFormatLabel(file)} •{" "}
                          {formatFileSize(file.size)}
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
            </div>

            <DialogFooter className="m-0 border-t border-slate-100 bg-slate-50/80 px-6 py-4 flex flex-col-reverse sm:flex-row sm:justify-end gap-3 w-full rounded-b-xl">
              <Button
                type="button"
                variant="outline"
                onClick={handleClose}
                disabled={isSubmitting}
                className="w-full sm:w-auto cursor-pointer">
                Cancelar
              </Button>
              <Button
                type="submit"
                className="w-full sm:w-auto primary-action cursor-pointer"
                disabled={!file || isSubmitting}>
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
        )}
      </DialogContent>
    </Dialog>
  );
};
