import { useEffect, useMemo, useState, type ReactNode } from "react";
import { isAxiosError } from "axios";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  CalendarDays,
  CalendarRange,
  Check,
  ChevronDown,
  Clock3,
  GraduationCap,
  Loader2,
  Search,
  Star,
  UserCheck,
  UserX,
} from "lucide-react";
import { api } from "@/services/api";
import { useAuth } from "@/hooks/useAuth";
import type { DashboardData } from "@/types/dashboard";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const ALL = "all";
const MONTHS = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

type PeriodPreset = "30d" | "6m" | "year" | "all" | "custom";

interface AppliedFilters {
  preset: PeriodPreset;
  from: string;
  to: string;
  eventId: string;
  courseId: string;
  teacherId: string;
}

const submissionColors = ["#17104f", "#12a7d4", "#10b981", "#e11d48"];

function toInputDate(date: Date) {
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

function presetRange(preset: PeriodPreset): { from: string; to: string } {
  const today = new Date();
  const to = toInputDate(today);
  if (preset === "30d") {
    const from = new Date(today);
    from.setDate(from.getDate() - 29);
    return { from: toInputDate(from), to };
  }
  if (preset === "year") {
    return { from: `${today.getFullYear()}-01-01`, to };
  }
  if (preset === "all") {
    return { from: "2020-01-01", to };
  }
  const from = new Date(today.getFullYear(), today.getMonth() - 5, 1);
  return { from: toInputDate(from), to };
}

function initialFilters(): AppliedFilters {
  const range = presetRange("6m");
  return { preset: "6m", ...range, eventId: ALL, courseId: ALL, teacherId: ALL };
}

function toRangeStart(date: string) {
  return new Date(`${date}T00:00:00-03:00`).toISOString();
}

function toRangeEnd(date: string) {
  return new Date(`${date}T23:59:59.999-03:00`).toISOString();
}

function formatDay(date: string) {
  const [year, month, day] = date.split("-");
  return `${day}/${month}/${year}`;
}

function formatEventDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toLocaleDateString("pt-BR");
}

