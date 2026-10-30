import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Combobox,
  ComboboxChip,
  ComboboxChips,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxItem,
  ComboboxList,
  ComboboxValue,
  useComboboxAnchor,
} from "@/components/ui/combobox";
import { AlertCircle, Loader2, LogOut, UserPlus, Users, X} from "lucide-react";
import api from "@/services/api";
import { useAuth } from "@/hooks/useAuth";
import type { OptionItem } from "@/types/optionItem";
import { GroupMemberStatus, type GroupItem } from "@/types/group";

const getErrorMessage = (err: unknown, fallback: string): string => {
  const errorObj = err as {
    response?: { data?: { message?: string } };
    message?: string;
  };
  return errorObj?.response?.data?.message || errorObj?.message || fallback;
};

const statusLabel: Record<number, string> = {
  [GroupMemberStatus.Invited]: "Convidado",
  [GroupMemberStatus.Accepted]: "Aceito",
  [GroupMemberStatus.Declined]: "Recusou",
};

const statusStyle: Record<number, string> = {
  [GroupMemberStatus.Invited]: "bg-amber-50 text-amber-700",
  [GroupMemberStatus.Accepted]: "bg-emerald-50 text-emerald-700",
  [GroupMemberStatus.Declined]: "bg-slate-100 text-slate-500",
};

interface StudentPickerProps {
  students: OptionItem[];
  value: string[];
  onChange: (ids: string[]) => void;
}

const StudentPicker: React.FC<StudentPickerProps> = ({
  students,
  value,
  onChange,
}) => {
  const anchor = useComboboxAnchor();

  return (
    <Combobox
      items={students}
      multiple
      value={value}
      onValueChange={(ids) => onChange(ids)}>
      <ComboboxChips className="w-full" ref={anchor}>
        <ComboboxValue>
          {value.map((id) => (
            <ComboboxChip key={id}>
              {students.find((s) => s.value === id)?.label ?? id}
            </ComboboxChip>
          ))}
        </ComboboxValue>
      </ComboboxChips>
      <ComboboxContent anchor={anchor}>
        <ComboboxEmpty>Nenhum aluno disponível</ComboboxEmpty>
        <ComboboxList>
          {students.map((student) => (
            <ComboboxItem key={student.value} value={student.value}>
              {student.label}
            </ComboboxItem>
          ))}
        </ComboboxList>
      </ComboboxContent>
    </Combobox>
  );
};

interface EventGroupPanelProps {
  eventId: string;
  onGroupChange: (hasGroup: boolean) => void;
}

