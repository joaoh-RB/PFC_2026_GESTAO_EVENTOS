import React, { useEffect, useMemo, useState } from "react";
import { api } from "../services/api";
import type { OptionItem } from "../types/optionItem";
import { PageHeader } from "../components/PageHeader";
import {
  AlertCircle,
  Check,
  CircleOff,
  Loader2,
  Pencil,
  Plus,
  Search,
  UserCheck,
  UserX,
  X,
  User2,
} from "lucide-react";
import { DeleteConfirmation } from "@/components/ui/deleteConfirm";

type ApprovalStatus = 1 | 2 | 3;

interface ManagedUser {
  id: string;
  name: string;
  email: string;
  uniqueIdentifier: string;
  institutionId: string;
  courseId: string;
  isActive: boolean;
  approvalStatus: ApprovalStatus;
  approvedByUserName?: string | null;
  approvedDate?: string | null;
}

interface UserForm {
  name: string;
  email: string;
  uniqueIdentifier: string;
  institutionId: string;
  courseId: string;
  password: string;
}

const emptyForm: UserForm = {
  name: "",
  email: "",
  uniqueIdentifier: "",
  institutionId: "",
  courseId: "",
  password: "",
};

const statusLabel: Record<ApprovalStatus, string> = {
  1: "Pendente",
  2: "Aprovado",
  3: "Reprovado",
};

const formatDate = (value?: string | null) => {
  if (!value) return "—";
  return new Date(value).toLocaleString("pt-BR");
};

