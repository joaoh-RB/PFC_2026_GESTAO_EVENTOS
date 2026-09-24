import { useState } from "react";
import { CalendarPlus, Check, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import api from "@/services/api";

export function AddToCalendarButton({ eventId }: { eventId: string }) {
  const [loading, setLoading] = useState(false);
  const [added, setAdded] = useState(false);

  const handleAddToCalendar = async () => {
    setLoading(true);
    try {
      await api.post(`/events/${eventId}/add-to-calendar`);
      setAdded(true);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Button
      variant="outline"
      size="sm"
      disabled={loading || added}
      onClick={handleAddToCalendar}
      className="gap-2">
      {loading ? (
        <Loader2 className="size-4 animate-spin" />
      ) : added ? (
        <Check className="size-4 text-emerald-600" />
      ) : (
        <CalendarPlus className="size-4 text-[#109bc6]" />
      )}
      {added ? "Adicionado à Agenda" : "Adicionar à Agenda"}
    </Button>
  );
}
