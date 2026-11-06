import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { CheckCircle2, Loader2 } from "lucide-react";
import api from "@/services/api";

interface AttendanceConfirmProps {
  eventId: string;
  attended: boolean;
}

const getErrorMessage = (err: unknown, fallback: string): string => {
  const errorObj = err as {
    response?: { data?: { message?: string } };
    message?: string;
  };
  return errorObj?.response?.data?.message || errorObj?.message || fallback;
};

export const AttendanceConfirm: React.FC<AttendanceConfirmProps> = ({
  eventId,
  attended,
}) => {
  const [code, setCode] = useState("");
  const [confirmed, setConfirmed] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleConfirm = async () => {
    setIsSending(true);
    setError(null);
    try {
      await api.post(`/events/${eventId}/attendance/confirm`, { code });
      setConfirmed(true);
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Erro ao confirmar a presença."));
    } finally {
      setIsSending(false);
    }
  };

  if (attended || confirmed) {
    return (
      <div className="flex items-center justify-end gap-2 text-sm font-medium text-emerald-700">
        <CheckCircle2 className="h-4 w-4" />
        Presença confirmada
      </div>
    );
  }

  return (
    <div className="space-y-2">
      <p className="text-xs text-slate-500">
        O evento começou. Digite o código informado pelo professor para
        confirmar sua presença.
      </p>
      <div className="flex items-center gap-2">
        <Input
          value={code}
          maxLength={6}
          placeholder="Código"
          className="h-9 w-32 uppercase tracking-widest"
          onChange={(e) => setCode(e.target.value.toUpperCase())}
        />
        <Button 
          size="sm"
          className="primary-action cursor-pointer"
          disabled={code.length !== 6 || isSending}
          onClick={handleConfirm}>
          {isSending && <Loader2 className="h-4 w-4 animate-spin mr-1" />}
          Confirmar presença
        </Button>
      </div>
      {error && <p className="text-xs text-red-600">{error}</p>}
    </div>
  );
};