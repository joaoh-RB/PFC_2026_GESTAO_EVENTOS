import React from 'react';
import { useAuth } from '../hooks/useAuth';

export const Dashboard: React.FC = () => {
    const { user } = useAuth();


    return (
        <div className="min-h-screen bg-slate-50">

            <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
                <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
                    <div className="flex items-center justify-between">
                        <div>
                            <h2 className="text-lg font-semibold text-slate-800">Bem-vindo(a), {user?.name}!</h2>
                            <p className="mt-1 text-sm text-slate-500">Você está conectado com o perfil: {user?.role}:</p>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    );
};