export const EventGroupPanel: React.FC<EventGroupPanelProps> = ({
  eventId,
  onGroupChange,
}) => {
  const { user } = useAuth();
  const [group, setGroup] = useState<GroupItem | null>(null);
  const [invites, setInvites] = useState<GroupItem[]>([]);
  const [students, setStudents] = useState<OptionItem[]>([]);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isWorking, setIsWorking] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      let myGroup: GroupItem | null = null;
      try {
        const res = await api.get<GroupItem>(`/groups/event/${eventId}/mine`);
        myGroup = res.data;
      } catch (err: unknown) {
        const status = (err as { response?: { status?: number } }).response
          ?.status;
        if (status !== 404) throw err;
      }
      setGroup(myGroup);
      onGroupChange(!!myGroup);

      if (myGroup) {
        setInvites([]);
      } else {
        const invitesRes = await api.get<GroupItem[]>("/groups/invites");
        setInvites(invitesRes.data.filter((g) => g.eventId === eventId));
      }

      if (!myGroup || myGroup.leaderId === user?.id) {
        const studentsRes = await api.get<OptionItem[]>(
          `/groups/event/${eventId}/students`,
        );
        setStudents(studentsRes.data);
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Não foi possível carregar o grupo."));
    } finally {
      setIsLoading(false);
    }
  }, [eventId, user?.id, onGroupChange]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const runAction = async (
    action: () => Promise<unknown>,
    fallback: string,
  ) => {
    setIsWorking(true);
    setError(null);
    try {
      await action();
      setSelectedIds([]);
      await loadData();
    } catch (err: unknown) {
      setError(getErrorMessage(err, fallback));
    } finally {
      setIsWorking(false);
    }
  };

  const isLeader = !!group && group.leaderId === user?.id;
  const invitable = students.filter(
    (s) =>
      !group?.members.some(
        (m) =>
          m.studentId === s.value && m.status !== GroupMemberStatus.Declined,
      ),
  );

  return (
    <div className="space-y-2.5 w-full min-w-0">
      <Label className="text-xs font-semibold text-slate-700 uppercase tracking-wider">
        Grupo
      </Label>

      {error && (
        <div className="flex items-center gap-2 p-2.5 text-xs text-red-600 bg-red-50 rounded-lg border border-red-200">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span className="flex-1 break-words">{error}</span>
        </div>
      )}

      {isLoading ? (
        <div className="flex items-center justify-center p-4 text-slate-400 gap-2 border rounded-xl bg-slate-50/50">
          <Loader2 className="h-4 w-4 animate-spin text-[#109bc6]" />
          <span className="text-xs">Carregando grupo...</span>
        </div>
      ) : group ? (
        <div className="border rounded-xl bg-slate-50/50 p-3 space-y-3">
          <div className="flex items-center justify-between">
            <span className="flex items-center gap-2 text-sm font-semibold text-slate-800">
              <Users className="h-4 w-4 text-[#109bc6]" />
              Grupo {group.number}
            </span>
            {isLeader && (
              <span className="text-[11px] text-slate-500">
                Você é o líder
              </span>
            )}
          </div>

          <ul className="space-y-1.5">
            {group.members.map((member) => (
              <li
                key={member.studentId}
                className="flex items-center justify-between gap-2 text-sm text-slate-700">
                <span className="truncate">
                  {member.name}
                  {member.isLeader ? " (líder)" : ""}
                </span>
                <span className="flex items-center gap-1 shrink-0">
                  <span
                    className={`text-[11px] px-2 py-0.5 rounded-full ${statusStyle[member.status]}`}>
                    {statusLabel[member.status]}
                  </span>
                  {isLeader && !member.isLeader && (
                    <button
                      type="button"
                      title="Remover do grupo"
                      disabled={isWorking}
                      className="h-6 w-6 flex items-center justify-center rounded text-slate-400 hover:text-red-600 hover:bg-red-50 cursor-pointer"
                      onClick={() =>
                        runAction(
                          () =>
                            api.delete(
                              `/groups/${group.id}/members/${member.studentId}`,
                            ),
                          "Erro ao remover o membro.",
                        )
                      }>
                      <X className="h-4 w-4" />
                    </button>
                  )}
                </span>
              </li>
            ))}
          </ul>

          {isLeader && (
            <div className="space-y-2 border-t border-slate-100 pt-3">
              <Label className="text-xs text-slate-600">Convidar alunos</Label>
              <StudentPicker
                students={invitable}
                value={selectedIds}
                onChange={setSelectedIds}
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="gap-2 cursor-pointer"
                disabled={selectedIds.length === 0 || isWorking}
                onClick={() =>
                  runAction(
                    () =>
                      api.post(`/groups/${group.id}/members`, {
                        studentIds: selectedIds,
                      }),
                    "Erro ao convidar os alunos.",
                  )
                }>
                <UserPlus className="h-4 w-4 text-[#109bc6]" />
                Convidar
              </Button>
            </div>
          )}

          {(!isLeader ||
            !group.members.some(
              (m) =>
                m.studentId !== group.leaderId &&
                m.status === GroupMemberStatus.Accepted,
            )) && (
            <div className="border-t border-slate-100 pt-3">
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="gap-2 cursor-pointer text-red-600 hover:text-red-700"
                disabled={isWorking}
                onClick={() =>
                  runAction(
                    () => api.post(`/groups/${group.id}/leave`),
                    "Erro ao sair do grupo.",
                  )
                }>
                <LogOut className="h-4 w-4" />
                Sair do grupo
              </Button>
            </div>
          )}
        </div>
      ) : (
        <div className="space-y-3">
          {invites.map((invite) => (
            <div
              key={invite.id}
              className="flex items-center justify-between gap-3 border rounded-xl bg-amber-50/50 p-3">
              <div className="min-w-0 text-sm text-slate-700">
                <p className="font-medium">
                  Convite para o Grupo {invite.number}
                </p>
                <p className="text-xs text-slate-500 truncate">
                  Líder: {invite.leaderName}
                </p>
              </div>
              <div className="flex gap-2 shrink-0">
                <Button
                  type="button"
                  size="sm"
                  className="primary-action cursor-pointer"
                  disabled={isWorking}
                  onClick={() =>
                    runAction(
                      () => api.post(`/groups/${invite.id}/accept`),
                      "Erro ao aceitar o convite.",
                    )
                  }>
                  Aceitar
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  className="cursor-pointer"
                  disabled={isWorking}
                  onClick={() =>
                    runAction(
                      () => api.post(`/groups/${invite.id}/decline`),
                      "Erro ao recusar o convite.",
                    )
                  }>
                  Recusar
                </Button>
              </div>
            </div>
          ))}

          <div className="border border-dashed rounded-xl bg-slate-50/40 p-3 space-y-2">
            <p className="text-xs text-slate-500">
              Você ainda não está em um grupo neste evento. Crie um e convide
              colegas inscritos.
            </p>
            <StudentPicker
              students={students}
              value={selectedIds}
              onChange={setSelectedIds}
            />
            <Button
              type="button"
              size="sm"
              className="gap-2 primary-action cursor-pointer"
              disabled={isWorking}
              onClick={() =>
                runAction(
                  () =>
                    api.post("/groups", { eventId, studentIds: selectedIds }),
                  "Erro ao criar o grupo.",
                )
              }>
              <Users className="h-4 w-4" />
              Criar grupo
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};