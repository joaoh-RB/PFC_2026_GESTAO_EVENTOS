import { useCallback, useEffect, useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { AlertCircle, CheckCircle2, Loader2 } from "lucide-react";
import api from "@/services/api";
import { formatDateForShort } from "@/utils/format";
import type { EventItem } from "./EventList";
import type { EventFeedbackDto } from "./EventFeedbackModal";

interface AttendanceRegistration {
  studentId: string;
  studentName: string;
  courseName: string;
  attended: boolean;
  attendedAt: string | null;
  method: number | null;
}

interface AttendanceModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  event: EventItem | null;
}

const methodLabel: Record<number, string> = {
  1: "pelo código",
  2: "manual",
};

const getErrorMessage = (err: unknown, fallback: string): string => {
  const errorObj = err as {
    response?: { data?: { message?: string } };
    message?: string;
  };
  return errorObj?.response?.data?.message || errorObj?.message || fallback;
};

export const AttendanceModal: React.FC<AttendanceModalProps> = ({
  isOpen,
  onOpenChange,
  event,
}) => {
  const [code, setCode] = useState<string | null>(null);
  const [registrations, setRegistrations] = useState<AttendanceRegistration[]>(
    [],
  );
  const [feedbacks, setFeedbacks] = useState<EventFeedbackDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [workingId, setWorkingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    if (!event) return;
    setIsLoading(true);
    setError(null);
    try {
      const [codeRes, listRes, feedbacksRes] = await Promise.all([
        api.get<{ code: string }>(`/events/${event.id}/attendance/code`),
        api.get<AttendanceRegistration[]>(`/events/${event.id}/attendance`),
        api.get<EventFeedbackDto[]>(`/events/${event.id}/feedbacks`),
      ]);
      setCode(codeRes.data.code);
      setRegistrations(listRes.data);
      setFeedbacks(Array.isArray(feedbacksRes.data) ? feedbacksRes.data : []);
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Não foi possível carregar a presença."));
    } finally {
      setIsLoading(false);
    }
  }, [event]);

  useEffect(() => {
    if (isOpen) loadData();
  }, [isOpen, loadData]);

  const handleToggle = async (registration: AttendanceRegistration) => {
    if (!event) return;
    setWorkingId(registration.studentId);
    setError(null);
    try {
      const url = `/events/${event.id}/attendance/${registration.studentId}`;
      if (registration.attended) {
        await api.delete(url);
      } else {
        await api.post(url);
      }
      const listRes = await api.get<AttendanceRegistration[]>(
        `/events/${event.id}/attendance`,
      );
      setRegistrations(listRes.data);
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Erro ao atualizar a presença."));
    } finally {
      setWorkingId(null);
    }
  };

  const presentCount = registrations.filter((r) => r.attended).length;

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[560px] max-h-[90vh] flex flex-col p-0 overflow-hidden gap-0">
        <DialogHeader className="px-6 pt-6 pb-4 pr-12 min-w-0 shrink-0 border-b border-slate-100">
          <DialogTitle className="truncate text-lg font-semibold text-slate-800">
            Presença do Evento
          </DialogTitle>
          <DialogDescription className="break-words text-sm text-slate-500">
            {event?.name}
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
          {error && (
            <div className="flex items-center gap-2 p-2.5 text-xs text-red-600 bg-red-50 rounded-lg border border-red-200">
              <AlertCircle className="h-4 w-4 shrink-0" />
              <span className="flex-1 break-words">{error}</span>
            </div>
          )}

          {isLoading ? (
            <div className="flex items-center justify-center p-6 text-slate-400 gap-2">
              <Loader2 className="h-4 w-4 animate-spin text-[#109bc6]" />
              <span className="text-xs">Carregando...</span>
            </div>
          ) : (
            <>
              <div className="space-y-2">
                <Label className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
                  Código de presença
                </Label>
                <div className="rounded-xl border bg-slate-50/50 p-4 text-center">
                  <p className="text-3xl font-bold tracking-[0.4em] text-slate-800">
                    {code ?? "------"}
                  </p>
                  <p className="mt-2 text-xs text-slate-500">
                    Informe este código aos alunos presentes durante o evento.
                  </p>
                </div>
              </div>

              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
                    Inscritos
                  </Label>
                  <span className="text-xs text-slate-500">
                    {presentCount} de {registrations.length} presentes
                  </span>
                </div>

                {registrations.length === 0 ? (
                  <p className="rounded-xl border border-dashed p-4 text-center text-sm text-slate-500">
                    Nenhum aluno inscrito neste evento.
                  </p>
                ) : (
                  <ul className="divide-y rounded-xl border">
                    {registrations.map((registration) => (
                      <li
                        key={registration.studentId}
                        className="flex items-center justify-between gap-3 p-3">
                        <div className="min-w-0 text-sm text-slate-700">
                          <p className="truncate font-medium">
                            {registration.studentName}
                          </p>
                          <p className="truncate text-xs text-slate-500">
                            {registration.courseName}
                          </p>
                          {registration.attended && registration.attendedAt && (
                            <p className="flex items-center gap-1 text-xs text-emerald-700">
                              <CheckCircle2 className="h-3 w-3" />
                              Presente em{" "}
                              {formatDateForShort(registration.attendedAt)}
                              {registration.method
                                ? ` (${methodLabel[registration.method]})`
                                : ""}
                            </p>
                          )}
                        </div>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          className="shrink-0 cursor-pointer"
                          disabled={workingId === registration.studentId}
                          onClick={() => handleToggle(registration)}>
                          {workingId === registration.studentId && (
                            <Loader2 className="h-4 w-4 animate-spin mr-1" />
                          )}
                          {registration.attended
                            ? "Desmarcar"
                            : "Marcar presença"}
                        </Button>
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              <div className="space-y-2 border-t border-slate-100 pt-4">
                <div className="flex items-center justify-between">
                  <Label className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
                    Feedbacks do Evento
                  </Label>
                  <span className="text-xs text-slate-500">
                    {feedbacks.length} avaliação(ões)
                  </span>
                </div>

                {feedbacks.length === 0 ? (
                  <p className="rounded-xl border border-dashed p-4 text-center text-sm text-slate-500">
                    Nenhum feedback enviado para este evento ainda.
                  </p>
                ) : (
                  <ul className="divide-y rounded-xl border">
                    {feedbacks.map((fb) => (
                      <li key={fb.id} className="space-y-1.5 p-3">
                        <div className="flex items-center justify-between gap-2">
                          <div className="min-w-0">
                            <p className="truncate text-sm font-medium text-slate-800">
                              {fb.studentName}
                            </p>
                            <p className="truncate text-xs text-slate-500">
                              {fb.courseName} • {formatDateForShort(fb.createdAt)}
                            </p>
                          </div>
                          <Badge variant="secondary" className="shrink-0">
                            {fb.rating} / 10
                          </Badge>
                        </div>
                        {fb.feedbackText && (
                          <p className="text-xs text-slate-700 break-words bg-slate-50 rounded-lg p-2">
                            {fb.feedbackText}
                          </p>
                        )}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
};
