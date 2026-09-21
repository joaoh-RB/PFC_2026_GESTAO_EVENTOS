import React, { useEffect, useMemo, useState, useCallback } from "react";
import { api } from "../services/api";
import { useAuth } from "../hooks/useAuth";
import type { OptionItem } from "../types/optionItem";
import { DeleteConfirmation } from "../components/ui/deleteConfirm";
import { PageHeader } from "../components/PageHeader";
import { CreateCourseForm } from "../components/courses/CreateCourseForm";
import { type CreateCourseFormData } from "../schemas/courseSchema";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  AlertCircle,
  BookOpen,
  Check,
  CircleOff,
  ListFilter,
  Loader2,
  Pencil,
  Plus,
  Search,
} from "lucide-react";
import {
  InputGroup,
  InputGroupAddon,
  InputGroupInput,
} from "@/components/ui/input-group";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface CourseDetail {
  id: string;
  name: string;
  institutionId: string;
  isActive: boolean;
  createdAt: string;
}

interface CourseManagementItem {
  id: string;
  name: string;
  isActive: boolean;
}

const extractErrorMessage = (err: any, fallback: string) => {
  if (err?.response?.status === 403) {
    return "Você não tem permissão para realizar esta ação.";
  }
  const data = err?.response?.data;
  if (data?.message) return data.message;
  if (data?.errors) {
    const firstField = Object.values(data.errors)[0];
    if (Array.isArray(firstField) && firstField.length)
      return firstField[0] as string;
  }
  return fallback;
};