export function StaffDashboard() {
  const { user } = useAuth();
  const canFilterByTeacher = user?.role !== "Professor";
  const [draft, setDraft] = useState<AppliedFilters>(initialFilters);
  const [applied, setApplied] = useState<AppliedFilters>(initialFilters);
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    const load = async () => {
      setRefreshing(true);
      setError(null);
      try {
        const response = await api.get<DashboardData>("/dashboard", {
          signal: controller.signal,
          params: {
            from: toRangeStart(applied.from),
            to: toRangeEnd(applied.to),
            ...(applied.eventId !== ALL ? { eventId: applied.eventId } : {}),
            ...(applied.courseId !== ALL ? { courseId: applied.courseId } : {}),
            ...(applied.teacherId !== ALL ? { teacherId: applied.teacherId } : {}),
          },
        });
        setData(response.data);
      } catch (err) {
        if (controller.signal.aborted) return;
        const message = isAxiosError(err)
          ? err.response?.data?.message
          : null;
        setError(message || "Não foi possível carregar o painel.");
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
          setRefreshing(false);
        }
      }
    };
    void load();
    return () => controller.abort();
  }, [applied]);

  const courses = data?.courses ?? [];
  const teachers = data?.teachers ?? [];
  const visibleCourses = useMemo(() => {
    if (draft.teacherId === ALL) return courses;
    const teacher = teachers.find((item) => item.id === draft.teacherId);
    const allowed = new Set(teacher?.courseIds ?? []);
    return courses.filter((course) => allowed.has(course.id));
  }, [courses, teachers, draft.teacherId]);

  const visibleEvents = useMemo(() => {
    return (data?.eventOptions ?? []).filter((event) => {
      if (draft.courseId !== ALL && !event.courseIds.includes(draft.courseId)) return false;
      if (draft.teacherId !== ALL && !event.teacherIds.includes(draft.teacherId)) return false;
      return true;
    });
  }, [data?.eventOptions, draft.courseId, draft.teacherId]);

  const courseItems = [
    { value: ALL, label: "Todos os cursos" },
    ...visibleCourses.map((course) => ({ value: course.id, label: course.name })),
  ];
  const teacherItems = [
    { value: ALL, label: "Todos os professores" },
    ...teachers.map((teacher) => ({ value: teacher.id, label: teacher.name })),
  ];
  const eventItems = [
    { value: ALL, label: "Todos os eventos" },
    ...visibleEvents.map((event) => ({
      value: event.id,
      label: event.name,
      hint: formatEventDate(event.startDate),
    })),
  ];
  const presetItems = [
    { value: "30d", label: "Últimos 30 dias" },
    { value: "6m", label: "Últimos 6 meses" },
    { value: "year", label: "Este ano" },
    { value: "all", label: "Desde 2020" },
    { value: "custom", label: "Período personalizado" },
  ];

  const applyPreset = (preset: PeriodPreset) => {
    if (preset === "custom") {
      setDraft((current) => ({ ...current, preset }));
      return;
    }
    const range = presetRange(preset);
    setDraft((current) => ({ ...current, preset, ...range }));
  };

  const eventValue =
    draft.eventId === ALL || visibleEvents.some((event) => event.id === draft.eventId)
      ? draft.eventId
      : ALL;
  const courseValue =
    draft.courseId === ALL || visibleCourses.some((course) => course.id === draft.courseId)
      ? draft.courseId
      : ALL;

  const applyPeriod = () => {
    if (draft.from > draft.to) {
      setError("A data final deve ser posterior à data inicial.");
      return;
    }
    setApplied((current) => ({
      ...current,
      preset: draft.preset,
      from: draft.from,
      to: draft.to,
    }));
  };

  const applyDetail = () => {
    setApplied((current) => ({
      ...current,
      eventId: eventValue,
      courseId: courseValue,
      teacherId: draft.teacherId,
    }));
  };

  const clearFilters = () => {
    const next = initialFilters();
    setDraft(next);
    setApplied(next);
  };

  const submissionChart = data
    ? [
        { name: "Grupos", value: data.submissions.groups },
        { name: "Envios", value: data.submissions.submissions },
        { name: "Aprovados", value: data.submissions.approved },
        { name: "Reprovados", value: data.submissions.rejected },
      ]
    : [];
  const feedbackChart =
    data?.feedback.distribution.map((point) => ({
      name: `${point.rating}`,
      quantidade: point.count,
    })) ?? [];
  const timelineChart =
    data?.timeline.map((point) => ({
      name: `${MONTHS[point.month - 1]}/${String(point.year).slice(2)}`,
      Inscrições: point.registrations,
      Presenças: point.attendances,
      Envios: point.submissions,
    })) ?? [];
  const hasSubmissions = submissionChart.some((item) => item.value > 0);
  const hasFeedback = (data?.feedback.count ?? 0) > 0;
  const chartRegistrations = data?.timeline.reduce((sum, point) => sum + point.registrations, 0) ?? 0;
  const chartAttendances = data?.timeline.reduce((sum, point) => sum + point.attendances, 0) ?? 0;
  const attendanceRate =
    chartRegistrations > 0 ? Math.round((chartAttendances / chartRegistrations) * 100) : null;
  const institutionHint =
    user?.role === "Administrador" && !user.institutionId
      ? "De todas as instituições"
      : "Da sua instituição";

  return (
    <>
      <div className="page-heading">
        <div>
          <h1 className="page-title">Painel</h1>
          <p className="page-description">
            {data?.scopeDescription ?? "Acompanhe a operação acadêmica no período selecionado."}
          </p>
        </div>
      </div>

      <section className="flex flex-col gap-3 rounded-xl border border-[#e1e4ea] bg-white p-4 shadow-[0_1px_2px_rgba(20,18,55,0.03)]">
        <div className="grid flex-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
          <div className="space-y-2">
            <Label htmlFor="dashboard-preset">Período</Label>
            <Select
              value={draft.preset}
              items={presetItems}
              onValueChange={(value) => applyPreset((value as PeriodPreset) ?? "6m")}>
              <SelectTrigger id="dashboard-preset" className="w-full !h-10">
                <SelectValue placeholder="Período" />
              </SelectTrigger>
              <SelectContent side="bottom" align="start">
                <SelectGroup>
                  {presetItems.map((item) => (
                    <SelectItem key={item.value} value={item.value}>
                      {item.label}
                    </SelectItem>
                  ))}
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="dashboard-from">De</Label>
            <Input
              id="dashboard-from"
              type="date"
              value={draft.from}
              onChange={(event) =>
                setDraft((current) => ({ ...current, preset: "custom", from: event.target.value }))
              }
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="dashboard-to">Até</Label>
            <Input
              id="dashboard-to"
              type="date"
              value={draft.to}
              onChange={(event) =>
                setDraft((current) => ({ ...current, preset: "custom", to: event.target.value }))
              }
            />
          </div>
        </div>
        <div className="flex gap-2 sm:justify-end">
          <Button type="button" onClick={applyPeriod} disabled={refreshing}>
            {refreshing ? <Loader2 className="size-4 animate-spin" /> : null}
            Aplicar período
          </Button>
          <Button type="button" variant="outline" onClick={clearFilters} disabled={refreshing}>
            Limpar
          </Button>
        </div>
      </section>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {!data ? (
        loading ? (
          <div className="surface-card flex min-h-48 items-center justify-center gap-2 text-sm text-slate-500">
            <Loader2 className="size-4 animate-spin text-[#12a7d4]" />
            Carregando painel...
          </div>
        ) : null
      ) : (
        <>
          <section className="space-y-3">
            <div>
              <h2 className="text-sm font-semibold text-[#262242]">Alunos da instituição</h2>
              <p className="mt-1 text-xs text-slate-500">
                Aprovados, pendentes e reprovados consideram a instituição inteira, sem o período e sem o evento.
              </p>
            </div>
            <div className="grid gap-4 sm:grid-cols-3">
              <MetricCard
                label="Aprovados"
                value={data.students}
                hint={institutionHint}
                icon={GraduationCap}
                tone="bg-violet-50 text-violet-600"
              />
              <MetricCard
                label="Pendentes"
                value={data.pendingStudents}
                hint={institutionHint}
                icon={Clock3}
                tone="bg-amber-50 text-amber-600"
              />
              <MetricCard
                label="Reprovados"
                value={data.rejectedStudents ?? 0}
                hint={institutionHint}
                icon={UserX}
                tone="bg-rose-50 text-rose-600"
              />
            </div>
          </section>

          <section className="space-y-3">
            <div>
              <h2 className="text-sm font-semibold text-[#262242]">Eventos no período</h2>
              <p className="mt-1 text-xs text-slate-500">
                De {formatDay(applied.from)} até {formatDay(applied.to)}, sem filtrar um evento específico.
              </p>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <MetricCard
                label="Eventos"
                value={data.events}
                hint="No período selecionado"
                icon={CalendarDays}
                tone="bg-cyan-50 text-cyan-600"
              />
              <MetricCard
                label="Inscrições"
                value={data.registrations}
                hint="Confirmadas no período"
                icon={UserCheck}
                tone="bg-emerald-50 text-emerald-600"
              />
            </div>
          </section>

          <section className="flex flex-col gap-3 rounded-xl border border-[#e1e4ea] bg-white p-4 shadow-[0_1px_2px_rgba(20,18,55,0.03)]">
            <div>
              <h2 className="text-sm font-semibold text-[#262242]">Recorte dos gráficos</h2>
              <p className="mt-1 text-xs text-slate-500">
                Evento, curso ou professor valem apenas para os gráficos abaixo, dentro do período já aplicado.
              </p>
            </div>
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              <div className="space-y-2">
                <Label htmlFor="dashboard-event">Evento</Label>
                <FilterSearchSelect
                  id="dashboard-event"
                  value={eventValue}
                  items={eventItems}
                  placeholder="Todos os eventos"
                  searchPlaceholder="Pesquisar evento"
                  emptyMessage="Nenhum evento encontrado."
                  onValueChange={(value) =>
                    setDraft((current) => ({ ...current, eventId: value }))
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="dashboard-course">Curso</Label>
                <FilterSearchSelect
                  id="dashboard-course"
                  value={courseValue}
                  items={courseItems}
                  placeholder="Todos os cursos"
                  searchPlaceholder="Pesquisar curso"
                  emptyMessage="Nenhum curso encontrado."
                  onValueChange={(value) =>
                    setDraft((current) => ({
                      ...current,
                      courseId: value,
                      eventId: ALL,
                    }))
                  }
                />
              </div>
              {canFilterByTeacher && (
                <div className="space-y-2">
                  <Label htmlFor="dashboard-teacher">Professor</Label>
                  <FilterSearchSelect
                    id="dashboard-teacher"
                    value={draft.teacherId}
                    items={teacherItems}
                    placeholder="Todos os professores"
                    searchPlaceholder="Pesquisar professor"
                    emptyMessage="Nenhum professor encontrado."
                    onValueChange={(value) =>
                      setDraft((current) => ({
                        ...current,
                        teacherId: value,
                        courseId: ALL,
                        eventId: ALL,
                      }))
                    }
                  />
                </div>
              )}
            </div>
            <div className="flex gap-2 sm:justify-end">
              <Button type="button" onClick={applyDetail} disabled={refreshing}>
                {refreshing ? <Loader2 className="size-4 animate-spin" /> : null}
                Aplicar recorte
              </Button>
            </div>
          </section>

          <div className="grid gap-4 xl:grid-cols-2">
            <ChartCard
              title="Submissões"
              description="Grupos criados, envios e o resultado da análise no recorte.">
              {hasSubmissions ? (
                <div className="h-72">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={submissionChart} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                      <CartesianGrid stroke="#eef0f4" vertical={false} />
                      <XAxis dataKey="name" tick={{ fill: "#64748b", fontSize: 12 }} axisLine={false} tickLine={false} />
                      <YAxis allowDecimals={false} tick={{ fill: "#94a3b8", fontSize: 12 }} axisLine={false} tickLine={false} />
                      <Tooltip cursor={{ fill: "#f8fafc" }} />
                      <Bar dataKey="value" name="Quantidade" radius={[8, 8, 0, 0]} maxBarSize={48}>
                        {submissionChart.map((entry, index) => (
                          <Cell key={entry.name} fill={submissionColors[index]} />
                        ))}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              ) : (
                <EmptyChart message="Nenhum grupo ou envio neste recorte." />
              )}
              <p className="mt-2 text-xs text-slate-500">
                {data.submissions.pending} envio{data.submissions.pending === 1 ? "" : "s"} ainda aguarda{data.submissions.pending === 1 ? "" : "m"} análise.
              </p>
            </ChartCard>

            <ChartCard
              title="Feedback"
              description="Distribuição das notas dadas pelos alunos depois dos eventos.">
              <div className="mb-4 flex items-end justify-between gap-4">
                <div>
                  <p className="text-xs font-medium text-slate-400">Média</p>
                  <p className="mt-1 flex items-center gap-2 text-3xl font-semibold tracking-tight text-[#191638]">
                    <Star className="size-5 fill-amber-400 text-amber-400" />
                    {data.feedback.averageRating ?? "—"}
                  </p>
                </div>
                <p className="text-sm text-slate-500">
                  {data.feedback.count} avaliaç{data.feedback.count === 1 ? "ão" : "ões"}
                </p>
              </div>
              {hasFeedback ? (
                <div className="h-56">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={feedbackChart} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                      <CartesianGrid stroke="#eef0f4" vertical={false} />
                      <XAxis dataKey="name" tick={{ fill: "#64748b", fontSize: 12 }} axisLine={false} tickLine={false} />
                      <YAxis allowDecimals={false} tick={{ fill: "#94a3b8", fontSize: 12 }} axisLine={false} tickLine={false} />
                      <Tooltip cursor={{ fill: "#f8fafc" }} />
                      <Bar dataKey="quantidade" name="Avaliações" fill="#12a7d4" radius={[8, 8, 0, 0]} maxBarSize={42} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              ) : (
                <EmptyChart message="Nenhum feedback neste recorte." />
              )}
            </ChartCard>
          </div>

          <ChartCard
            title="Movimento no período"
            description="Inscrições, presenças e envios de trabalho mês a mês. A presença mostra o aproveitamento dos eventos.">
            <div className="h-80">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={timelineChart} margin={{ top: 8, right: 12, left: -16, bottom: 0 }}>
                  <CartesianGrid stroke="#eef0f4" vertical={false} />
                  <XAxis dataKey="name" minTickGap={28} tick={{ fill: "#64748b", fontSize: 12 }} axisLine={false} tickLine={false} />
                  <YAxis allowDecimals={false} tick={{ fill: "#94a3b8", fontSize: 12 }} axisLine={false} tickLine={false} />
                  <Tooltip />
                  <Legend />
                  <Line type="monotone" dataKey="Inscrições" stroke="#17104f" strokeWidth={2.5} dot={false} />
                  <Line type="monotone" dataKey="Presenças" stroke="#10b981" strokeWidth={2.5} dot={false} />
                  <Line type="monotone" dataKey="Envios" stroke="#12a7d4" strokeWidth={2.5} dot={false} />
                </LineChart>
              </ResponsiveContainer>
            </div>
            <p className="mt-2 flex items-center gap-2 text-xs text-slate-500">
              <CalendarRange className="size-3.5 text-[#12a7d4]" />
              {attendanceRate === null
                ? "Ainda não há inscrições confirmadas neste recorte para calcular a presença."
                : `${attendanceRate}% das inscrições deste recorte já têm presença registrada (${chartAttendances} de ${chartRegistrations}).`}
            </p>
          </ChartCard>
        </>
      )}
    </>
  );
}

