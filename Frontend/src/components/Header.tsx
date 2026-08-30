import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import type { UserRole } from '../types/auth';
import {
    Settings,
    Home,
    GraduationCap,
    LogOut,
    Menu,
    X,
    User,
} from 'lucide-react';

interface NavItem {
    label: string;
    path: string;
    icon: React.ComponentType<{ className?: string }>;
    roles?: UserRole[];
}

const navItems: NavItem[] = [
    {
        label: 'Dashboard',
        path: '/dashboard',
        icon: Home,
    },
    {
        label: 'Segurança & 2FA',
        path: '/settings',
        icon: Settings,
    },
];

export const Header: React.FC = () => {
    const { user, logout } = useAuth();
    const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

    // Filtra as rotas permitidas para o papel do usuário atual
    const visibleNavItems = navItems.filter((item) => {
        if (!item.roles) return true;
        return user?.role && item.roles.includes(user.role);
    });

    const getRoleBadge = () => {
        switch (user?.role) {
            case 'Administrador':
                return (
                    <span className="inline-flex items-center gap-1 rounded-full bg-red-100 px-2.5 py-0.5 text-xs font-semibold text-red-800">
                        Admin
                    </span>
                );
            case 'Professor':
                return (
                    <span className="inline-flex items-center gap-1 rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-semibold text-blue-800">
                        Professor
                    </span>
                );
            default:
                return (
                    <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-800">
                        Aluno
                    </span>
                );
        }
    };

    return (
        <header className="sticky top-0 z-50 border-b border-slate-200 bg-white shadow-xs">
            <div className="mx-auto flex max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
                {/* Logo / Título */}
                <div className="flex items-center gap-8 py-4">
                    <div className="flex items-center gap-2">
                        <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-indigo-600 text-white shadow-sm">
                            <GraduationCap className="h-5 w-5" />
                        </div>
                        <span className="text-lg font-bold tracking-tight text-slate-900">
                            Gestão de Eventos
                        </span>
                    </div>

                    {/* Links Desktop */}
                    <nav className="hidden md:flex items-center gap-1">
                        {visibleNavItems.map((item) => {
                            const Icon = item.icon;
                            return (
                                <NavLink
                                    key={item.path}
                                    to={item.path}
                                    className={({ isActive }) =>
                                        `inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${isActive
                                            ? 'bg-indigo-50 text-indigo-700'
                                            : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                                        }`
                                    }
                                >
                                    <Icon className="h-4 w-4" />
                                    {item.label}
                                </NavLink>
                            );
                        })}
                    </nav>
                </div>

                {/* Informações do Usuário & Logout Desktop */}
                <div className="hidden md:flex items-center gap-4">
                    <div className="flex items-center gap-3 border-r border-slate-200 pr-4">
                        <div className="flex h-9 w-9 items-center justify-center rounded-full bg-slate-100 text-slate-600">
                            <User className="h-5 w-5" />
                        </div>
                        <div className="text-right">
                            <div className="flex items-center justify-end gap-2">
                                <p className="text-sm font-semibold text-slate-900">{user?.name}</p>
                                {getRoleBadge()}
                            </div>
                            <p className="text-xs text-slate-500">{user?.email}</p>
                        </div>
                    </div>

                    <button
                        onClick={logout}
                        className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3.5 py-2 text-sm font-medium text-slate-700 shadow-xs hover:bg-red-50 hover:text-red-700 hover:border-red-200 transition-colors"
                        title="Sair da conta"
                    >
                        <LogOut className="h-4 w-4" />
                        Sair
                    </button>
                </div>

                {/* Botão Mobile Hamburguer */}
                <div className="flex md:hidden">
                    <button
                        onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
                        className="rounded-lg p-2 text-slate-600 hover:bg-slate-100"
                    >
                        {isMobileMenuOpen ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
                    </button>
                </div>
            </div>

            {/* Menu Mobile */}
            {isMobileMenuOpen && (
                <div className="border-t border-slate-200 bg-white px-4 py-4 md:hidden">
                    <div className="mb-4 flex items-center gap-3 border-b border-slate-100 pb-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-slate-600">
                            <User className="h-6 w-6" />
                        </div>
                        <div>
                            <p className="text-sm font-semibold text-slate-900">{user?.name}</p>
                            <div className="mt-0.5 flex items-center gap-2">
                                {getRoleBadge()}
                                <span className="text-xs text-slate-500">{user?.email}</span>
                            </div>
                        </div>
                    </div>

                    <nav className="flex flex-col gap-1">
                        {visibleNavItems.map((item) => {
                            const Icon = item.icon;
                            return (
                                <NavLink
                                    key={item.path}
                                    to={item.path}
                                    onClick={() => setIsMobileMenuOpen(false)}
                                    className={({ isActive }) =>
                                        `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${isActive
                                            ? 'bg-indigo-50 text-indigo-700'
                                            : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                                        }`
                                    }
                                >
                                    <Icon className="h-5 w-5" />
                                    {item.label}
                                </NavLink>
                            );
                        })}

                        <button
                            onClick={logout}
                            className="mt-2 flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-red-600 hover:bg-red-50 transition-colors"
                        >
                            <LogOut className="h-5 w-5" />
                            Sair da Conta
                        </button>
                    </nav>
                </div>
            )}
        </header>
    );
};