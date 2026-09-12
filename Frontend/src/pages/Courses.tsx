import React, { useEffect, useMemo, useState, useCallback } from "react";
import { api } from "../services/api";
import { useAuth } from "../hooks/useAuth";
import type { OptionItem } from "../types/optionItem";
import { DeleteConfirmation } from "../components/ui/deleteConfirm";
import { PageHeader } from "../components/PageHeader";
import { CreateCourseForm } from "../components/courses/CreateCourseForm";
import { type CreateCourseFormData } from "../schemas/courseSchema";
import {
    AlertCircle,
    BookOpen,
    ListFilter,
    Loader2,
    Pencil,
    Plus,
    Search,
    Trash2,
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
    const data = err?.response?.data;
    if (data?.message) return data.message;
    if (data?.errors) {
        const firstField = Object.values(data.errors)[0];
        if (Array.isArray(firstField) && firstField.length) return firstField[0] as string;
    }
    return fallback;
};

export const Courses: React.FC = () => {
    const { user } = useAuth();
    const userInstitutionId = user?.institutionId || null;

    const [institutions, setInstitutions] = useState<OptionItem[]>([]);
    const [selectedInstitutionId, setSelectedInstitutionId] = useState(userInstitutionId ?? "");
    const [courses, setCourses] = useState<OptionItem[]>([]);

    const [loading, setLoading] = useState(true);
    const [listError, setListError] = useState<string | null>(null);
    const [search, setSearch] = useState("");

    const [editing, setEditing] = useState(false);
    const [currentCourseBeingUpdated, setCurrentCourseBeingUpdated] = useState<
        (CreateCourseFormData & { id: string }) | null
        >(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState<string | null>(null);
    const [isSuccess, setIsSuccess] = useState(false);

    useEffect(() => {
        if (userInstitutionId) {
            setLoading(false);
            return;
        }
        api
            .get<OptionItem[]>("/institutions")
            .then((res) => setInstitutions(res.data))
            .catch((err) =>
                setListError(extractErrorMessage(err, "Não foi possível carregar as instituições.")),
            )
            .finally(() => setLoading(false));
    }, [userInstitutionId]);

    const loadCourses = useCallback(async (institutionId: string) => {
        if (!institutionId) {
            setCourses([]);
            return;
        }
        try {
            const response = await api.get<OptionItem[]>("/courses", {
                params: { institutionId },
            });
            setCourses(response.data);
        } catch (err: any) {
            setListError(extractErrorMessage(err, "Não foi possível carregar os cursos."));
        }
    }, []);

    useEffect(() => {
        if (!editing) {
            loadCourses(selectedInstitutionId);
        }
    }, [editing, selectedInstitutionId, loadCourses]);

    const filteredCourses = useMemo(
        () => courses.filter((item) => item.label.toLowerCase().includes(search.toLowerCase())),
        [courses, search],
    );

    const openCreate = () => {
        setCurrentCourseBeingUpdated(null);
        setSubmitError(null);
        setEditing(true);
    };

    const handleStartEdition = async (item: OptionItem) => {
        setListError(null);
        try {
            const { data } = await api.get<CourseDetail>(`/courses/${item.value}`);
            setCurrentCourseBeingUpdated({ id: data.id, name: data.name, institutionId: data.institutionId });
            setSubmitError(null);
            setEditing(true);
        } catch (err: any) {
            setListError(extractErrorMessage(err, "Não foi possível carregar este curso."));
        }
    };

    const handleCreateCourse = async (data: CreateCourseFormData) => {
        setIsSubmitting(true);
        setSubmitError(null);
        try {
            if (currentCourseBeingUpdated) {
                await api.put(`/courses/${currentCourseBeingUpdated.id}`, { name: data.name });
            } else {
                await api.post("/courses", { name: data.name, institutionId: data.institutionId });
                if (!userInstitutionId) setSelectedInstitutionId(data.institutionId);
            }
            setIsSuccess(true);
            setTimeout(() => {
                setIsSuccess(false);
                setEditing(false);
            }, 1200);
        } catch (err: any) {
            setSubmitError(extractErrorMessage(err, "Não foi possível salvar o curso."));
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleDelete = async (id: string) => {
        setListError(null);
        try {
            await api.delete(`/courses/${id}`);
            await loadCourses(selectedInstitutionId);
        } catch (err: any) {
            setListError(extractErrorMessage(err, "Não foi possível excluir o curso."));
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
                title={editing ? (currentCourseBeingUpdated ? "Atualizar curso" : "Novo curso") : "Cursos"}
                description={
                    editing
                        ? "Preencha os dados para salvar o curso."
                        : userInstitutionId
                            ? "Veja, edite e remova os cursos da sua instituição."
                            : "Escolha uma instituição para consultar e gerenciar seus cursos."
                }
                action={
                    <button
                        onClick={() => (editing ? setEditing(false) : openCreate())}
                        disabled={!editing && !selectedInstitutionId}
                        title={!editing && !selectedInstitutionId ? "Selecione uma instituição primeiro" : undefined}
                        className={editing ? "secondary-action" : "primary-action"}>
                        {editing ? (
                            <>
                                <ListFilter className="h-4 w-4" /> Ver lista de cursos
                            </>
                        ) : (
                            <>
                                <Plus className="h-4 w-4" /> Novo curso
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
                <CreateCourseForm
                    institutions={institutions}
                    userInstitutionId={userInstitutionId}
                    currentCourseData={currentCourseBeingUpdated}
                    onSubmit={handleCreateCourse}
                    isSubmitting={isSubmitting}
                    isSuccess={isSuccess}
                    apiError={submitError}
                />
            ) : (
                <>
                    <div className="filter-bar">
                        {!userInstitutionId && (
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
                        )}
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
                                                    onClick={() => handleStartEdition(item)}
                                                    className="rounded p-2 text-indigo-600 hover:bg-indigo-50">
                                                    <Pencil className="h-4 w-4" />
                                                </button>
                                                <DeleteConfirmation
                                                    descriptionText={`Excluir "${item.label}"? Essa ação não pode ser desfeita e vai falhar se houver alunos, professores ou eventos vinculados.`}
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
                </>
            )}
        </main>
    );
};