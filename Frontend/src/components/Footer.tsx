import React from "react";
import { Link } from "react-router-dom";

export const Footer: React.FC = () => {
  return (
    <footer className="border-t border-slate-100 bg-transparent py-4">
      <div className="mx-auto max-w-7xl px-4 text-center">
        <p className="text-xs text-slate-500">
          SYMPLOSIO 2026. Todos os direitos reservados.
          {" "}
          <Link
            to="/termos"
            target="_blank"
            rel="noopener noreferrer"
            className="ml-2 font-semibold text-indigo-800 hover:underline"
          >
            Política de Privacidade e Termos de Uso
          </Link>
        </p>
      </div>
    </footer>
  );
};
