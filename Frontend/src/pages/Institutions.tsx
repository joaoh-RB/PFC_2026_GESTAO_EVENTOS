import React, { useEffect, useMemo, useState } from "react";
import { api } from "../services/api";
import type { OptionItem } from "../types/optionItem";
import { DeleteConfirmation } from "../components/ui/deleteConfirm";
import { PageHeader } from "../components/PageHeader";
import { maskCnpj, maskPhone, unmaskCnpj, unmaskPhone } from "../utils/masks";
import {
    AlertCircle,
    Building2,
    Loader2,
    Pencil,
    Plus,
    Search,
    Trash2,
    X,
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

interface InstitutionForm {
    name: string;
    cnpj: string;
    address: string;
    phone: string;
}

const emptyForm: InstitutionForm = { name: "", cnpj: "", address: "", phone: "" };

// Padroniza a mensagem de erro vinda da API. Como o Forbid() n├úo devolve
// corpo nenhum (s├│ o status 403), tratamos esse caso ├á parte ÔÇö sen├úo a tela
// mostraria "undefined" pro usu├írio.
const extractErrorMessage = (err: any, fallback: string) => {
    if (err?.response?.status === 403) {
        return "Voc├¬ n├úo tem permiss├úo para realizar esta a├º├úo.";
    }
    return err?.response?.data?.message || fallback;
};

export const Institutions: React.FC = () => {
    const [institutions, setInstitutions] = useState<OptionItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [search, setSearch] = useState("");

    const [showForm, setShowForm] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [form, setForm] = useState<InstitutionForm>(emptyForm);

    const loadInstitutions = async () => {
        const response = await api.get<OptionItem[]>("/institutions");
        setInstitutions(response.data);
    };

    useEffect(() => {
        loadInstitutions()
            .catch((err) =>
                setError(extractErrorMessage(err, "N├úo foi poss├¡vel carregar as institui├º├Áes.")),
            )
            .finally(() => setLoading(false));
    }, []);

    const filteredInstitutions = useMemo(
        () =>
            institutions.filter((item) =>
                item.label.toLowerCase().includes(search.toLowerCase()),
            ),
        [institutions, search],
    );

    const openCreate = () => {
        setEditingId(null);
        setForm(emptyForm);
        setShowForm(true);
        setError(null);
    };

    // Busca o detalhe (GET /institutions/{id}) porque a listagem s├│ traz nome ÔÇö
    // ├® aqui que testamos de fato o endpoint da Tarefa 8.1.
    const openEdit = async (item: OptionItem) => {
        setError(null);
        try {
            const { data } = await api.get<InstitutionDetail>(`/institutions/${item.value}`);
            setEditingId(data.id);
            setForm({
                name: data.name,
                cnpj: data.cnpj ? maskCnpj(data.cnpj) : "",
                address: data.address ?? "",
                phone: data.phone ? maskPhone(data.phone) : "",
            });
            setShowForm(true);
        } catch (err: any) {
            setError(extractErrorMessage(err, "N├úo foi poss├¡vel carregar esta institui├º├úo."));
        }
    };

    const closeForm = () => {
        setShowForm(false);
        setEditingId(null);
        setForm(emptyForm);
    };

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();
        setSaving(true);
        setError(null);
        try {
            const payload = {
                name: form.name,
                cnpj: form.cnpj ? unmaskCnpj(form.cnpj) : undefined,
                address: form.address || undefined,
                phone: form.phone ? unmaskPhone(form.phone) : undefined,
            };
            // PUT quando est├í editando (Tarefa 8.2), POST quando ├® cadastro novo.
            if (editingId) await api.put(`/institutions/${editingId}`, payload);
            else await api.post("/institutions", payload);
            await loadInstitutions();
            closeForm();
        } catch (err: any) {
            setError(extractErrorMessage(err, "N├úo foi poss├¡vel salvar a institui├º├úo."));
        } finally {
            setSaving(false);
        }
    };

    // DELETE (Tarefa 8.3). Se o backend acusar v├¡nculo (HasDependenciesAsync),
    // a mensagem de neg├│cio j├í vem pronta no response.data.message.
    const remove = async (id: string) => {
        setError(null);
        try {
            await api.delete(`/institutions/${id}`);
            await loadInstitutions();
        } catch (err: any) {
            setError(extractErrorMessage(err, "N├úo foi poss├¡vel excluir a institui├º├úo."));
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
                    title="Institui├º├Áes"
                    description="Cadastre, edite e gerencie as institui├º├Áes parceiras."
                    action={
                    <button
                        onClick={openCreate}
                        className="primary-action">
                        <Plus className="h-4 w-4" /> Nova institui├º├úo
                    </button>
                    }
                />

                {error && (
                    <div className="mb-4 flex items-center gap-2 rounded-lg bg-red-50 p-3 text-sm text-red-700">
                        <AlertCircle className="h-5 w-5" />
                        {error}
                    </div>
                )}

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
                                <th className="px-4 py-3 text-right">A├º├Áes</th>
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
                                                onClick={() => openEdit(item)}
                                                className="rounded p-2 text-indigo-600 hover:bg-indigo-50">
                                                <Pencil className="h-4 w-4" />
                                            </button>
                                            <DeleteConfirmation
                                                descriptionText={`Excluir "${item.label}"? Essa a├º├úo n├úo pode ser desfeita e vai falhar se houver usu├írios, cursos ou eventos vinculados.`}
                                                confirmationText="Excluir"
                                                onDelete={() => remove(item.value)}>
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
                                        Nenhuma institui├º├úo encontrada.
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            {showForm && (
                <div className="modal-backdrop">
                    <div className="modal-panel max-w-lg">
                        <div className="mb-5 flex items-center justify-between">
                            <h2 className="text-xl font-bold text-slate-900">
                                {editingId ? "Editar institui├º├úo" : "Nova institui├º├úo"}
                            </h2>
                            <button onClick={closeForm} className="rounded p-1 text-slate-500 hover:bg-slate-100">
                                <X />
                            </button>
                        </div>
                        <form onSubmit={submit} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                            <label className="sm:col-span-2 text-sm font-medium text-slate-700">
                                Nome
                                <input
                                    required
                                    value={form.name}
                                    onChange={(e) => setForm({ ...form, name: e.target.value })}
                                    className="form-control mt-1"
                                />
                            </label>
                            <label className="text-sm font-medium text-slate-700">
                                CNPJ
                                <input
                                    value={form.cnpj}
                                    onChange={(e) => setForm({ ...form, cnpj: maskCnpj(e.target.value) })}
                                    maxLength={18}
                                    placeholder="00.000.000/0000-00"
                                    className="form-control mt-1"
                                />
                            </label>
                            <label className="text-sm font-medium text-slate-700">
                                Telefone
                                <input
                                    value={form.phone}
                                    onChange={(e) => setForm({ ...form, phone: maskPhone(e.target.value) })}
                                    maxLength={15}
                                    placeholder="(11) 91234-5678"
                                    className="form-control mt-1"
                                />
                            </label>
                            <label className="sm:col-span-2 text-sm font-medium text-slate-700">
                                Endere├ºo
                                <input
                                    value={form.address}
                                    onChange={(e) => setForm({ ...form, address: e.target.value })}
                                    className="form-control mt-1"
                                />
                            </label>
                            <div className="sm:col-span-2 flex justify-end gap-3 pt-2">
                                <button
                                    type="button"
                                    onClick={closeForm}
                                    className="secondary-action">
                                    Cancelar
                                </button>
                                <button
                                    disabled={saving}
                                    className="primary-action">
                                    {saving && <Loader2 className="h-4 w-4 animate-spin" />}
                                    Salvar
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </main>
    );
};