export const Users: React.FC = () => {
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [institutions, setInstitutions] = useState<OptionItem[]>([]);
  const [courses, setCourses] = useState<OptionItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<"all" | ApprovalStatus>("all");
  const [editing, setEditing] = useState<ManagedUser | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<UserForm>(emptyForm);

  const loadUsers = async () => {
    const response = await api.get<ManagedUser[]>("/users");
    setUsers(response.data);
  };
  const selectedInstitutionId = form.institutionId;
  useEffect(() => {
    Promise.all([
      api.get<ManagedUser[]>("/users"),
      api.get<OptionItem[]>("/institutions"),
    ])
      .then(([userResponse, institutionResponse]) => {
        setUsers(userResponse.data);
        setInstitutions(institutionResponse.data);
      })
      .catch((err) => {
        setError(
          err.response?.data?.message ||
            "Não foi possível carregar os usuários.",
        );
      })
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedInstitutionId) {
      (async () => {
        setCourses([]);
        setForm(prev => ({ ...prev, courseId: "" }));
        return;
      })();
    }
    const fetchCoursesByInstitution = async () => {
      try {
        const res = await api.get<OptionItem[]>("/courses", {
          params: { institutionId: selectedInstitutionId },
        });
        setCourses(res.data);
      } catch {
        setCourses([]);
        setError("Não foi possível carregar os cursos desta instituição.");
      }
    };
    fetchCoursesByInstitution();
  }, [selectedInstitutionId]);

  const filteredUsers = useMemo(
    () =>
      users.filter((user) => {
        const term = search.toLowerCase();
        const matchesSearch =
          user.name.toLowerCase().includes(term) ||
          user.email.toLowerCase().includes(term) ||
          user.uniqueIdentifier.toLowerCase().includes(term);
        return (
          matchesSearch && (status === "all" || user.approvalStatus === status)
        );
      }),
    [users, search, status],
  );

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm);
    setShowForm(true);
    setError(null);
  };

  const openEdit = (user: ManagedUser) => {
    setEditing(user);
    setForm({
      name: user.name,
      email: user.email,
      uniqueIdentifier: user.uniqueIdentifier,
      institutionId: user.institutionId,
      courseId: user.courseId,
      password: "",
    });
    setShowForm(true);
    setError(null);
  };

  const closeForm = () => {
    setShowForm(false);
    setEditing(null);
    setForm(emptyForm);
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!editing && form.password.length < 6) {
      setError("A senha deve ter pelo menos 6 caracteres.");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const payload = { ...form, password: form.password || undefined };
      if (editing) await api.put(`/users/${editing.id}`, payload);
      else await api.post("/users", payload);
      setSuccessMessage(editing ? "Usuário atualizado com sucesso! Redirecionando..." : "Usuário criado com sucesso! Redirecionando...");
      await loadUsers();
      setTimeout(() => {
        setSuccessMessage(null);
        closeForm();
      }, 1400);
    } catch (err: any) {
      setError(
        err.response?.data?.message || "Não foi possível salvar o usuário.",
      );
    } finally {
      setSaving(false);
    }
  };

  const setApproval = async (user: ManagedUser, approvalStatus: 2 | 3) => {
    setError(null);
    try {
      await api.patch(`/users/${user.id}/approval`, { status: approvalStatus });
      await loadUsers();
    } catch (err: any) {
      setError(
        err.response?.data?.message || "Não foi possível alterar a aprovação.",
      );
    }
  };

  const toggleActive = async (user: ManagedUser) => {
    setError(null);
    try {
      await api.patch(`/users/${user.id}/active`, { isActive: !user.isActive });
      await loadUsers();
      setSuccessMessage(user.isActive ? "Usuário desativado com sucesso." : "Usuário ativado com sucesso.");
      setTimeout(() => setSuccessMessage(null), 1400);
    } catch (err: any) {
      setError(
        err.response?.data?.message || "Não foi possível alterar a situação.",
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
          title="Alunos"
          description="Aprove cadastros e gerencie o acesso dos estudantes."
          action={
          <button
            onClick={openCreate}
            className="primary-action">
            <Plus className="h-4 w-4" /> Novo aluno
          </button>
          }
        />

        {error && (
          <div className="mb-4 flex items-center gap-2 rounded-lg bg-red-50 p-3 text-sm text-red-700">
            <AlertCircle className="h-5 w-5" />
            {error}
          </div>
        )}
        {successMessage && (
          <div className="mb-4 flex items-center gap-2 rounded-lg bg-emerald-50 p-3 text-sm text-emerald-700">
            <Check className="h-5 w-5" />
            {successMessage}
          </div>
        )}

        <div className="filter-bar">
          <label className="relative flex-1">
            <Search className="absolute left-3 top-2.5 h-5 w-5 text-slate-400" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Buscar por nome, e-mail ou matrícula"
              className="form-control pl-10"
            />
          </label>
          <select
            value={status}
            onChange={(e) =>
              setStatus(
                e.target.value === "all"
                  ? "all"
                  : (Number(e.target.value) as ApprovalStatus),
              )
            }
            className="form-control sm:w-56">
            <option value="all">Todas as aprovações</option>
            <option value="1">Pendentes</option>
            <option value="2">Aprovados</option>
            <option value="3">Reprovados</option>
          </select>
        </div>

        <div className="surface-card overflow-x-auto">
          <table className="data-table">
            <thead>
              <tr>
                <th className="px-4 py-3">Usuário</th>
                <th className="px-4 py-3">Matrícula</th>
                <th className="px-4 py-3">Aprovação</th>
                <th className="px-4 py-3">Aprovado por</th>
                <th className="px-4 py-3">Acesso</th>
                <th className="px-4 py-3 text-right">Ações</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((user) => (
                <tr key={user.id} className="hover:bg-slate-50">
                  <td className="px-4 py-3">
                    <div className="font-medium text-slate-900">
                      {user.name}
                    </div>
                    <div className="text-xs text-slate-500">{user.email}</div>
                  </td>
                  <td className="px-4 py-3 text-slate-600">
                    {user.uniqueIdentifier}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                        user.approvalStatus === 2
                          ? "bg-emerald-100 text-emerald-700"
                          : user.approvalStatus === 3
                            ? "bg-red-100 text-red-700"
                            : "bg-amber-100 text-amber-700"
                      }`}>
                      {statusLabel[user.approvalStatus]}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-slate-600">
                    {user.approvedByUserName ? (
                      <>
                        <div>{user.approvedByUserName}</div>
                        <div className="text-xs text-slate-400">
                          {formatDate(user.approvedDate)}
                        </div>
                      </>
                    ) : (
                      <div>—</div>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={
                        user.isActive ? "text-emerald-700" : "text-slate-500"
                      }>
                      {user.isActive ? "Ativo" : "Inativo"}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-1">
                      {user.approvalStatus !== 2 && (
                        <button
                          title="Aprovar"
                          onClick={() => setApproval(user, 2)}
                          className="rounded p-2 text-emerald-600 hover:bg-emerald-50">
                          <UserCheck className="h-4 w-4" />
                        </button>
                      )}
                      {user.approvalStatus === 1 && (
                        <button
                          title="Reprovar"
                          onClick={() => setApproval(user, 3)}
                          className="rounded p-2 text-red-600 hover:bg-red-50">
                          <UserX className="h-4 w-4" />
                        </button>
                      )}
                      {user.isActive ? (
                        <DeleteConfirmation
                          children={
                            <button
                              title="Inativar"
                              className="rounded p-2 text-amber-600 hover:bg-amber-50">
                              <CircleOff className="h-4 w-4" />
                            </button>
                          }
                          onDelete={() => toggleActive(user)}
                          onCancel={() => { setSuccessMessage(null); setError(null); }}
                          descriptionText={"Tem certeza que deseja desativar este usuário?"}
                          confirmationText={"Desativar"}
                        />
                      ) : (
                        <DeleteConfirmation
                          children={
                            <button
                              title="Ativar"
                              className="rounded p-2 text-amber-600 hover:bg-amber-50">
                              <Check className="h-4 w-4" />
                            </button>
                          }
                          onDelete={() => toggleActive(user)}
                          onCancel={() => { setSuccessMessage(null); setError(null); }}
                          descriptionText={"Tem certeza que deseja ativar este usuário?"}
                          confirmationText={"Ativar"}
                          deleteButtonClassName={"bg-green-500 hover:bg-green-600 text-gray-100"}
                        />
                      )}
                      <button
                        title="Editar"
                        onClick={() => openEdit(user)}
                        className="rounded p-2 text-indigo-600 hover:bg-indigo-50">
                        <Pencil className="h-4 w-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {!filteredUsers.length && (
                <tr>
                  <td
                    colSpan={6}
                    className="px-4 py-10 text-center text-slate-500">
                    Nenhum usuário encontrado.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      {showForm && (
        <div className="modal-backdrop">
          <div className="modal-panel max-w-xl">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-cyan-50 text-[#12a7d4]">
              <User2 className="h-6 w-6" />
            </div>
            <div className="text-center sm:text-left mb-2">
              <p className="text-sm text-slate-600">{editing ? "Preencha os dados abaixo para atualizar o usuário acadêmico" : "Preencha os dados abaixo para cadastrar um novo usuário acadêmico"}</p>
            </div>
            <div className="mb-5 flex items-center justify-between">
              <h2 className="text-xl font-bold text-slate-900">
                {editing ? "Editar usuário" : "Novo usuário"}
              </h2>
              <button
                onClick={() => { setSuccessMessage(null); setError(null); closeForm(); }}
                className="rounded p-1 text-slate-500 hover:bg-slate-100">
                <X />
              </button>
            </div>
            {successMessage && (
              <div className="mb-4 flex items-center gap-2 rounded-lg bg-emerald-50 p-3 text-sm text-emerald-700">
                <Check className="h-5 w-5" />
                {successMessage}
              </div>
            )}
            {error && (
              <div className="mb-4 flex items-center gap-2 rounded-lg bg-red-50 p-3 text-sm text-red-700">
                <AlertCircle className="h-5 w-5" />
                {error}
              </div>
            )}
            <form
              onSubmit={submit}
              className="grid grid-cols-1 gap-4 sm:grid-cols-2">
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
                E-mail
                <input
                  required
                  type="email"
                  value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                  className="form-control mt-1"
                />
              </label>
              <label className="text-sm font-medium text-slate-700">
                RA / Matrícula
                <input
                  required
                  value={form.uniqueIdentifier}
                  onChange={(e) =>
                    setForm({ ...form, uniqueIdentifier: e.target.value })
                  }
                  className="form-control mt-1"
                />
              </label>
              <label className="text-sm font-medium text-slate-700">
                Instituição
                <select
                  required
                  value={form.institutionId}
                  onChange={(e) =>
                    setForm({ ...form, institutionId: e.target.value })
                  }
                  className="form-control mt-1">
                  <option value="">Selecione</option>
                  {institutions.map((item) => (
                    <option key={item.value} value={item.value}>
                      {item.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="text-sm font-medium text-slate-700">
                Curso
                <select
                  required
                  value={form.courseId}
                  onChange={(e) =>
                    setForm({ ...form, courseId: e.target.value })
                  }
                  className="form-control mt-1">
                  <option value="">Selecione</option>
                  {courses.map((item) => (
                    <option key={item.value} value={item.value}>
                      {item.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="sm:col-span-2 text-sm font-medium text-slate-700">
                {editing ? "Nova senha (opcional)" : "Senha"}
                <input
                  required={!editing}
                  minLength={6}
                  type="password"
                  value={form.password}
                  onChange={(e) =>
                    setForm({ ...form, password: e.target.value })
                  }
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
