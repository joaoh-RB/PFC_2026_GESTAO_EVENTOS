import React, { useState } from "react";
import { FileSpreadsheet, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "cn";

interface ExportExcelButtonProps {
  onExport: () => void | Promise<void>;
  disabled?: boolean;
  label?: string;
  className?: string;
}

export const ExportExcelButton: React.FC<ExportExcelButtonProps> = ({
  onExport,
  disabled = false,
  label = "Exportar Excel",
  className,
}) => {
  const [isExporting, setIsExporting] = useState(false);

  const handleClick = async () => {
    if (isExporting || disabled) return;
    setIsExporting(true);
    try {
      await onExport();
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <Button
      type="button"
      variant="outline"
      disabled={disabled || isExporting}
      onClick={() => void handleClick()}
      className={cn(
        "secondary-action h-10 shrink-0 gap-2 cursor-pointer",
        className,
      )}>
      {isExporting ? (
        <Loader2 className="h-4 w-4 animate-spin text-emerald-600" />
      ) : (
        <FileSpreadsheet className="h-4 w-4 text-emerald-600" />
      )}
      {label}
    </Button>
  );
};

