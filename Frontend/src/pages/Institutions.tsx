import React, { useEffect, useMemo, useState, useCallback } from "react";
import { api } from "../services/api";
import { useAuth } from "../hooks/useAuth";
import type { OptionItem } from "../types/optionItem";
import { DeleteConfirmation } from "../components/ui/deleteConfirm";
import { PageHeader } from "../components/PageHeader";
import { CreateInstitutionForm } from "../components/institutions/CreateInstitutionForm";
import { type CreateInstitutionFormData } from "../schemas/institutionSchema";
import { unmaskCnpj } from "../utils/masks";
import {
    AlertCircle,
    Building2,
    ListFilter,
    Loader2,
    Pencil,
    Plus,
    Search,
    Trash2,
} from "lucide-react";

interface InstitutionDetail {
    id: string;
    name: string;
    cnpj?: string | null;
    address?: string | null;
    phone?: string | null;
    isActive: boolean;
    createdAt: string;
}

const extractErrorMessage = (err: any, fallback: string) => {
    if (err?.response?.status === 403) {
        return "Você não tem permissão para realizar esta ação.";
    }
    const data = err?.response?.data;
    if (data?.message) return data.message;
    if (data?.errors) {
        const firstField = Object.values(data.errors)[0];
        if (Array.isArray(firstField) && firstField.length) return firstField[0] as string;
    }
    return fallback;
};

const toPayload = (data: CreateInstitutionFormData) => ({
    name: data.name,
    cnpj: data.cnpj ? unmaskCnpj(data.cnpj) : undefined,
    address: data.address,
    phone: data.phone,
});

const MyInstitutionEditor: React.FC<{ institutionId: string }> = ({ institutionId }) => {
    const [loading, setLoading] = useState(true);
    const [loadError, setLoadError] = useState<string | null>(null);
    const [currentData, setCurrentData] = useState<CreateInstitutionFormData | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState<string | null>(null);
    const [isSuccess, setIsSuccess] = useState(false);

    useEffect(() => {
        api
            .get<InstitutionDetail>(`/institutions/${institutionId}`)
            .then(({ data }) => {
                setCurrentData({
                    name: data.name,
                    cnpj: data.cnpj ?? "",
                    address: data.address ?? "",
                    phone: data.phone ?? "",
                });
            })
            .catch((err) =>
                setLoadError(extractErrorMessage(err, "Não foi possível carregar sua instituição.")),
            )
            .finally(() => setLoading(false));
    }, [institutionId]);

    const handleUpdate = async (data: CreateInstitutionFormData) => {
        setIsSubmitting(true);
        setSubmitError(null);
        try {
            await api.put(`/institutions/${institutionId}`, toPayload(data));
            setIsSuccess(true);
            setTimeout(() => setIsSuccess(false), 1500);
        } catch (err: any) {
            setSubmitError(extractErrorMessage(err, "Não foi possível salvar a instituição."));
        } finally {
            setIsSubmitting(false);
        }
    };

    if (loading) {
        return (
            <div className="flex min-h-[60vh] items-center justify-center text-indigo-600">
                <Loader2 className="h-8 w-8 animate-spin" />
            </div>
        );
    }

    return (
        <main className="app-page">
            <PageHeader title="Minha instituição" description="Edite os dados da sua instituição." />
            {loadError && (
                <div className="mb-4 flex items-center gap-2 rounded-lg bg-red-50 p-3 text-sm text-red-700">
                    <AlertCircle className="h-5 w-5" />
                    {loadError}
                </div>
            )}
            <CreateInstitutionForm
                currentInstitutionData={currentData}
                onSubmit={handleUpdate}
                isSubmitting={isSubmitting}
                isSuccess={isSuccess}
                apiError={submitError}
            />
        </main>
    );
};

