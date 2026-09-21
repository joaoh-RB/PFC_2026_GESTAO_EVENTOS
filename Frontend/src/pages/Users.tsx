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
import { useAuth } from "../hooks/useAuth";
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";

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
}

const emptyForm: UserForm = {
  name: "",
  email: "",
  uniqueIdentifier: "",
  institutionId: "",
  courseId: "",
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
  const { user } = useAuth();
  const userInstitutionId = user?.institutionId ?? "";
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
  const getInitialForm = (institutionId = ""): UserForm => ({
    ...emptyForm,
    institutionId,
  });
  const [form, setForm] = useState<UserForm>(() =>
    getInitialForm(userInstitutionId),
  );

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
        setForm((prev) => ({ ...prev, courseId: "" }));
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
      users.filter((managedUser) => {
        const term = search.toLowerCase();
        const matchesSearch =
          managedUser.name.toLowerCase().includes(term) ||
          managedUser.email.toLowerCase().includes(term) ||
          managedUser.uniqueIdentifier.toLowerCase().includes(term);
        const matchesStatus =
          status === "all" || managedUser.approvalStatus === status;
        const matchesInstitution =
          user?.role === "Administrador" ||
          (!!userInstitutionId &&
            managedUser.institutionId === userInstitutionId);

        return matchesSearch && matchesStatus && matchesInstitution;
      }),
    [users, search, status, user?.role, userInstitutionId],
  );

  const openCreate = () => {
    setEditing(null);
    setForm(getInitialForm(userInstitutionId));
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
    });
    setShowForm(true);
    setError(null);
  };

  const closeForm = () => {
    setShowForm(false);
    setEditing(null);
    setForm(getInitialForm(userInstitutionId));
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const payload = { ...form };
      if (editing) await api.put(`/users/${editing.id}`, payload);
      else await api.post("/users", payload);
      setSuccessMessage(
        editing
          ? "Usuário atualizado com sucesso! Redirecionando..."
          : "Usuário criado com sucesso! Redirecionando...",
      );
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
      setSuccessMessage(
        user.isActive
          ? "Usuário desativado com sucesso."
          : "Usuário ativado com sucesso.",
      );
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
          <button onClick={openCreate} className="primary-action">
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
        <InputGroup>
          <InputGroupInput
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar por nome, e-mail ou matrícula"></InputGroupInput>
          <InputGroupAddon>
            <Search></Search>
          </InputGroupAddon>
        </InputGroup>
        <Select
          items={[
            { value: "all", label: "Todas as aprovações" },
            { value: "1", label: "Pendentes" },
            { value: "2", label: "Aprovados" },
            { value: "3", label: "Reprovados" },
          ]}
          value={status == "all" ? "all" : String(status)}
          onValueChange={(e) =>
            setStatus(e === "all" ? "all" : (Number(e) as ApprovalStatus))
          }>
          <SelectTrigger className={"w-full"}>
            <SelectValue placeholder={"Todas as Aprovações"}></SelectValue>
          </SelectTrigger>
          <SelectContent alignItemWithTrigger={true}>
            <SelectGroup>
              <SelectItem key={"all"} value={"all"}>
                Todas as aprovações
              </SelectItem>
              <SelectItem key={"1"} value={"1"}>
                Pendentes
              </SelectItem>
              <SelectItem key={"2"} value={"2"}>
                Aprovados
              </SelectItem>
              <SelectItem key={"3"} value={"3"}>
                Reprovados
              </SelectItem>
            </SelectGroup>
          </SelectContent>
        </Select>
      </div>

      <div className="surface-card overflow-x-auto">
        <Table className="table-fixed">
          <TableHeader>
            <TableRow>
              <TableHead className="w-[32%] text-left">Usuário</TableHead>
              <TableHead className="w-[12%] text-center">Matrícula</TableHead>
              <TableHead className="w-[12%] text-center">Aprovação</TableHead>
              <TableHead className="w-[18%] text-left">Aprovado por</TableHead>
              <TableHead className="w-[10%] text-center">Acesso</TableHead>
              <TableHead className="w-[16%] min-w-[220px] text-center">Ações</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredUsers.map((user) => (
              <TableRow key={user.id} className="hover:bg-slate-50 min-h-14">
                <TableCell className="w-[32%] px-4 py-4 align-middle">
                  <div className="space-y-0.5 leading-tight">
                    <div className="font-medium text-slate-900">{user.name}</div>
                    <div className="text-xs text-slate-500">{user.email}</div>
                  </div>
                </TableCell>
                <TableCell className="w-[12%] px-4 py-4 text-center text-slate-600 align-middle">
                  {user.uniqueIdentifier}
                </TableCell>
                <TableCell className="w-[12%] px-4 py-4 text-center align-middle">
                  <span
                    className={`inline-flex min-h-6 items-center rounded-full px-2.5 py-1 text-xs font-semibold ${
                      user.approvalStatus === 2
                        ? "bg-emerald-100 text-emerald-700"
                        : user.approvalStatus === 3
                          ? "bg-red-100 text-red-700"
                          : "bg-amber-100 text-amber-700"
                    }`}>
                    {statusLabel[user.approvalStatus]}
                  </span>
                </TableCell>
                <TableCell className="w-[18%] px-4 py-4 text-slate-600 align-middle">
                  {user.approvedByUserName ? (
                    <div className="space-y-0.5 leading-tight">
                      <div>{user.approvedByUserName}</div>
                      <div className="text-xs text-slate-400">
                        {formatDate(user.approvedDate)}
                      </div>
                    </div>
                  ) : (
                    <div>—</div>
                  )}
                </TableCell>
                <TableCell className="w-[10%] px-4 py-4 text-center align-middle">
                  <span
                    className={`inline-flex min-h-6 items-center rounded-full px-2.5 py-1 text-xs font-medium ${
                      user.isActive
                        ? "bg-emerald-50 text-emerald-700"
                        : "bg-slate-100 text-slate-600"
                    }`}>
                    {user.isActive ? "Ativo" : "Inativo"}
                  </span>
                </TableCell>
                <TableCell className="w-[16%] min-w-[220px] px-4 py-4 align-middle">
                  <div className="grid w-full grid-cols-4 items-center justify-items-center gap-3">
                    {user.approvalStatus !== 2 ? (
                      <button
                        title="Aprovar"
                        onClick={() => setApproval(user, 2)}
                        className="rounded p-2 text-emerald-600 hover:bg-emerald-50">
                        <UserCheck className="h-4 w-4" />
                      </button>
                    ) : (
                      <span aria-hidden="true" className="h-8 w-8" />
                    )}
                    {user.approvalStatus === 1 ? (
                      <button
                        title="Reprovar"
                        onClick={() => setApproval(user, 3)}
                        className="rounded p-2 text-red-600 hover:bg-red-50">
                        <UserX className="h-4 w-4" />
                      </button>
                    ) : (
                      <span aria-hidden="true" className="h-8 w-8" />
                    )}
                    <button
                      title="Editar"
                      onClick={() => openEdit(user)}
                      className="rounded p-2 text-indigo-600 hover:bg-indigo-50">
                      <Pencil className="h-4 w-4" />
                    </button>
                    {user.isActive ? (
                      <DeleteConfirmation
                        children={
                          <button
                            title="Inativar"
                            className="rounded p-2 text-red-600 hover:bg-red-50">
                            <CircleOff className="h-4 w-4" />
                          </button>
                        }
                        onDelete={() => toggleActive(user)}
                        onCancel={() => {
                          setSuccessMessage(null);
                          setError(null);
                        }}
                        descriptionText={
                          "Tem certeza que deseja desativar este usuário?"
                        }
                        confirmationText={"Desativar"}
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
                        onDelete={() => toggleActive(user)}
                        onCancel={() => {
                          setSuccessMessage(null);
                          setError(null);
                        }}
                        descriptionText={
                          "Tem certeza que deseja ativar este usuário?"
                        }
                        confirmationText={"Ativar"}
                        deleteButtonClassName={
                          "bg-green-500 hover:bg-green-600 text-gray-100"
                        }
                      />
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {!filteredUsers.length && (
              <TableRow>
                <TableCell
                  colSpan={6}
                  className="px-4 py-10 text-center text-slate-500">
                  Nenhum usuário encontrado.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      {showForm && (
        <div className="modal-backdrop">
          <div className="modal-panel max-w-xl">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-cyan-50 text-[#12a7d4]">
              <User2 className="h-6 w-6" />
            </div>
            <div className="text-center sm:text-left mb-2">
              <p className="text-sm text-slate-600">
                {editing
                  ? "Preencha os dados abaixo para atualizar o usuário acadêmico"
                  : "Preencha os dados abaixo para cadastrar um novo usuário acadêmico"}
              </p>
            </div>
            <div className="mb-5 flex items-center justify-between">
              <h2 className="text-xl font-bold text-slate-900">
                {editing ? "Editar usuário" : "Novo usuário"}
              </h2>
              <button
                onClick={() => {
                  setSuccessMessage(null);
                  setError(null);
                  closeForm();
                }}
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
              <div className="sm:col-span-2 text-sm font-medium text-slate-700">
                <Label className={"mb-1"} required>Nome</Label>
                <Input
                  required
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                />
              </div>
              <div className="text-sm font-medium text-slate-700">
                <Label className={"mb-1"} required>E-mail</Label>
                <Input
                  required
                  type="email"
                  value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                />
              </div>
              <div className="text-sm font-medium text-slate-700">
                <Label className={"mb-1"} required>RA / Matrícula</Label>
                <Input
                  required
                  value={form.uniqueIdentifier}
                  onChange={(e) =>
                    setForm({ ...form, uniqueIdentifier: e.target.value })
                  }
                />
              </div>
              {!userInstitutionId && (
                <div className="text-sm font-medium text-slate-700">
                  <Label className={"mb-1"} required> Instituição</Label>
                  <Select
                    required
                    value={form.institutionId}
                    onValueChange={(e) =>
                      setForm({ ...form, institutionId: e ?? "" })
                    }
                    items={institutions}>
                    <SelectTrigger className={"w-full !h-10"}>
                      <SelectValue placeholder="Selecione"></SelectValue>
                    </SelectTrigger>
                    <SelectContent alignItemWithTrigger={true}>
                      <SelectGroup>
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
              <div className="text-sm font-medium text-slate-700">
                <Label className={"mb-1"} required>Curso</Label>
                <Select
                  required
                  value={form.courseId}
                  onValueChange={(e) => setForm({ ...form, courseId: e ?? "" })}
                  items={courses}>
                  <SelectTrigger className={"w-full !h-10"}>
                    <SelectValue placeholder="Selecione"></SelectValue>
                  </SelectTrigger>
                  <SelectContent alignItemWithTrigger={true}>
                    <SelectGroup>
                      {form.institutionId === "" && (
                        <SelectItem value="" disabled>
                          Selecione uma instituição primeiro
                        </SelectItem>
                      )}
                      {courses.map((item) => (
                        <SelectItem key={item.value} value={item.value}>
                          {item.label}
                        </SelectItem>
                      ))}
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </div>
              <div className="sm:col-span-2 flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={closeForm}
                  className="secondary-action">
                  Cancelar
                </button>
                <button disabled={saving} className="primary-action">
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
