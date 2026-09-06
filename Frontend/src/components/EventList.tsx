import React, { useState } from "react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import {
  Calendar,
  Clock,
  MapPin,
  Users,
  Loader2,
} from "lucide-react";
import {  type PagedResult } from "@/types/utils";
import {formatDateForShort} from "@/utils/format";
import type { OptionItem } from "@/types/optionItem";
import { DeleteConfirmation } from "./ui/deleteConfirm";

export interface EventItem {
  id: string;
  name: string;
  description: string;
  institutionId: string;
  institutionName: string;
  startDate: string;
  endDate: string;
  capacity: number;
  confirmedRegistrations: number;
  eventType: number;
  allowDocuments: boolean;
  allowedCourseNames: string[];
  allowedCourseIds: string[];
}

export interface EventFilterState {
  institutionId?: string;
  fromDate?: string;
}

interface EventsListProps {
  data: PagedResult<EventItem>;
  institutions: OptionItem[];
  isLoading: boolean;
  currentFilters?: EventFilterState;
  onFilter: (filters: EventFilterState) => void;
  onPageChange: (page: number) => void;
  handleStartEdition: (eventData: EventItem) => void;
  handleDelete: (eventId: string) => void;
}

export const EventsList: React.FC<EventsListProps> = ({
  data,
  institutions,
  isLoading,
  currentFilters,
  onFilter,
  onPageChange,
  handleStartEdition,
  handleDelete,
}) => {
  const allInstitutionsName = "Todas as instituições";
  const [fromDate, setFromDate] = useState<string>(
    currentFilters?.fromDate || new Date().toISOString().split("T")[0],
  );
  const [institutionId, setInstitutionId] = useState<string>(
    currentFilters?.institutionId || allInstitutionsName,
  );

  const handleApplyFilter = () => {
    onFilter({
      fromDate: fromDate || undefined,
      institutionId:
        institutionId !== allInstitutionsName ? institutionId : undefined,
    });
  };

  return (
    <div className="space-y-6">
      <Card className="bg-white">
        <CardContent className="pt-6">
          <div className="grid grid-cols-1 sm:grid-cols-6 gap-2 items-end">
            <div className="space-y-2 col-span-2">
              <Label htmlFor="filter-from-date">A partir de</Label>
              <Input
                id="filter-from-date"
                type="date"
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
              />
            </div>

            <div className={"col-span-2"}>
              <Label htmlFor="filter-institution" className={"mb-2 "}>
                Instituição
              </Label>
              <Select
                id="filter-institution"
                value={institutionId}
                onValueChange={setInstitutionId}>
                <SelectTrigger className={"w-full"}>
                  <SelectValue placeholder={allInstitutionsName} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={allInstitutionsName}>
                    Todas as instituições
                  </SelectItem>
                  {institutions.map((inst) => (
                    <SelectItem key={inst.value} value={inst.value}>
                      {inst.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="flex gap-2">
              <Button
                onClick={handleApplyFilter}
                disabled={isLoading}
                className="w-full bg-indigo-600 hover:bg-indigo-700">
                Filtrar
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {isLoading ? (
        <div className="flex justify-center items-center py-16">
          <Loader2 className="h-8 w-8 animate-spin text-indigo-600" />
        </div>
      ) : data.items.length === 0 ? (
        <Card className="text-center py-12">
          <CardContent>
            <p className="text-slate-500">
              Nenhum evento encontrado para os filtros selecionados.
            </p>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {data.items.map((event) => {
              const availableSeats =
                event.capacity - event.confirmedRegistrations;
              const isFull = availableSeats <= 0;

              return (
                <Card
                  key={event.id}
                  className="flex flex-col shadow-sm hover:shadow-md transition-shadow">
                  <CardHeader>
                    <div className="flex justify-between items-start gap-2 mb-2">
                      <Badge variant={isFull ? "destructive" : "secondary"}>
                        {isFull
                          ? "Esgotado"
                          : `${availableSeats} vagas restantes`}
                      </Badge>
                      {event.allowDocuments && (
                        <Badge
                          variant="outline"
                          className="text-xs bg-green-100 text-green-800">
                          Permite envio de documentos
                        </Badge>
                      )}
                    </div>
                    <CardTitle className="text-xl line-clamp-1">
                      {event.name}
                    </CardTitle>
                    <CardDescription className="line-clamp-2">
                      {event.description}
                    </CardDescription>
                  </CardHeader>

                  <CardContent className="space-y-3 text-sm text-slate-600">
                    <div className="flex items-center gap-2">
                      <MapPin className="h-4 w-4 text-indigo-600 shrink-0" />
                      <span className="truncate">{event.institutionName}</span>
                    </div>

                    <div className="flex items-center gap-2">
                      <Calendar className="h-4 w-4 text-indigo-600 shrink-0" />
                      <span>Início: {formatDateForShort(event.startDate)}</span>
                    </div>

                    <div className="flex items-center gap-2">
                      <Clock className="h-4 w-4 text-indigo-600 shrink-0" />
                      <span>Término: {formatDateForShort(event.endDate)}</span>
                    </div>

                    <div className="flex items-center gap-2">
                      <Users className="h-4 w-4 text-indigo-600 shrink-0" />
                      <span>
                        Inscritos: {event.confirmedRegistrations} /{" "}
                        {event.capacity}
                      </span>
                    </div>

                    {event.allowedCourseNames.length > 0 && (
                      <div>
                        <div>
                          Cursos permitidos:
                        </div>
                        <div className="pt-2 flex flex-wrap gap-1">
                          {event.allowedCourseNames
                            .slice(0, 3)
                            .map((course, idx) => (
                              <span
                                key={idx}
                                className="bg-slate-100 text-slate-700 px-2 py-0.5 rounded text-xs">
                                {course}
                              </span>
                            ))}
                          {event.allowedCourseNames.length > 3 && (
                            <span className="text-xs text-slate-400 self-center">
                              +{event.allowedCourseNames.length - 3}
                            </span>
                          )}
                        </div>
                      </div>
                    )}
                    {event.startDate > new Date().toISOString() && (
                      <div className="flex justify-between items-center gap-2 pt-4">
                        <Button
                          className={
                            "bg-white text-black hover:bg-gray-100 cursor-pointer"
                          }
                          onClick={() => handleStartEdition(event)}>
                          Editar
                        </Button>

                        <DeleteConfirmation
                          children={
                            <Button
                              className={
                                "bg-red-500 hover:bg-red-600 cursor-pointer"
                              }>
                              Excluir
                            </Button>
                          }
                          onDelete={() => handleDelete(event.id)}
                          descriptionText={
                            "Tem certeza que deseja excluir este evento?"
                          }
                          confirmationText={"Excluir"}></DeleteConfirmation>
                      </div>
                    )}
                  </CardContent>
                </Card>
              );
            })}
          </div>

          {data.totalPages > 1 && (
            <div className="flex justify-center pt-4">
              <Pagination>
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious
                      onClick={() => onPageChange(data.pageNumber - 1)}
                      className={
                        !data.hasPreviousPage
                          ? "pointer-events-none opacity-50"
                          : "cursor-pointer"
                      }
                    />
                  </PaginationItem>

                  {Array.from({ length: data.totalPages }, (_, index) => {
                    const pageNum = index + 1;
                    return (
                      <PaginationItem key={pageNum}>
                        <PaginationLink
                          isActive={pageNum === data.pageNumber}
                          onClick={() => onPageChange(pageNum)}
                          className="cursor-pointer">
                          {pageNum}
                        </PaginationLink>
                      </PaginationItem>
                    );
                  })}

                  <PaginationItem>
                    <PaginationNext
                      onClick={() => onPageChange(data.pageNumber + 1)}
                      className={
                        !data.hasNextPage
                          ? "pointer-events-none opacity-50"
                          : "cursor-pointer"
                      }
                    />
                  </PaginationItem>
                </PaginationContent>
              </Pagination>
            </div>
          )}
        </>
      )}
    </div>
  );
};
