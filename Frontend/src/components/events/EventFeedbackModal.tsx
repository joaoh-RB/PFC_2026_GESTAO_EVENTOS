import React, { useState, useEffect, useCallback } from "react";
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
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import {
  Loader2,
  CheckCircle2,
  AlertCircle,
  MessageSquarePlus,
  Star,
} from "lucide-react";
import type { EventItem } from "./EventList";
import api from "@/services/api";
import { formatDateForShort } from "@/utils/format";

export interface EventFeedbackDto {
  id: string;
  eventId: string;
  studentId: string;
  studentName: string;
  courseName: string;
  rating: number;
  feedbackText: string;
  createdAt: string;
}

export interface EventFeedbackModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  event: EventItem | null;
}

const RATINGS = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

export const EventFeedbackModal: React.FC<EventFeedbackModalProps> = ({
  isOpen,
  onOpenChange,
  event,
}) => {
  const [existingFeedback, setExistingFeedback] =
    useState<EventFeedbackDto | null>(null);
  const [isLoadingFeedback, setIsLoadingFeedback] = useState(false);
  const [rating, setRating] = useState<number | null>(null);
  const [feedbackText, setFeedbackText] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const loadMyFeedback = useCallback(async () => {
    if (!event?.id) return;
    setIsLoadingFeedback(true);
    setError(null);
    try {
      const response = await api.get<EventFeedbackDto>(
        `/events/${event.id}/feedback/mine`,
      );
      setExistingFeedback(response.data);
    } catch (err: unknown) {
      const errorObj = err as {
        response?: { data?: { message?: string }; status?: number };
        message?: string;
      };
      if (errorObj?.response?.status === 404) {
        setExistingFeedback(null);
      } else {
        setError(
          errorObj?.response?.data?.message ||
            errorObj?.message ||
            "Não foi possível carregar as informações do seu feedback.",
        );
      }
    } finally {
      setIsLoadingFeedback(false);
    }
  }, [event]);

  useEffect(() => {
    if (isOpen && event?.id) {
      loadMyFeedback();
    } else if (!isOpen) {
      setExistingFeedback(null);
      setRating(null);
      setFeedbackText("");
      setError(null);
      setIsSuccess(false);
    }
  }, [isOpen, event?.id, loadMyFeedback]);

  const handleClose = () => {
    if (!isSubmitting) {
      setError(null);
      setIsSuccess(false);
      onOpenChange(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!event) return;

    if (rating === null || rating < 0 || rating > 10) {
      setError("Selecione uma nota de 0 a 10 para avaliar o evento.");
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const response = await api.post<EventFeedbackDto>(
        `/events/${event.id}/feedback`,
        {
          rating,
          feedbackText: feedbackText.trim(),
        },
      );
      setExistingFeedback(response.data);
      setIsSuccess(true);
    } catch (err: unknown) {
      const errorObj = err as {
        response?: { data?: { message?: string } };
        message?: string;
      };
      setError(
        errorObj?.response?.data?.message ||
          errorObj?.message ||
          "Erro ao enviar o feedback.",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent className="sm:max-w-[540px] max-h-[90vh] flex flex-col p-0 overflow-hidden gap-0">
        <DialogHeader className="px-6 pt-6 pb-4 pr-12 min-w-0 shrink-0 border-b border-slate-100">
          <DialogTitle className="truncate text-lg font-semibold text-slate-800">
            Feedback do Evento
          </DialogTitle>
          <DialogDescription className="break-words text-sm text-slate-500">
            {event
              ? `Avaliação do evento: ${event.name}`
              : "Envie ou consulte a sua avaliação sobre o evento."}
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit}
          className="flex flex-col flex-1 min-h-0 w-full min-w-0 max-w-full m-0 p-0 overflow-hidden">
          <div className="flex-1 overflow-y-auto px-6 py-4 space-y-5 w-full min-w-0 max-w-full">
            {error && (
              <div className="flex items-center gap-2 p-3 text-sm text-red-600 bg-red-50 rounded-lg border border-red-200 w-full min-w-0">
                <AlertCircle className="h-4 w-4 shrink-0" />
                <span className="min-w-0 flex-1 break-words">{error}</span>
              </div>
            )}

            {isSuccess && (
              <div className="flex items-center gap-2 p-3 text-sm text-emerald-700 bg-emerald-50 rounded-lg border border-emerald-200 w-full min-w-0">
                <CheckCircle2 className="h-4 w-4 shrink-0 text-emerald-600" />
                <span className="min-w-0 flex-1 font-medium">
                  Feedback enviado com sucesso!
                </span>
              </div>
            )}

            {isLoadingFeedback ? (
              <div className="flex items-center justify-center p-8 text-slate-400 gap-2 border rounded-xl bg-slate-50/50">
                <Loader2 className="h-5 w-5 animate-spin text-[#109bc6]" />
                <span className="text-xs">Carregando seu feedback...</span>
              </div>
            ) : existingFeedback ? (
              <div className="space-y-4">
                <div className="rounded-xl border border-slate-200 bg-slate-50/60 p-4 space-y-4">
                  <div className="flex items-center justify-between gap-2 flex-wrap">
                    <div className="flex items-center gap-2">
                      <Star className="h-4 w-4 text-amber-500 fill-amber-500" />
                      <span className="text-sm font-semibold text-slate-800">
                        Sua nota:
                      </span>
                      <Badge variant="secondary" className="text-sm font-semibold">
                        {existingFeedback.rating} / 10
                      </Badge>
                    </div>
                    <span className="text-xs text-slate-500">
                      Enviado em {formatDateForShort(existingFeedback.createdAt)}
                    </span>
                  </div>

                  <div className="space-y-1.5">
                    <Label className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
                      Seu comentário
                    </Label>
                    <Textarea
                      value={
                        existingFeedback.feedbackText ||
                        "Nenhum comentário informado."
                      }
                      disabled
                      rows={4}
                      className="resize-none bg-white text-slate-700 disabled:opacity-90"
                    />
                  </div>
                </div>

                <p className="text-xs text-slate-500 text-center">
                  O feedback já foi registrado e não pode ser alterado.
                </p>
              </div>
            ) : (
              <div className="space-y-5">
                <div className="space-y-2.5">
                  <Label
                    required
                    className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
                    Nota do Evento (0 a 10)
                  </Label>
                  <div className="grid grid-cols-6 sm:grid-cols-11 gap-1.5">
                    {RATINGS.map((value) => {
                      const isSelected = rating === value;
                      return (
                        <Button
                          key={value}
                          type="button"
                          variant={isSelected ? "default" : "outline"}
                          size="sm"
                          onClick={() => {
                            setRating(value);
                            setError(null);
                          }}
                          disabled={isSubmitting}
                          className={`h-9 px-0 font-semibold cursor-pointer ${
                            isSelected ? "primary-action" : ""
                          }`}>
                          {value}
                        </Button>
                      );
                    })}
                  </div>
                </div>

                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <Label
                      htmlFor="feedback-text"
                      className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
                      Comentário
                    </Label>
                    <span className="text-xs text-slate-400">
                      {feedbackText.length}/1000
                    </span>
                  </div>
                  <Textarea
                    id="feedback-text"
                    value={feedbackText}
                    onChange={(e) => setFeedbackText(e.target.value)}
                    maxLength={1000}
                    rows={4}
                    disabled={isSubmitting}
                    className="resize-none"
                    placeholder="Conte como foi sua experiência neste evento..."
                  />
                </div>
              </div>
            )}
          </div>

          <DialogFooter className="m-0 shrink-0 border-t border-slate-100 bg-slate-50/80 px-6 py-4 flex flex-col-reverse sm:flex-row sm:justify-end gap-3 w-full rounded-b-xl">
            <Button
              type="button"
              variant="outline"
              onClick={handleClose}
              disabled={isSubmitting}
              className="w-full sm:w-auto cursor-pointer">
              {existingFeedback ? "Fechar" : "Cancelar"}
            </Button>
            {!existingFeedback && !isLoadingFeedback && (
              <Button
                type="submit"
                className="w-full sm:w-auto primary-action cursor-pointer"
                disabled={rating === null || isSubmitting}>
                {isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Enviando...
                  </>
                ) : (
                  <>
                    <MessageSquarePlus className="mr-2 h-4 w-4" />
                    Enviar feedback
                  </>
                )}
              </Button>
            )}
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};

