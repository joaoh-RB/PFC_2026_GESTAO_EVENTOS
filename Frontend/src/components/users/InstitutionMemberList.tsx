import React, { useState } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
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
import { Loader2 } from "lucide-react";
import { type PagedResult } from "@/types/utils";
import type { OptionItem } from "@/types/optionItem";
import { DeleteConfirmation } from "@/components/ui/deleteConfirm";
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from "../ui/table";
import { useAuth } from "@/hooks/useAuth";

export interface InstitutionMemberItem {
  id: string;
  name: string;
  email: string;
  userRole: string;
  userRoleId: number;
  institutionId: string;
  institutionName: string;
  coursesNames?: string[];
  coursesIds?: string[];
  isActive: boolean;
}

export interface InstitutionMemberFilterState {
  institutionId?: string;
  userRoleId?: number;
}

interface InstitutionMemberListProps {
  data: PagedResult<InstitutionMemberItem>;
  institutions: OptionItem[];
  roles: OptionItem[];
  isLoading: boolean;
  currentFilters?: InstitutionMemberFilterState;
  userInstitutionId?: string | null;
  onFilter: (filters: InstitutionMemberFilterState) => void;
  onPageChange: (page: number) => void;
  handleStartEdition: (userData: InstitutionMemberItem) => void;
  handleDelete: (userId: string, isActive: boolean) => void;
}

export const InstitutionMemberList: React.FC<InstitutionMemberListProps> = ({
  data,
  institutions,
  roles,
  isLoading,
  userInstitutionId,
  currentFilters,
  onFilter,
  onPageChange,
  handleStartEdition,
  handleDelete,
}) => {
  const allInstitutionsName = "Todas as instituições";
  const [userRoleId, setUserRoleId] = useState<number | string>(
    currentFilters?.userRoleId || "Todos os cargos",
  );
  const [institutionId, setInstitutionId] = useState<string>(
    currentFilters?.institutionId || allInstitutionsName,
  );
  const { user: currentUser } = useAuth();
  const handleApplyFilter = () => {
    onFilter({
      userRoleId:
        userRoleId !== "Todos os cargos"
          ? parseInt(userRoleId as string)
          : undefined,
      institutionId:
        institutionId !== allInstitutionsName ? institutionId : undefined,
    });
  };

  return (
    <div className="space-y-6">
      <Card className="bg-white">
        <CardContent className="pt-6">
          <div className="grid grid-cols-1 sm:grid-cols-6 gap-2 items-end">
            <div className="col-span-2">
              <Label htmlFor="filter-user-role" className={"mb-2"}>Cargo</Label>
              <Select
                id="filter-user-role"
                items={roles}
                value={userRoleId.toString()}
                onValueChange={(e) => setUserRoleId(e || "Todos os cargos")}>
                <SelectTrigger className={"w-full"}>
                  <SelectValue placeholder={"Todos os cargos"} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Todos os cargos">
                    Todos os cargos
                  </SelectItem>
                  {roles.map((role) => (
                    <SelectItem key={role.value} value={role.value}>
                      {role.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            {!userInstitutionId && ( 
            <div className={"col-span-2"}>
              <Label htmlFor="filter-institution" className={"mb-2 "}>
                Instituição
              </Label>
              <Select
                id="filter-institution"
                value={institutionId}
                items={institutions}
                onValueChange={(value) =>
                  setInstitutionId(value ?? allInstitutionsName)
                }>
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
            )}
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
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 bg-white-100">
            {data.items.length === 0 && (
              <div className="col-span-full text-center text-slate-500 py-16">
                Nenhum usuário encontrado.
              </div>
            )}
          </div>
          {data.items.length > 0 && (
            <div className={"bg-white rounded-lg shadow-md overflow-hidden"}>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nome</TableHead>
                    <TableHead>Cargo</TableHead>
                    <TableHead>Instituição</TableHead>
                    <TableHead>Cursos</TableHead>
                    <TableHead>Ativo</TableHead>
                    <TableHead>Ações</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell>{user.name}</TableCell>
                      <TableCell>{user.userRole}</TableCell>
                      <TableCell>{user.institutionName}</TableCell>
                      <TableCell>
                        {user.coursesNames
                          ? user.coursesNames.join(", ")
                          : "Não se aplica"}
                      </TableCell>
                      <TableCell>{user.isActive ? "Sim" : "Não"}</TableCell>
                      <TableCell>
                        <div className="flex gap-2">
                          <Button
                            className="bg-indigo-600 hover:bg-indigo-700"
                            onClick={() => handleStartEdition(user)}>
                            Editar
                          </Button>
                          {user.isActive && user.id !== currentUser!.id &&(
                            <DeleteConfirmation
                              children={
                                <Button
                                  className={
                                    "bg-red-500 hover:bg-red-600 cursor-pointer"
                                  }>
                                  Desativar
                                </Button>
                              }
                              onDelete={() => handleDelete(user.id, false)}
                              descriptionText={
                                "Tem certeza que deseja desativar este usuário?"
                              }
                              confirmationText={"Desativar"}></DeleteConfirmation>
                          )}
                          {!user.isActive && (
                            <DeleteConfirmation
                              children={
                                <Button
                                  className={
                                    "bg-green-500 hover:bg-green-600 text-gray-100 cursor-pointer"
                                  }>
                                  Ativar
                                </Button>
                              }
                              onDelete={() => handleDelete(user.id, true)}
                              descriptionText={
                                "Tem certeza que deseja ativar este usuário?"
                              }
                              confirmationText={"Ativar"}
                              deleteButtonClassName={"bg-green-500 hover:bg-green-600 text-gray-100"}>
                            </DeleteConfirmation>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
                {data.totalPages > 1 && (
                  <TableFooter>
                    <TableRow className={"w-100"}>
                      <TableCell colSpan={5} className={"w-100"}>
                        <div className="flex justify-center pt-4">
                          <Pagination>
                            <PaginationContent>
                              <PaginationItem>
                                <PaginationPrevious
                                  onClick={() =>
                                    onPageChange(data.pageNumber - 1)
                                  }
                                  className={
                                    !data.hasPreviousPage
                                      ? "pointer-events-none opacity-50"
                                      : "cursor-pointer"
                                  }
                                />
                              </PaginationItem>

                              {Array.from(
                                { length: data.totalPages },
                                (_, index) => {
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
                                },
                              )}

                              <PaginationItem>
                                <PaginationNext
                                  onClick={() =>
                                    onPageChange(data.pageNumber + 1)
                                  }
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
                      </TableCell>
                    </TableRow>
                  </TableFooter>
                )}
              </Table>
            </div>
          )}
        </>
      )}
    </div>
  );
};