const AllInstitutionsManager: React.FC = () => {
    const [institutions, setInstitutions] = useState<OptionItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [listError, setListError] = useState<string | null>(null);
    const [search, setSearch] = useState("");

    const [editing, setEditing] = useState(false);
    const [currentInstitutionBeingUpdated, setCurrentInstitutionBeingUpdated] = useState<
        (CreateInstitutionFormData & { id: string }) | null
        >(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState<string | null>(null);
    const [isSuccess, setIsSuccess] = useState(false);

    const loadInstitutions = useCallback(async () => {
        try {
            const response = await api.get<OptionItem[]>("/institutions");
            setInstitutions(response.data);
        } catch (err: any) {
            setListError(extractErrorMessage(err, "Não foi possível carregar as instituições."));
        }
    }, []);

    useEffect(() => {
        loadInstitutions().finally(() => setLoading(false));
    }, [loadInstitutions]);

    const filteredInstitutions = useMemo(
        () => institutions.filter((item) => item.label.toLowerCase().includes(search.toLowerCase())),
        [institutions, search],
    );

    const openCreate = () => {
        setCurrentInstitutionBeingUpdated(null);
        setSubmitError(null);
        setEditing(true);
    };

    const handleStartEdition = async (item: OptionItem) => {
        setListError(null);
        try {
            const { data } = await api.get<InstitutionDetail>(`/institutions/${item.value}`);
            setCurrentInstitutionBeingUpdated({
                id: data.id,
                name: data.name,
                cnpj: data.cnpj ?? "",
                address: data.address ?? "",
                phone: data.phone ?? "",
            });
            setSubmitError(null);
            setEditing(true);
        } catch (err: any) {
            setListError(extractErrorMessage(err, "Não foi possível carregar esta instituição."));
        }
    };

    const handleCreateInstitution = async (data: CreateInstitutionFormData) => {
        setIsSubmitting(true);
        setSubmitError(null);
        try {
            if (currentInstitutionBeingUpdated) {
                await api.put(`/institutions/${currentInstitutionBeingUpdated.id}`, toPayload(data));
            } else {
                await api.post("/institutions", toPayload(data));
            }
            await loadInstitutions();
            setIsSuccess(true);
            setTimeout(() => {
                setIsSuccess(false);
                setEditing(false);
            }, 1200);
        } catch (err: any) {
            setSubmitError(extractErrorMessage(err, "Não foi possível salvar a instituição."));
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleDelete = async (id: string) => {
        setListError(null);
        try {
            await api.delete(`/institutions/${id}`);
            await loadInstitutions();
        } catch (err: any) {
            setListError(extractErrorMessage(err, "Não foi possível excluir a instituição."));
        }
    };

    if (loading) {
        return (
            <div className="flex min-h-[60vh] items-center justify-center text-indigo-600">
                <Loader2 className="h-8 w-8 animate-spin" />
            </div>
        );
    }

    return (
        <main className="app-page">
            <PageHeader
                title={
                    editing
                        ? currentInstitutionBeingUpdated
                            ? "Atualizar instituição"
                            : "Nova instituição"
                        : "Instituições"
                }
                description={
                    editing
                        ? "Preencha os dados para salvar a instituição."
                        : "Cadastre, edite e gerencie as instituições parceiras."
                }
                action={
                    <button
                        onClick={() => (editing ? setEditing(false) : openCreate())}
                        className={editing ? "secondary-action" : "primary-action"}>
                        {editing ? (
                            <>
                                <ListFilter className="h-4 w-4" /> Ver lista
                            </>
                        ) : (
                            <>
                                <Plus className="h-4 w-4" /> Nova instituição
                            </>
                        )}
                    </button>
                }
            />

            {listError && !editing && (
                <div className="mb-4 flex items-center gap-2 rounded-lg bg-red-50 p-3 text-sm text-red-700">
                    <AlertCircle className="h-5 w-5" />
                    {listError}
                </div>
            )}

            {editing ? (
                <CreateInstitutionForm
                    currentInstitutionData={currentInstitutionBeingUpdated}
                    onSubmit={handleCreateInstitution}
                    isSubmitting={isSubmitting}
                    isSuccess={isSuccess}
                    apiError={submitError}
                />
            ) : (
                <>
                    <div className="filter-bar">
                        <label className="relative block">
                            <Search className="absolute left-3 top-2.5 h-5 w-5 text-slate-400" />
                            <input
                                value={search}
                                onChange={(e) => setSearch(e.target.value)}
                                placeholder="Buscar por nome"
                                className="form-control pl-10"
                            />
                        </label>
                    </div>

                    <div className="surface-card overflow-x-auto">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th className="px-4 py-3">Nome</th>
                                    <th className="px-4 py-3 text-right">Ações</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredInstitutions.map((item) => (
                                    <tr key={item.value} className="hover:bg-slate-50">
                                        <td className="px-4 py-3">
                                            <div className="flex items-center gap-2 font-medium text-slate-900">
                                                <Building2 className="h-4 w-4 text-slate-400" />
                                                {item.label}
                                            </div>
                                        </td>
                                        <td className="px-4 py-3">
                                            <div className="flex justify-end gap-1">
                                                <button
                                                    title="Editar"
                                                    onClick={() => handleStartEdition(item)}
                                                    className="rounded p-2 text-indigo-600 hover:bg-indigo-50">
                                                    <Pencil className="h-4 w-4" />
                                                </button>
                                                <DeleteConfirmation
                                                    descriptionText={`Excluir "${item.label}"? Essa ação não pode ser desfeita e vai falhar se houver usuários, cursos ou eventos vinculados.`}
                                                    confirmationText="Excluir"
                                                    onDelete={() => handleDelete(item.value)}>
                                                    <button title="Excluir" className="rounded p-2 text-red-600 hover:bg-red-50">
                                                        <Trash2 className="h-4 w-4" />
                                                    </button>
                                                </DeleteConfirmation>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                                {!filteredInstitutions.length && (
                                    <tr>
                                        <td colSpan={2} className="px-4 py-10 text-center text-slate-500">
                                            Nenhuma instituição encontrada.
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </>
            )}
        </main>
    );
};

export const Institutions: React.FC = () => {
    const { user } = useAuth();
    const userInstitutionId = user?.institutionId || null;

    if (userInstitutionId) {
        return <MyInstitutionEditor institutionId={userInstitutionId} />;
    }
    return <AllInstitutionsManager />;
};