export const Courses: React.FC = () => {
  const { user } = useAuth();
  const userInstitutionId = user?.institutionId || null;

  const [institutions, setInstitutions] = useState<OptionItem[]>([]);
  const [selectedInstitutionId, setSelectedInstitutionId] = useState(
    userInstitutionId ?? "",
  );
  const [courses, setCourses] = useState<CourseManagementItem[]>([]);

  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
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
        setListError(
          extractErrorMessage(
            err,
            "Não foi possível carregar as instituições.",
          ),
        ),
      )
      .finally(() => setLoading(false));
  }, [userInstitutionId]);

  const loadCourses = useCallback(async (institutionId: string) => {
    if (!institutionId) {
      setCourses([]);
      return;
    }
    try {
      const response = await api.get<CourseManagementItem[]>("/courses/management", {
        params: { institutionId },
      });
      setCourses(response.data);
    } catch (err: any) {
      setListError(
        extractErrorMessage(err, "Não foi possível carregar os cursos."),
      );
    }
  }, []);

  useEffect(() => {
    if (!editing) {
      loadCourses(selectedInstitutionId);
    }
  }, [editing, selectedInstitutionId, loadCourses]);

  const filteredCourses = useMemo(
    () =>
      courses.filter((item) =>
        item.name.toLowerCase().includes(search.toLowerCase()),
      ),
    [courses, search],
  );

  const openCreate = () => {
    setCurrentCourseBeingUpdated(null);
    setSubmitError(null);
    setEditing(true);
  };

  const handleStartEdition = async (item: CourseManagementItem) => {
    setListError(null);
    try {
      const { data } = await api.get<CourseDetail>(`/courses/${item.id}`);
      setCurrentCourseBeingUpdated({
        id: data.id,
        name: data.name,
        institutionId: data.institutionId,
      });
      setSubmitError(null);
      setEditing(true);
    } catch (err: any) {
      setListError(
        extractErrorMessage(err, "Não foi possível carregar este curso."),
      );
    }
  };

  const handleCreateCourse = async (data: CreateCourseFormData) => {
    setIsSubmitting(true);
    setSubmitError(null);
    try {
      if (currentCourseBeingUpdated) {
        await api.put(`/courses/${currentCourseBeingUpdated.id}`, {
          name: data.name,
        });
      } else {
        await api.post("/courses", {
          name: data.name,
          institutionId: data.institutionId,
        });
        if (!userInstitutionId) setSelectedInstitutionId(data.institutionId);
      }
      setIsSuccess(true);
      setTimeout(() => {
        setIsSuccess(false);
        setEditing(false);
      }, 1200);
    } catch (err: any) {
      setSubmitError(
        extractErrorMessage(err, "Não foi possível salvar o curso."),
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const toggleActive = async (item: CourseManagementItem) => {
    setListError(null);
    try {
      await api.patch(`/courses/${item.id}/active`, { isActive: !item.isActive });
      await loadCourses(selectedInstitutionId);
      setSuccessMessage(
        item.isActive
          ? "Curso inativado com sucesso."
          : "Curso ativado com sucesso.",
      );
      setTimeout(() => setSuccessMessage(null), 1400);
    } catch (err: any) {
      setListError(
        extractErrorMessage(err, "Não foi possível alterar a situação."),
      );
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
            ? currentCourseBeingUpdated
              ? "Atualizar curso"
              : "Novo curso"
            : "Cursos"
        }
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
            title={
              !editing && !selectedInstitutionId
                ? "Selecione uma instituição primeiro"
                : undefined
            }
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
      {successMessage && !editing && (
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-emerald-50 p-3 text-sm text-emerald-700">
          <Check className="h-5 w-5" />
          {successMessage}
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
              <div className={"relative flex-1"}>
                <Select
                  value={selectedInstitutionId}
                  items={institutions}
                  onValueChange={(e) => setSelectedInstitutionId(e ?? "")}>
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Selecione a instituição"></SelectValue>
                  </SelectTrigger>
                  <SelectContent alignItemWithTrigger={true}>
                    <SelectGroup>
                      <SelectItem key={""} value="">
                        Selecione a instituição
                      </SelectItem>
                      {institutions.map((item) => (
                        <SelectItem key={item.value} value={item.value}>
                          {item.label}
                        </SelectItem>
                      ))}
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </div>
            )}
            <div className="relative flex-1">
              <InputGroup>
                <InputGroupInput
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="Buscar por nome do curso"></InputGroupInput>
                <InputGroupAddon>
                  <Search />
                </InputGroupAddon>
              </InputGroup>
            </div>
          </div>

          <div className="surface-card overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>Acesso</TableHead>
                  <TableHead className="text-center">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredCourses.map((item) => (
                  <TableRow key={item.id} className="hover:bg-slate-50">
                    <TableCell>
                      <div className="flex items-center gap-2 font-medium text-slate-900">
                        <BookOpen className="h-4 w-4 text-slate-400" />
                        {item.name}
                      </div>
                    </TableCell>
                    <TableCell>
                      <span
                        className={
                          item.isActive ? "text-emerald-700" : "text-slate-500"
                        }>
                        {item.isActive ? "Ativo" : "Inativo"}
                      </span>
                    </TableCell>
                    <TableCell>
                      <div className="flex w-full items-center justify-center gap-3">
                        <button
                          title="Editar"
                          onClick={() => handleStartEdition(item)}
                          className="rounded p-2 text-indigo-600 hover:bg-indigo-50">
                          <Pencil className="h-4 w-4" />
                        </button>
                        {item.isActive ? (
                          <DeleteConfirmation
                            children={
                              <button
                                title="Inativar"
                                className="rounded p-2 text-red-600 hover:bg-red-50">
                                <CircleOff className="h-4 w-4" />
                              </button>
                            }
                            onDelete={() => toggleActive(item)}
                            descriptionText={`Tem certeza que deseja inativar o curso "${item.name}"?`}
                            confirmationText="Inativar"
                          />
                        ) : (
                          <DeleteConfirmation
                            children={
                              <button
                                title="Ativar"
                                className="rounded p-2 text-emerald-600 hover:bg-emerald-50">
                                <Check className="h-4 w-4" />
                              </button>
                            }
                            onDelete={() => toggleActive(item)}
                            descriptionText={`Tem certeza que deseja ativar o curso "${item.name}"?`}
                            confirmationText="Ativar"
                            deleteButtonClassName="bg-green-500 hover:bg-green-600 text-gray-100"
                          />
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {!filteredCourses.length && (
                  <TableRow>
                    <TableCell
                      colSpan={3}
                      className="px-4 py-10 text-center text-slate-500">
                      {selectedInstitutionId
                        ? "Nenhum curso encontrado."
                        : "Selecione uma instituição para ver os cursos."}
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>
        </>
      )}
    </main>
  );
};