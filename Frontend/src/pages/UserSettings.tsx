import React, { useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { api } from '../services/api';
import { useAuth } from '../hooks/useAuth';
import { ShieldCheck, ShieldAlert, KeyRound, Copy, Check, Loader2, AlertCircle, Trash2 } from 'lucide-react';
import { PageHeader } from '../components/PageHeader';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '../components/ui/dialog';

interface Setup2FAResponse {
    secretKey: string;
    qrCodeUrl: string;
}

export const UserSettings: React.FC = () => {
    const { user, logout } = useAuth();
    const canAnonymize = user?.role !== 'Administrador';

    const [is2FAEnabled, setIs2FAEnabled] = useState(user?.twoFactorEnabled ?? false);
    const [setupData, setSetupData] = useState<Setup2FAResponse | null>(null);
    const [code, setCode] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [isActivating, setIsActivating] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [copied, setCopied] = useState(false);
    const [anonymizeOpen, setAnonymizeOpen] = useState(false);
    const [confirmedPermanent, setConfirmedPermanent] = useState(false);
    const [isAnonymizing, setIsAnonymizing] = useState(false);
    const [anonymizeError, setAnonymizeError] = useState<string | null>(null);

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

    const handleOpenAnonymize = () => {
        setAnonymizeError(null);
        setConfirmedPermanent(false);
        setAnonymizeOpen(true);
    };

    const handleAnonymize = async () => {
        if (!confirmedPermanent) return;
        setAnonymizeError(null);
        setIsAnonymizing(true);
        try {
            await api.post('/auth/anonymize');
            setAnonymizeOpen(false);
            await logout();
        } catch (err: any) {
            setAnonymizeError(err.response?.data?.message || 'Não foi possível anonimizar os dados.');
            setIsAnonymizing(false);
        }
    };

    const handleCopySecret = () => {
        if (!setupData?.secretKey) return;
        navigator.clipboard.writeText(setupData.secretKey);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    return (
        <div className="app-page">
            <PageHeader title="Segurança" description="Gerencie a proteção e as preferências de acesso da sua conta." />
            <div className="surface-card p-6 sm:p-8">
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

                {!is2FAEnabled && !setupData && (
                    <div className="mt-6">
                        <p className="text-sm text-slate-600">
                            Adicione uma camada extra de segurança à sua conta exigindo um código TOTP temporário no momento do login.
                        </p>
                        <button
                            onClick={handleStartSetup}
                            disabled={isLoading}
                            className="primary-action mt-4"
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

                {setupData && (
                    <div className="mt-6 space-y-6">
                        <div className="rounded-xl bg-cyan-50/60 p-6">
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
                                        className="form-control pl-9 font-mono tracking-widest"
                                    />
                                </div>

                                <button
                                    type="submit"
                                    disabled={isActivating || code.length !== 6}
                                    className="primary-action"
                                >
                                    {isActivating ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Ativar'}
                                </button>
                            </form>
                        </div>
                    </div>
                )}

                {canAnonymize && (
                    <div className="mt-8 border-t border-slate-100 pt-8">
                        <h3 className="text-base font-semibold text-slate-900">Excluir e anonimizar dados</h3>
                        <p className="mt-1 max-w-2xl text-sm text-slate-600">
                            Remove sua identificação das tabelas do sistema e anonimiza o histórico de e-mail. A conta deixa de poder ser acessada.
                        </p>
                        <button
                            type="button"
                            onClick={handleOpenAnonymize}
                            className="mt-4 inline-flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-2.5 text-sm font-semibold text-red-700 hover:bg-red-100"
                        >
                            <Trash2 className="h-4 w-4" />
                            Anonimizar Meus Dados
                        </button>
                    </div>
                )}
            </div>

            {canAnonymize && (
                <Dialog
                    open={anonymizeOpen}
                    onOpenChange={(open) => {
                        if (!open && !isAnonymizing) setAnonymizeOpen(false);
                    }}
                >
                    <DialogContent
                        showCloseButton={!isAnonymizing}
                        className="w-[calc(100vw-1.5rem)] sm:max-w-4xl max-h-[92vh] overflow-y-auto gap-0 p-0"
                    >
                        <div className="border-b border-red-100 bg-red-50 px-8 py-8 sm:px-10">
                            <DialogHeader className="gap-3">
                                <DialogTitle className="text-2xl font-bold text-slate-900 sm:text-3xl">
                                    Esta exclusão é permanente
                                </DialogTitle>
                                <DialogDescription className="text-base leading-relaxed text-slate-700">
                                    Se você confirmar, seus dados pessoais serão anonimizados agora e não poderão ser recuperados. O administrador não executa esta ação: ela vale apenas para a sua própria conta.
                                </DialogDescription>
                            </DialogHeader>
                        </div>

                        <div className="space-y-5 px-8 py-8 text-base leading-relaxed text-slate-700 sm:px-10">
                            <p className="font-medium text-slate-900">
                                Ao confirmar, o sistema faz o seguinte de forma definitiva:
                            </p>
                            <ul className="list-disc space-y-3 pl-5">
                                <li>O seu nome deixa de aparecer em eventos, grupos, documentos, presenças, avaliações e demais tabelas vinculadas.</li>
                                <li>O e-mail atual é substituído e o histórico de e-mail também é anonimizado.</li>
                                <li>A autenticação em dois fatores é removida e a conta é desativada.</li>
                                <li>Não existe como desfazer, restaurar o nome ou voltar a entrar com este usuário.</li>
                            </ul>

                            <label className="flex items-start gap-3 rounded-xl border border-slate-200 bg-slate-50 p-4">
                                <input
                                    type="checkbox"
                                    className="mt-1 h-4 w-4"
                                    checked={confirmedPermanent}
                                    onChange={(e) => setConfirmedPermanent(e.target.checked)}
                                    disabled={isAnonymizing}
                                />
                                <span className="text-sm font-medium text-slate-800">
                                    Confirmo que entendo que esta exclusão é permanente e quero anonimizar meus dados.
                                </span>
                            </label>

                            {anonymizeError && (
                                <div className="flex items-center gap-2 rounded-lg bg-red-50 p-4 text-sm text-red-700">
                                    <AlertCircle className="h-5 w-5 shrink-0" />
                                    <span>{anonymizeError}</span>
                                </div>
                            )}
                        </div>

                        <DialogFooter className="mx-0 mb-0 gap-3 px-8 py-6 sm:px-10">
                            <button
                                type="button"
                                className="rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50"
                                onClick={() => setAnonymizeOpen(false)}
                                disabled={isAnonymizing}
                            >
                                Cancelar
                            </button>
                            <button
                                type="button"
                                className="inline-flex items-center justify-center gap-2 rounded-lg bg-red-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-50"
                                onClick={handleAnonymize}
                                disabled={!confirmedPermanent || isAnonymizing}
                            >
                                {isAnonymizing ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />}
                                Confirmar exclusão permanente
                            </button>
                        </DialogFooter>
                    </DialogContent>
                </Dialog>
            )}
        </div>
    );
};