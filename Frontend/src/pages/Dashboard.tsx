import { Link } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import {
  ArrowRight,
  BookOpen,
  Building2,
  CalendarDays,
  GraduationCap,
  ShieldCheck,
  UsersRound,
} from "lucide-react";
import type { UserRole } from "../types/auth";

const quickLinks: {
  title: string;
  description: string;
  descriptionsByRole?: Partial<Record<UserRole, string>>;
  path: string;
  icon: typeof CalendarDays;
  color: string;
  roles?: UserRole[];
}[] = [
  {
    title: "Eventos",
    description: "Crie, acompanhe e organize a programação acadêmica.",
    descriptionsByRole: {
      Aluno: "Veja a programação e inscreva-se nos eventos disponíveis.",
    },
    path: "/events",
    icon: CalendarDays,
    color: "bg-cyan-50 text-cyan-600",
  },
  {
    title: "Alunos",
    description: "Aprove cadastros e gerencie o acesso dos estudantes.",
    descriptionsByRole: {
      Secretaria: "Aprove cadastros e gerencie os alunos da sua instituição.",
      Professor: "Acompanhe e aprove os cadastros dos seus alunos.",
    },
    path: "/users",
    icon: GraduationCap,
    color: "bg-violet-50 text-violet-600",
    roles: ["Administrador", "Professor", "Secretaria"],
  },
  {
    title: "Instituições",
    description: "Administre instituições parceiras e seus dados.",
    descriptionsByRole: {
      Secretaria: "Administre sua instituição e seus dados.",
    },
    path: "/institutions",
    icon: Building2,
    color: "bg-emerald-50 text-emerald-600",
    roles: ["Administrador", "Secretaria"],
  },
  {
    title: "Cursos",
    description: "Mantenha o catálogo acadêmico de todas as instituições.",
    descriptionsByRole: {
      Secretaria: "Mantenha o catálogo de cursos da sua instituição.",
    },
    path: "/courses",
    icon: BookOpen,
    color: "bg-amber-50 text-amber-600",
    roles: ["Administrador", "Secretaria"],
  },
];

export function Dashboard() {
  const { user } = useAuth();
  const firstName = user?.name?.split(" ")[0] || "usuário";

  const visibleQuickLinks = quickLinks.filter(
    (item) => !item.roles || (user?.role && item.roles.includes(user.role)),
  );

  const linksWithDescriptions = visibleQuickLinks.map((item) => ({
    ...item,
    description:
      (user?.role && item.descriptionsByRole?.[user.role]) || item.description,
  }));

  const accessCardCopy: Record<
    "Administrador" | "Secretaria" | "Professor",
    string
  > = {
    Administrador:
      "Gerencie alunos, membros, instituições e permissões em toda a plataforma.",
    Secretaria: "Gerencie alunos, professores e permissões da sua instituição.",
    Professor: "Acompanhe e aprove os cadastros de alunos da sua instituição.",
  };

  const showAccessCard = user?.role !== "Aluno";

  return (
    <div className="app-page">
      <div className="page-heading">
        <div>
          <h1 className="page-title">Bem-vindo ao SYMPLOSIO</h1>
          <p className="page-description">
            Olá, {firstName}. Acompanhe e gerencie sua operação acadêmica.
          </p>
        </div>
      </div>

      <div className={`grid gap-4 ${showAccessCard ? "lg:grid-cols-2" : ""}`}>
        <section className="surface-card p-5">
          <div className="flex items-center gap-2">
            <ShieldCheck className="size-4 text-[#12a7d4]" />
            <h2 className="text-sm font-semibold text-[#262242]">Seu acesso</h2>
          </div>
          <p className="mt-4 text-xs text-slate-400">Perfil atual</p>
          <p className="mt-1 text-sm font-semibold text-[#262242]">
            {user?.role}
          </p>
          <p className="mt-3 text-xs leading-5 text-slate-500">
            Os itens do menu e as ações disponíveis são exibidos conforme as
            permissões do seu perfil.
          </p>
        </section>

        {showAccessCard && (
          <section className="surface-card p-6">
            <div className="flex items-start gap-4">
              <div className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-violet-50 text-violet-600">
                <UsersRound className="size-5" />
              </div>
              <div className="flex-1">
                <h2 className="font-semibold text-[#211e42]">
                  Cadastros e acessos
                </h2>
                <p className="mt-1 text-sm leading-5 text-slate-500">
                  {
                    accessCardCopy[
                      user?.role as "Administrador" | "Secretaria" | "Professor"
                    ]
                  }
                </p>
                <Link
                  to="/users"
                  className="mt-4 inline-flex items-center gap-1.5 text-xs font-semibold text-[#109bc6] hover:gap-2">
                  Ir para cadastros <ArrowRight className="size-3.5" />
                </Link>
              </div>
            </div>
          </section>
        )}
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {linksWithDescriptions.map((item) => {
          const Icon = item.icon;
          return (
            <Link
              key={item.path}
              to={item.path}
              className="surface-card group p-5 transition hover:-translate-y-0.5 hover:border-slate-300 hover:shadow-md">
              <div
                className={`flex size-10 items-center justify-center rounded-xl ${item.color}`}>
                <Icon className="size-[18px]" />
              </div>
              <h3 className="mt-4 text-sm font-semibold text-[#262242]">
                {item.title}
              </h3>
              <p className="mt-1.5 min-h-10 text-xs leading-5 text-slate-500">
                {item.description}
              </p>
              <ArrowRight className="mt-4 size-4 text-slate-300 transition group-hover:translate-x-1 group-hover:text-[#12a7d4]" />
            </Link>
          );
        })}
      </div>
    </div>
  );
}
