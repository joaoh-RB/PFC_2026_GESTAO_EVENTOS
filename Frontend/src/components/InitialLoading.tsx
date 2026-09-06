import { Loader2 } from "lucide-react";

export const InitialLoading: React.FC = () => {
  return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <div className="flex flex-col items-center gap-2 text-indigo-600">
          <Loader2 className="h-8 w-8 animate-spin" />
          <span className="text-sm font-medium">Carregando dados...</span>
        </div>
      </div>
  );
};