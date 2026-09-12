import React, { useEffect, useMemo, useState } from "react";
import { api } from "../services/api";
import { useAuth } from "../hooks/useAuth";
import type { OptionItem } from "../types/optionItem";
import { DeleteConfirmation } from "../components/ui/deleteConfirm";
import {
    AlertCircle,
    BookOpen,
    Loader2,
    Pencil,
    Plus,
    Search,
    Trash2,
    X,
} from "lucide-react";

interface CourseDetail {
    id: string;
    name: string;
    institutionId: string;
    isActive: boolean;
    createdAt: string;
}

const extractErrorMessage = (err: any, fallback: string) => {
    if (err?.response?.status === 403) {
        return "Você não tem permissão para realizar esta ação.";
    }
    return err?.response?.data?.message || fallback;
};

export const Courses: React.FC = () => {
    const { user } = useAuth();
    const userInstitutionId = user?.institutionId || null;

    const [institutions, setInstitutions] = useState<OptionItem[]>([]);
    const [selectedInstitutionId, setSelectedInstitutionId] = useState(userInstitutionId ?? "");
    const [courses, setCourses] = useState<OptionItem[]>([]);

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [search, setSearch] = useState("");

    const [showForm, setShowForm] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [name, setName] = useState("");

    useEffect(() => {
        if (userInstitutionId) {
            setLoading(false);
            return;
        }
        api
            .get<OptionItem[]>("/institutions")
            .then((res) => setInstitutions(res.data))
            .catch((err) =>
                setError(extractErrorMessage(err, "Não foi possível carregar as instituições.")),
            )
            .finally(() => setLoading(false));
    }, [userInstitutionId]);

    const loadCourses = async (institutionId: string) => {
        if (!institutionId) {
            setCourses([]);
            return;
        }
        const response = await api.get<OptionItem[]>("/courses", {
            params: { institutionId },
        });
        setCourses(response.data);
    };

    useEffect(() => {
        loadCourses(selectedInstitutionId).catch((err) =>
            setError(extractErrorMessage(err, "Não foi possível carregar os cursos.")),
        );
    }, [selectedInstitutionId]);

    const filteredCourses = useMemo(
        () => courses.filter((item) => item.label.toLowerCase().includes(search.toLowerCase())),
        [courses, search],
    );

    const openCreate = () => {
        setEditingId(null);
        setName("");
        setShowForm(true);
        setError(null);
    };

    const openEdit = async (item: OptionItem) => {
        setError(null);
        try {
            const { data } = await api.get<CourseDetail>(`/courses/${item.value}`);
            setEditingId(data.id);
            setName(data.name);
            setShowForm(true);
        } catch (err: any) {
            setError(extractErrorMessage(err, "Não foi possível carregar este curso."));
        }
    };

    const closeForm = () => {
        setShowForm(false);
        setEditingId(null);
        setName("");
    };

    const submit = async (event: React.FormEvent) => {
        event.preventDefault();
        if (!editingId && !selectedInstitutionId) {
            setError("Selecione uma instituição antes de cadastrar um curso.");
            return;
        }
        setSaving(true);
        setError(null);
        try {
            if (editingId) {
                await api.put(`/courses/${editingId}`, { name });
            } else {
                await api.post("/courses", { name, institutionId: selectedInstitutionId });
            }
            await loadCourses(selectedInstitutionId);
            closeForm();
        } catch (err: any) {
            setError(extractErrorMessage(err, "Não foi possível salvar o curso."));
        } finally {
            setSaving(false);
        }
    };

    const remove = async (id: string) => {
        setError(null);
        try {
            await api.delete(`/courses/${id}`);
            await loadCourses(selectedInstitutionId);
        } catch (err: any) {
            setError(extractErrorMessage(err, "Não foi possível excluir o curso."));
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
        <main className="min-h-screen bg-slate-50 px-4 py-8 sm:px-6 lg:px-8">
            <div className="mx-auto max-w-5xl">
                <div className="mb-6 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                    <div>
                        <h1 className="text-2xl font-bold text-slate-900">Cursos</h1>
                        <p className="text-sm text-slate-600">
                            {userInstitutionId
                                ? "Veja, edite e remova os cursos da sua instituição."
                                : "Escolha uma instituição para ver, editar e remover os cursos dela."}
                        </p>
                    </div>
                    <button
                        onClick={openCreate}
                        disabled={!selectedInstitutionId}
                        title={!selectedInstitutionId ? "Selecione uma instituição primeiro" : undefined}
                        className="inline-flex items-center justify-center gap-2 rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                        <Plus className="h-4 w-4" /> Novo curso
                    </button>
                </div>

                {error && (
                    <div className="mb-4 flex items-center gap-2 rounded-lg bg-red-50 p-3 text-sm text-red-700">
                        <AlertCircle className="h-5 w-5" />
                        {error}
                    </div>
                )}

                <div className="mb-4 flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 sm:flex-row">
                    {!userInstitutionId && (
                        <select
                            value={selectedInstitutionId}
                            onChange={(e) => setSelectedInstitutionId(e.target.value)}
                            className="rounded-lg border border-slate-300 px-3 py-2 text-sm sm:w-72">
                            <option value="">Selecione a instituição</option>
                            {institutions.map((item) => (
                                <option key={item.value} value={item.value}>
                                    {item.label}
                                </option>
                            ))}
                        </select>
                    )}
                    <label className="relative flex-1">
                        <Search className="absolute left-3 top-2.5 h-5 w-5 text-slate-400" />
                        <input
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            placeholder="Buscar por nome do curso"
                            className="w-full rounded-lg border border-slate-300 py-2 pl-10 pr-3 text-sm"
                        />
                    </label>
                </div>

                <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
                    <table className="w-full text-left text-sm">
                        <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                            <tr>
                                <th className="px-4 py-3">Nome</th>
                                <th className="px-4 py-3 text-right">Ações</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                            {filteredCourses.map((item) => (
                                <tr key={item.value} className="hover:bg-slate-50">
                                    <td className="px-4 py-3">
                                        <div className="flex items-center gap-2 font-medium text-slate-900">
                                            <BookOpen className="h-4 w-4 text-slate-400" />
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
                                                descriptionText={`Excluir "${item.label}"? Essa ação não pode ser desfeita e vai falhar se houver alunos, professores ou eventos vinculados.`}
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
                            {!filteredCourses.length && (
                                <tr>
                                    <td colSpan={2} className="px-4 py-10 text-center text-slate-500">
                                        {selectedInstitutionId
                                            ? "Nenhum curso encontrado."
                                            : "Selecione uma instituição para ver os cursos."}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {showForm && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
                    <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
                        <div className="mb-5 flex items-center justify-between">
                            <h2 className="text-xl font-bold text-slate-900">
                                {editingId ? "Editar curso" : "Novo curso"}
                            </h2>
                            <button onClick={closeForm} className="rounded p-1 text-slate-500 hover:bg-slate-100">
                                <X />
                            </button>
                        </div>
                        <form onSubmit={submit} className="space-y-4">
                            <label className="block text-sm font-medium text-slate-700">
                                Nome do curso
                                <input
                                    required
                                    value={name}
                                    onChange={(e) => setName(e.target.value)}
                                    className="mt-1 w-full rounded-lg border border-slate-300 p-2.5"
                                />
                            </label>
                            <div className="flex justify-end gap-3 pt-2">
                                <button
                                    type="button"
                                    onClick={closeForm}
                                    className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium">
                                    Cancelar
                                </button>
                                <button
                                    disabled={saving}
                                    className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white disabled:bg-indigo-400">
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