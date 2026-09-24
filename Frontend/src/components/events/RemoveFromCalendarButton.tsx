import { useState } from "react";
import { CalendarMinus, Loader2, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import api from "@/services/api";

interface RemoveFromCalendarButtonProps {
  eventId: string;
  onRemoved?: () => void;
}

export function RemoveFromCalendarButton({
  eventId,
  onRemoved,
}: RemoveFromCalendarButtonProps) {
  const [loading, setLoading] = useState(false);
  const [removed, setRemoved] = useState(false);

  const handleRemoveFromCalendar = async () => {
    setLoading(true);
    try {
      await api.post(`/events/${eventId}/remove-from-calendar`);
      setRemoved(true);
      onRemoved?.();
    } finally {
      setLoading(false);
    }
  };

  return (
    <Button
      variant="outline"
      size="sm"
      disabled={loading || removed}
      onClick={handleRemoveFromCalendar}
      className="gap-2">
      {loading ? (
        <Loader2 className="size-4 animate-spin" />
      ) : removed ? (
        <X className="size-4 text-slate-500" />
      ) : (
        <CalendarMinus className="size-4 text-[#109bc6]" />
      )}
      {removed ? "Removido da Agenda" : "Remover da Agenda"}
    </Button>
  );
}