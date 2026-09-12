import { useState } from "react";
import type { ComponentType } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import type { UserRole } from "../types/auth";
import { Brand } from "./Brand";
import {
  BookOpen,
  Building2,
  CalendarDays,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  GraduationCap,
  LayoutDashboard,
  LogOut,
  Menu,
  PanelLeftClose,
  Settings,
  ShieldCheck,
  UsersRound,
  X,
} from "lucide-react";

interface NavItem {
  label: string;
  path: string;
  icon: ComponentType<{ className?: string }>;
  roles?: UserRole[];
}

const navItems: NavItem[] = [
  { label: "Painel", path: "/dashboard", icon: LayoutDashboard },
  {
    label: "Eventos",
    path: "/events",
    icon: CalendarDays,
    roles: ["Administrador", "Professor", "Secretaria"],
  },
  {
    label: "Alunos",
    path: "/users",
    icon: GraduationCap,
    roles: ["Administrador", "Professor", "Secretaria"],
  },
  {
    label: "Membros",
    path: "/institution-members",
    icon: UsersRound,
    roles: ["Administrador", "Secretaria"],
  },
  {
    label: "Instituições",
    path: "/institutions",
    icon: Building2,
    roles: ["Administrador", "Professor", "Secretaria"],
  },
  {
    label: "Cursos",
    path: "/courses",
    icon: BookOpen,
    roles: ["Administrador", "Professor", "Secretaria"],
  },
  { label: "Segurança", path: "/settings", icon: ShieldCheck },
];

const pageNames: Record<string, string> = {
  "/dashboard": "Painel",
  "/events": "Eventos",
  "/users": "Alunos",
  "/institution-members": "Membros",
  "/institutions": "Instituições",
  "/courses": "Cursos",
  "/settings": "Segurança",
};

interface HeaderProps {
  collapsed: boolean;
  onToggleCollapsed: () => void;
}

interface SidebarProps extends HeaderProps {
  items: NavItem[];
  mobile?: boolean;
  onCloseMobile: () => void;
}

function Sidebar({
  collapsed,
  items,
  mobile = false,
  onCloseMobile,
  onToggleCollapsed,
}: SidebarProps) {
  return (
    <aside
      className={`flex h-full flex-col bg-[linear-gradient(180deg,#1d145d_0%,#130b46_100%)] text-white ${
        collapsed && !mobile ? "w-[76px]" : "w-[240px]"
      } transition-[width] duration-200`}>
      <div
        className={`flex h-[74px] items-center border-b border-white/10 px-5 ${
          collapsed && !mobile ? "justify-center" : "justify-between"
        }`}>
        <Brand compact={collapsed && !mobile} inverse />
        {mobile && (
          <button
            className="rounded-lg p-1.5 text-white/70 hover:bg-white/10"
            onClick={onCloseMobile}
            aria-label="Fechar menu">
            <X className="size-5" />
          </button>
        )}
      </div>
      <nav className="flex-1 space-y-1.5 px-3 py-5">
        {(!collapsed || mobile) && (
          <p className="mb-3 px-3 text-[10px] font-semibold uppercase tracking-[0.16em] text-white/35">
            Menu principal
          </p>
        )}
        {items.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.path}
              to={item.path}
              title={collapsed && !mobile ? item.label : undefined}
              onClick={onCloseMobile}
              className={({ isActive }) =>
                `flex h-11 items-center rounded-lg text-sm font-medium transition ${
                  collapsed && !mobile ? "justify-center px-0" : "gap-3 px-3"
                } ${
                  isActive
                    ? "bg-[#148fc2] text-white shadow-[0_8px_20px_rgba(10,134,184,0.24)]"
                    : "text-white/70 hover:bg-white/[0.07] hover:text-white"
                }`
              }>
              <Icon className="size-[18px] shrink-0" />
              {(!collapsed || mobile) && <span>{item.label}</span>}
            </NavLink>
          );
        })}
      </nav>
      <div className="border-t border-white/10 p-3">
        <button
          type="button"
          onClick={onToggleCollapsed}
          className={`hidden h-10 w-full items-center rounded-lg text-sm text-white/55 transition hover:bg-white/[0.07] hover:text-white lg:flex ${
            collapsed ? "justify-center" : "gap-3 px-3"
          }`}>
          <PanelLeftClose
            className={`size-[18px] transition ${collapsed ? "rotate-180" : ""}`}
          />
          {!collapsed && "Recolher menu"}
        </button>
      </div>
    </aside>
  );
}

