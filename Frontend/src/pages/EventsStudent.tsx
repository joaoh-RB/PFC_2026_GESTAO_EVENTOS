import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { AlertCircle } from "lucide-react";
import { useEffect, useState } from "react";
import { PageHeader } from "@/components/PageHeader";
import { EventsList } from "@/components/events/EventList";
import api from "@/services/api";
import type { OptionItem } from "@/types/optionItem";
import { InitialLoading } from "@/components/InitialLoading";

export const EventsStudent: React.FC = () => {
  const [pageError, setPageError] = useState<string | null>(null);
  const { user } = useAuth();
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [institutions, setInstitutions] = useState<OptionItem[]>([]);

  const handleJoinEvent = async (eventId: string) => {
    setPageError(null);
    try {
      await api.post(`/events/${eventId}/join`);
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { message?: string } } };
      setPageError(
        errorObj.response?.data?.message || "Erro ao entrar no evento.",
      );
    }
  };

  const handleLeaveEvent = async (eventId: string) => {
    setPageError(null);
    try {
      await api.post(`/events/${eventId}/leave`);
    } catch (err: unknown) {
      const errorObj = err as { response?: { data?: { message?: string } } };
      setPageError(errorObj.response?.data?.message || "Erro ao sair do evento.");
    }
  };

  const userInstitutionId = user?.institutionId || null;
  useEffect(() => {
    const controller = new AbortController();

    const loadInitialData = async () => {
      try {
        const [instRes] = await Promise.all([
          api.get<OptionItem[]>("/institutions"),
        ]);
        setInstitutions(instRes.data);
      } catch {
        if (controller.signal.aborted) return;
        setPageError("Não foi possível carregar as informações do sistema.");
      } finally {
        setIsInitialLoading(false);
      }
    };

    loadInitialData();

    return () => {
      controller.abort();
    };
  }, []);
  if (isInitialLoading) {
    return <InitialLoading></InitialLoading>;
  }
  if (pageError) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-4">
        <div className="flex max-w-md flex-col items-center gap-4 rounded-xl bg-white p-8 text-center shadow-lg">
          <AlertCircle className="h-12 w-12 text-red-500" />
          <p className="text-slate-800 font-medium">{pageError}</p>
          <Button
            onClick={() => window.location.reload()}
            variant="outline"
            className="mt-2">
            Tentar novamente
          </Button>
        </div>
      </div>
    );
  }
  return (
    <div className={"app-page"}>
      <PageHeader
        title="Eventos"
        description={"Inscreva-se em eventos disponíveis"}></PageHeader>
      <EventsList
        userInstitutionId={userInstitutionId}
        studentMode={true}
        institutions={institutions}
        onJoinEvent={handleJoinEvent}
        onLeaveEvent={handleLeaveEvent}></EventsList>
    </div>
  );
};