function FilterSearchSelect({
  id,
  value,
  items,
  placeholder,
  searchPlaceholder,
  emptyMessage,
  onValueChange,
}: {
  id: string;
  value: string;
  items: { value: string; label: string; hint?: string }[];
  placeholder: string;
  searchPlaceholder: string;
  emptyMessage: string;
  onValueChange: (value: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const selected = items.find((item) => item.value === value);
  const normalizedQuery = query.trim().toLowerCase();
  const visibleItems = items.filter((item) => {
    if (!normalizedQuery) return true;
    return `${item.label} ${item.hint ?? ""}`.toLowerCase().includes(normalizedQuery);
  });

  return (
    <Popover
      open={open}
      onOpenChange={(nextOpen) => {
        setOpen(nextOpen);
        if (!nextOpen) setQuery("");
      }}>
      <PopoverTrigger
        id={id}
        className="flex h-10 w-full items-center justify-between gap-2 rounded-lg border border-[#dfe3ea] bg-white px-3 text-left text-sm text-[#292640] outline-none transition focus-visible:border-[#19a7d1] focus-visible:ring-2 focus-visible:ring-[#19a7d1]/10">
        <span className="min-w-0 flex-1 truncate">
          {selected?.label ?? placeholder}
        </span>
        <ChevronDown className="size-4 shrink-0 text-slate-400" />
      </PopoverTrigger>
      <PopoverContent
        align="start"
        side="bottom"
        sideOffset={6}
        className="w-(--anchor-width) min-w-64 gap-2 p-2">
        <div className="relative">
          <Search className="pointer-events-none absolute top-1/2 left-2.5 size-3.5 -translate-y-1/2 text-slate-400" />
          <Input
            autoFocus
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder={searchPlaceholder}
            className="h-9 pl-8"
            aria-label={searchPlaceholder}
          />
        </div>
        <div className="max-h-64 overflow-y-auto">
          {visibleItems.length === 0 ? (
            <p className="px-2 py-3 text-center text-sm text-slate-500">{emptyMessage}</p>
          ) : (
            visibleItems.map((item) => {
              const isSelected = item.value === value;
              return (
                <button
                  key={item.value}
                  type="button"
                  onClick={() => {
                    onValueChange(item.value);
                    setOpen(false);
                    setQuery("");
                  }}
                  className={`flex w-full items-start gap-2 rounded-md px-2 py-2 text-left text-sm transition hover:bg-slate-50 ${
                    isSelected ? "bg-cyan-50 text-[#0d91bc]" : "text-[#292640]"
                  }`}>
                  <span className="min-w-0 flex-1">
                    <span className="block leading-5 whitespace-normal break-words">
                      {item.label}
                    </span>
                    {item.hint && (
                      <span className="mt-0.5 block text-xs text-slate-400">{item.hint}</span>
                    )}
                  </span>
                  {isSelected && <Check className="mt-0.5 size-4 shrink-0" />}
                </button>
              );
            })
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}

function MetricCard({
  label,
  value,
  hint,
  icon: Icon,
  tone,
}: {
  label: string;
  value: number | null | undefined;
  hint: string;
  icon: typeof GraduationCap;
  tone: string;
}) {
  const total = typeof value === "number" && Number.isFinite(value) ? value : 0;

  return (
    <section className="surface-card p-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.08em] text-slate-400">{label}</p>
          <p className="mt-3 text-3xl font-semibold tracking-tight text-[#191638]">{total}</p>
          <p className="mt-2 text-xs text-slate-500">{hint}</p>
        </div>
        <div className={`flex size-10 items-center justify-center rounded-xl ${tone}`}>
          <Icon className="size-[18px]" />
        </div>
      </div>
    </section>
  );
}

function ChartCard({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <section className="surface-card p-5">
      <h2 className="text-sm font-semibold text-[#262242]">{title}</h2>
      <p className="mt-1 text-xs leading-5 text-slate-500">{description}</p>
      <div className="mt-4">{children}</div>
    </section>
  );
}

function EmptyChart({ message }: { message: string }) {
  return (
    <div className="flex h-56 items-center justify-center rounded-lg border border-dashed border-slate-200 bg-slate-50 px-6 text-center text-sm text-slate-500">
      {message}
    </div>
  );
}
