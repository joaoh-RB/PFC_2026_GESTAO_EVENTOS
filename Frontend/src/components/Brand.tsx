import { CalendarCheck2 } from "lucide-react";

interface BrandProps {
  compact?: boolean;
  inverse?: boolean;
}

export function Brand({ compact = false, inverse = false }: BrandProps) {
  return (
    <div className="flex items-center gap-3">
      <div
        className={`flex size-9 shrink-0 items-center justify-center rounded-xl ${
          inverse
            ? "bg-white/10 text-cyan-300 ring-1 ring-white/15"
            : "bg-[#17104f] text-cyan-300"
        }`}>
        <CalendarCheck2 className="size-5" strokeWidth={2.2} />
      </div>
      {!compact && (
        <div className="leading-none">
          <div
            className={`text-[9px] font-semibold uppercase tracking-[0.22em] ${
              inverse ? "text-white/55" : "text-slate-400"
            }`}>
            Gestão acadêmica
          </div>
          <div
            className={`mt-1 text-lg font-bold tracking-[-0.04em] ${
              inverse ? "text-white" : "text-[#17104f]"
            }`}>
            SYMPLOSIO
          </div>
        </div>
      )}
    </div>
  );
}
