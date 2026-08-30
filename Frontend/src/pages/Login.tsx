import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { loginSchema, type LoginFormData } from '../schemas/authSchema';
import { Lock, Mail, KeyRound, AlertCircle, Loader2, ArrowLeft } from 'lucide-react';

export const Login: React.FC = () => {
    const [apiError, setApiError] = useState<string | null>(null);
    const [requires2FA, setRequires2FA] = useState(false);
    const { login } = useAuth();
    const navigate = useNavigate();

    const {
        register,
        handleSubmit,
        setValue,
        formState: { errors, isSubmitting },
    } = useForm<LoginFormData>({
        resolver: zodResolver(loginSchema),
        mode: 'onBlur',
    });

    const onSubmit = async (data: LoginFormData) => {
        setApiError(null);
        try {
            const response = await login(data);

            if (response.requiresTwoFactor) {
                setRequires2FA(true);
                return;
            }

            navigate('/dashboard');
        } catch (err: any) {
            setApiError(err.response?.data?.message || 'E-mail, senha ou código inválidos.');
        }
    };

    const handleBackToCredentials = () => {
        setRequires2FA(false);
        setValue('twoFactorCode', '');
        setApiError(null);
    };

    return (
        <div className="flex min-h-screen items-center justify-center bg-slate-100 p-4">
            <div className="w-full max-w-md rounded-2xl bg-white p-8 shadow-xl">
                <div className="mb-8 text-center">
                    <h1 className="text-2xl font-bold tracking-tight text-slate-900">
                        {requires2FA ? 'Verificação em Duas Etapas' : 'Gestão de Eventos'}
                    </h1>
                    <p className="mt-2 text-sm text-slate-600">
                        {requires2FA
                            ? 'Insira o código de 6 dígitos gerado no seu aplicativo autenticador (Google Authenticator)'
                            : 'Acesse sua conta'}
                    </p>
                </div>

                {apiError && (
                    <div className="mb-6 flex items-center gap-2 rounded-lg bg-red-50 p-4 text-sm text-red-700">
                        <AlertCircle className="h-5 w-5 shrink-0" />
                        <span>{apiError}</span>
                    </div>
                )}

                <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                    {!requires2FA ? (
                        /* ETAPA 1: E-mail e Senha */
                        <>
                            <div>
                                <label className="block text-sm font-medium text-slate-700">E-mail</label>
                                <div className="relative mt-1">
                                    <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                                        <Mail className="h-5 w-5" />
                                    </span>
                                    <input
                                        {...register('email')}
                                        type="email"
                                        placeholder="exemplo@email.com"
                                        className="w-full rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 text-sm focus:border-indigo-600 focus:outline-none focus:ring-1 focus:ring-indigo-600"
                                    />
                                </div>
                                {errors.email && (
                                    <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>
                                )}
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-slate-700">Senha</label>
                                <div className="relative mt-1">
                                    <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                                        <Lock className="h-5 w-5" />
                                    </span>
                                    <input
                                        {...register('password')}
                                        type="password"
                                        placeholder="••••••••"
                                        className="w-full rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 text-sm focus:border-indigo-600 focus:outline-none focus:ring-1 focus:ring-indigo-600"
                                    />
                                </div>
                                {errors.password && (
                                    <p className="mt-1 text-xs text-red-600">{errors.password.message}</p>
                                )}
                            </div>
                        </>
                    ) : (
                        <div>
                            <label className="block text-sm font-medium text-slate-700">Código de Autenticação</label>
                            <div className="relative mt-1">
                                <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                                    <KeyRound className="h-5 w-5" />
                                </span>
                                <input
                                    {...register('twoFactorCode')}
                                    type="text"
                                    maxLength={6}
                                    autoFocus
                                    placeholder="000000"
                                    className="w-full tracking-widest text-center text-lg font-mono rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 focus:border-indigo-600 focus:outline-none focus:ring-1 focus:ring-indigo-600"
                                />
                            </div>
                        </div>
                    )}

                    <button
                        type="submit"
                        disabled={isSubmitting}
                        className="flex w-full items-center justify-center rounded-lg bg-indigo-600 py-2.5 text-sm font-semibold text-white shadow-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:bg-indigo-400"
                    >
                        {isSubmitting ? (
                            <>
                                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                {requires2FA ? 'Validando código...' : 'Entrando...'}
                            </>
                        ) : requires2FA ? (
                            'Confirmar Código'
                        ) : (
                            'Entrar'
                        )}
                    </button>

                    {requires2FA && (
                        <button
                            type="button"
                            onClick={handleBackToCredentials}
                            className="flex w-full items-center justify-center gap-1.5 text-xs font-semibold text-slate-600 hover:text-slate-900"
                        >
                            <ArrowLeft className="h-3.5 w-3.5" />
                            Voltar e alterar credenciais
                        </button>
                    )}
                </form>

                {!requires2FA && (
                    <div className="mt-6 text-center text-sm text-slate-600">
                        Ainda não tem uma conta?{' '}
                        <Link to="/register" className="font-semibold text-indigo-600 hover:underline">
                            Cadastre-se
                        </Link>
                    </div>
                )}
            </div>
        </div>
    );
};