export function Header({ collapsed, onToggleCollapsed }: HeaderProps) {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);

  const visibleNavItems = navItems.filter(
    (item) => !item.roles || (user?.role && item.roles.includes(user.role)),
  );
  const currentPage = pageNames[location.pathname] ?? "Painel";
  const initials =
    user?.name
      ?.split(" ")
      .slice(0, 2)
      .map((part) => part[0])
      .join("")
      .toUpperCase() || "US";

  return (
    <>
      <div className="fixed inset-y-0 left-0 z-40 hidden lg:block">
        <Sidebar
          collapsed={collapsed}
          items={visibleNavItems}
          onCloseMobile={() => setMobileOpen(false)}
          onToggleCollapsed={onToggleCollapsed}
        />
      </div>

      {mobileOpen && (
        <div className="fixed inset-0 z-50 flex lg:hidden">
          <button
            className="absolute inset-0 bg-[#0f0b35]/50 backdrop-blur-sm"
            onClick={() => setMobileOpen(false)}
            aria-label="Fechar menu"
          />
          <div className="relative">
            <Sidebar
              collapsed={collapsed}
              items={visibleNavItems}
              mobile
              onCloseMobile={() => setMobileOpen(false)}
              onToggleCollapsed={onToggleCollapsed}
            />
          </div>
        </div>
      )}

      <header
        className={`fixed right-0 top-0 z-30 h-[74px] border-b border-[#e5e7ec] bg-white/95 backdrop-blur transition-[left] ${
          collapsed ? "left-0 lg:left-[76px]" : "left-0 lg:left-[240px]"
        }`}>
        <div className="flex h-full items-center justify-between px-4 sm:px-6">
          <div className="flex items-center gap-3">
            <button
              className="icon-action lg:hidden"
              onClick={() => setMobileOpen(true)}
              aria-label="Abrir menu">
              <Menu className="size-5" />
            </button>
            <button
              className="hidden size-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-50 lg:flex"
              onClick={onToggleCollapsed}
              aria-label="Alternar menu">
              {collapsed ? (
                <ChevronRight className="size-4" />
              ) : (
                <ChevronLeft className="size-4" />
              )}
            </button>
            <div className="hidden items-center gap-2 text-xs sm:flex">
              <span className="text-slate-400">Início</span>
              <span className="text-slate-300">›</span>
              <span className="font-semibold text-[#24203f]">{currentPage}</span>
            </div>
          </div>

          <div className="flex items-center gap-2 sm:gap-3">
            <div className="hidden items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs font-medium text-slate-600 md:flex">
              <Building2 className="size-3.5 text-[#14a5d1]" />
              {user?.institutionId ? "Instituição vinculada" : "Gestão de Eventos"}
            </div>

            <div className="relative">
              <button
                type="button"
                onClick={() => setProfileOpen((open) => !open)}
                className="flex h-11 items-center gap-3 rounded-xl border border-slate-200 bg-white px-2 pr-3 text-left shadow-sm transition hover:border-slate-300">
                <span className="flex size-8 items-center justify-center rounded-lg bg-[#17104f] text-[11px] font-bold text-white">
                  {initials}
                </span>
                <span className="hidden min-w-0 sm:block">
                  <span className="block max-w-32 truncate text-xs font-semibold text-[#211d42]">
                    {user?.name}
                  </span>
                  <span className="block text-[10px] text-slate-400">{user?.role}</span>
                </span>
                <ChevronDown
                  className={`size-3.5 text-slate-400 transition ${profileOpen ? "rotate-180" : ""}`}
                />
              </button>

              {profileOpen && (
                <div className="absolute right-0 mt-2 w-56 overflow-hidden rounded-xl border border-slate-200 bg-white p-2 shadow-xl">
                  <div className="border-b border-slate-100 px-3 py-2.5">
                    <p className="truncate text-sm font-semibold text-[#221e42]">
                      {user?.name}
                    </p>
                    <p className="mt-0.5 truncate text-xs text-slate-400">
                      {user?.email}
                    </p>
                  </div>
                  <NavLink
                    to="/settings"
                    onClick={() => setProfileOpen(false)}
                    className="mt-1 flex items-center gap-2.5 rounded-lg px-3 py-2.5 text-sm text-slate-600 hover:bg-slate-50">
                    <Settings className="size-4" />
                    Meu perfil
                  </NavLink>
                  <button
                    onClick={logout}
                    className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2.5 text-sm text-red-500 hover:bg-red-50">
                    <LogOut className="size-4" />
                    Sair
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </header>
    </>
  );
}
