import React, { useEffect, useMemo, useState } from "react";
import { api } from "../services/api";
import type { OptionItem } from "../types/optionItem";
import { DeleteConfirmation } from "../components/ui/deleteConfirm";
import { PageHeader } from "../components/PageHeader";
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
    const [institutions, setInstitutions] = useState<OptionItem[]>([]);
    const [selectedInstitutionId, setSelectedInstitutionId] = useState("");
    const [courses, setCourses] = useState<OptionItem[]>([]);

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [search, setSearch] = useState("");

    const [showForm, setShowForm] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [name, setName] = useState("");

    useEffect(() => {
        api
            .get<OptionItem[]>("/institutions")
            .then((res) => setInstitutions(res.data))
            .catch((err) =>
                setError(extractErrorMessage(err, "Não foi possível carregar as instituições.")),
            )
            .finally(() => setLoading(false));
    }, []);

    const loadCourses = async (institutionId: string) => {
        // GET /courses só retorna algo quando institutionId é informado
        // (ver CourseService.GetCoursesForSelectAsync) — por isso a tela exige
        // escolher a instituição primeiro.
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
                // PUT /courses/{id} (Tarefa 8.2) — só o nome é editável, o curso
                // continua vinculado à mesma instituição.
                await api.put(`/courses/${editingId}`, { name });
            } else {
                // POST /courses (Tarefa 8.4) — agora aceita Administrador OU
                // professor-admin da própria instituição.
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
            // DELETE /courses/{id} (Tarefa 8.3)
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
        <main className="app-page">
                <PageHeader
                    title="Cursos"
                    description="Escolha uma instituição para consultar e gerenciar seus cursos."
                    action={
                    <button
                        onClick={openCreate}
                        disabled={!selectedInstitutionId}
                        title={!selectedInstitutionId ? "Selecione uma instituição primeiro" : undefined}
                        className="primary-action">
                        <Plus className="h-4 w-4" /> Novo curso
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
                    <select
                        value={selectedInstitutionId}
                        onChange={(e) => setSelectedInstitutionId(e.target.value)}
                        className="form-control sm:w-72">
                        <option value="">Selecione a instituição</option>
                        {institutions.map((item) => (
                            <option key={item.value} value={item.value}>
                                {item.label}
                            </option>
                        ))}
                    </select>
                    <label className="relative flex-1">
                        <Search className="absolute left-3 top-2.5 h-5 w-5 text-slate-400" />
                        <input
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            placeholder="Buscar por nome do curso"
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
            {showForm && (
                <div className="modal-backdrop">
                    <div className="modal-panel max-w-md">
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
                                    className="form-control mt-1"
                                />
                            </label>
                            <div className="flex justify-end gap-3 pt-2">
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