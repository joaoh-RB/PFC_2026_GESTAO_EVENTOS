import React, { useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { api } from '../services/api';
import { useAuth } from '../hooks/useAuth';
import { ShieldCheck, ShieldAlert, KeyRound, Copy, Check, Loader2, AlertCircle } from 'lucide-react';

interface Setup2FAResponse {
    secretKey: string;
    qrCodeUrl: string;
}

export const UserSettings: React.FC = () => {
    const { user } = useAuth();

    const [is2FAEnabled, setIs2FAEnabled] = useState(user?.twoFactorEnabled ?? false);
    const [setupData, setSetupData] = useState<Setup2FAResponse | null>(null);
    const [code, setCode] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [isActivating, setIsActivating] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [copied, setCopied] = useState(false);

    // Etapa 1: Inicia o setup gerando a chave secreta e URI do QR Code
    const handleStartSetup = async () => {
        setError(null);
        setIsLoading(true);
        try {
            const response = await api.post<Setup2FAResponse>(`/auth/2fa/setup`);
            setSetupData(response.data);
        } catch (err: any) {
            setError(err.response?.data?.message || 'Erro ao gerar configuração de 2FA.');
        } finally {
            setIsLoading(false);
        }
    };

    // Etapa 2: Valida o código gerado no app do usuário e ativa o 2FA
    const handleEnable2FA = async (e: React.SubmitEvent) => {
        e.preventDefault();
        setError(null);
        setIsActivating(true);

        try {
            await api.post(`/auth/2fa/enable/`, { code });
            setIs2FAEnabled(true);
            setSetupData(null);
            setCode('');
        } catch (err: any) {
            setError(err.response?.data?.message || 'Código inválido. Tente novamente.');
        } finally {
            setIsActivating(false);
        }
    };

    const handleCopySecret = () => {
        if (!setupData?.secretKey) return;
        navigator.clipboard.writeText(setupData.secretKey);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    return (
        <div className="mx-auto max-w-4xl p-6">
            <div className="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
                <div className="flex items-start justify-between border-b border-slate-100 pb-6">
                    <div>
                        <h2 className="text-xl font-bold text-slate-900">Segurança da Conta</h2>
                        <p className="mt-1 text-sm text-slate-500">
                            Gerencie a autenticação em duas etapas (2FA) para proteger seus acessos.
                        </p>
                    </div>
                    {is2FAEnabled ? (
                        <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-3 py-1.5 text-xs font-semibold text-emerald-700">
                            <ShieldCheck className="h-4 w-4 text-emerald-600" />
                            2FA Ativo
                        </span>
                    ) : (
                        <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-50 px-3 py-1.5 text-xs font-semibold text-amber-700">
                            <ShieldAlert className="h-4 w-4 text-amber-600" />
                            2FA Desativado
                        </span>
                    )}
                </div>

                {error && (
                    <div className="mt-6 flex items-center gap-2 rounded-lg bg-red-50 p-4 text-sm text-red-700">
                        <AlertCircle className="h-5 w-5 shrink-0" />
                        <span>{error}</span>
                    </div>
                )}

                {/* Estado: 2FA Já Ativado */}
                {is2FAEnabled && !setupData && (
                    <div className="mt-6 rounded-xl bg-slate-50 p-6 text-sm text-slate-600">
                        <p className="font-medium text-slate-800">
                            Sua conta está protegida com autenticação em duas etapas.
                        </p>
                        <p className="mt-1">
                            Sempre que fizer login, você precisará informar o código gerado pelo aplicativo Google Authenticator ou similar.
                        </p>
                    </div>
                )}

                {/* Estado: 2FA Inativo - Botão de Iniciar */}
                {!is2FAEnabled && !setupData && (
                    <div className="mt-6">
                        <p className="text-sm text-slate-600">
                            Adicione uma camada extra de segurança à sua conta exigindo um código TOTP temporário no momento do login.
                        </p>
                        <button
                            onClick={handleStartSetup}
                            disabled={isLoading}
                            className="mt-4 inline-flex items-center justify-center rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:bg-indigo-400"
                        >
                            {isLoading ? (
                                <>
                                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                    Gerando Chave...
                                </>
                            ) : (
                                'Configurar Autenticação de Dois Fatores'
                            )}
                        </button>
                    </div>
                )}

                {/* Estado: Assistente de Configuração do 2FA */}
                {setupData && (
                    <div className="mt-6 space-y-6">
                        <div className="rounded-xl bg-indigo-50/50 p-6">
                            <h3 className="text-sm font-semibold text-slate-900">Passo 1: Escanear o QR Code</h3>
                            <p className="mt-1 text-xs text-slate-600">
                                Abra o aplicativo autenticador no seu celular (Google Authenticator, Microsoft Authenticator, etc.) e escaneie o código abaixo:
                            </p>

                            <div className="mt-4 flex flex-col items-center justify-center gap-4 sm:flex-row sm:justify-start">
                                <div className="rounded-lg bg-white p-3 shadow-sm">
                                    <QRCodeSVG value={setupData.qrCodeUrl} size={150} level="M" />
                                </div>

                                <div className="space-y-2 text-center sm:text-left">
                                    <p className="text-xs font-medium text-slate-500">Ou digite o código manual no app:</p>
                                    <div className="flex items-center gap-2">
                                        <code className="rounded bg-slate-200 px-2 py-1 font-mono text-xs font-semibold text-slate-800">
                                            {setupData.secretKey}
                                        </code>
                                        <button
                                            type="button"
                                            onClick={handleCopySecret}
                                            className="rounded p-1.5 text-slate-500 hover:bg-slate-200 hover:text-slate-800"
                                            title="Copiar Chave"
                                        >
                                            {copied ? <Check className="h-4 w-4 text-emerald-600" /> : <Copy className="h-4 w-4" />}
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="rounded-xl bg-slate-50 p-6">
                            <h3 className="text-sm font-semibold text-slate-900">Passo 2: Confirmar ativação</h3>
                            <p className="mt-1 text-xs text-slate-600">
                                Insira o código de 6 dígitos exibido no aplicativo para confirmar a vinculação:
                            </p>

                            <form onSubmit={handleEnable2FA} className="mt-4 flex max-w-sm gap-3">
                                <div className="relative flex-1">
                                    <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                                        <KeyRound className="h-4 w-4" />
                                    </span>
                                    <input
                                        type="text"
                                        required
                                        maxLength={6}
                                        value={code}
                                        onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
                                        placeholder="000000"
                                        className="w-full rounded-lg border border-slate-300 py-2 pl-9 pr-3 text-sm tracking-widest font-mono focus:border-indigo-600 focus:outline-none focus:ring-1 focus:ring-indigo-600"
                                    />
                                </div>

                                <button
                                    type="submit"
                                    disabled={isActivating || code.length !== 6}
                                    className="inline-flex items-center justify-center rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 focus:outline-none disabled:bg-indigo-400"
                                >
                                    {isActivating ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Ativar'}
                                </button>
                            </form>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};