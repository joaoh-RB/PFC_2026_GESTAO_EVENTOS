import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { Loader2, AlertCircle } from 'lucide-react';
import { CreateEventForm } from '../components/CreateEventForm'; // Importa o componente que criamos
import { type CreateEventFormData } from '../schemas/eventSchema';
import { useAuth } from '../hooks/useAuth';

interface OptionItem {
  id: string;
  name: string;
}

export const Events: React.FC = () => {
  const navigate = useNavigate();

  // Estados de dados da página
  const [institutions, setInstitutions] = useState<OptionItem[]>([]);
  const [courses, setCourses] = useState<OptionItem[]>([]);
  const [eventTypes, setEventTypes] = useState<OptionItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);

  // Estados de submissão
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const { user } = useAuth();
  const userInstitutionId = user?.institutionId || null; 

  // 1. Busca os dados ao carregar a página
  useEffect(() => {
    let isMounted = true;

    const loadData = async () => {
      try {
        const [instRes, courseRes, eventTypesRes] = await Promise.all([
          api.get<OptionItem[]>('/institutions'),
          api.get<OptionItem[]>('/courses'),
          api.get<OptionItem[]>('/events/types'),
        ]);

        if (isMounted) {
          setInstitutions(instRes.data);
          setCourses(courseRes.data);
          setEventTypes(eventTypesRes.data);
          setIsLoading(false);
        }
      } catch {
        if (isMounted) {
          setPageError('Não foi possível carregar as informações do sistema.');
          setIsLoading(false);
        }
      }
    };

    loadData();

    return () => { isMounted = false; };
  }, []);

  const handleCreateEvent = async (data: CreateEventFormData) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      await api.post('/events', data);
      setIsSuccess(true);
      
    } catch (err: any) {
      setSubmitError(err.response?.data?.message || 'Erro ao criar o evento.');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <div className="flex flex-col items-center gap-2 text-indigo-600">
          <Loader2 className="h-8 w-8 animate-spin" />
          <span className="text-sm font-medium">Carregando formulário...</span>
        </div>
      </div>
    );
  }

  if (pageError) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-4">
        <div className="flex max-w-md flex-col items-center gap-4 rounded-xl bg-white p-8 text-center shadow-lg">
          <AlertCircle className="h-12 w-12 text-red-500" />
          <p className="text-slate-800 font-medium">{pageError}</p>
          <button 
            onClick={() => window.location.reload()} 
            className="text-sm text-indigo-600 hover:underline"
          >
            Tentar novamente
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50 py-10 px-4 sm:px-6 lg:px-8">
      <CreateEventForm 
        institutions={institutions}
        courses={courses}
        eventTypes={eventTypes}
        userInstitutionId={userInstitutionId}
        onSubmit={handleCreateEvent}
        isSubmitting={isSubmitting}
        isSuccess={isSuccess}
        apiError={submitError}
      />
    </div>
  );
};