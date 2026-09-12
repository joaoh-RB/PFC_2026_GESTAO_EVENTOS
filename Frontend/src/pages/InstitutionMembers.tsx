import React, { useEffect, useState, useCallback } from "react";
import { api } from "../services/api";
import { AlertCircle, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useAuth } from "../hooks/useAuth";
import type { OptionItem } from "@/types/optionItem";
import { type PagedResult } from "@/types/utils";
import { InitialLoading } from "@/components/InitialLoading";
import {
  InstitutionMemberList,
  type InstitutionMemberFilterState,
  type InstitutionMemberItem,
} from "@/components/users/InstitutionMemberList";
import type { CreateInstitutionMemberFormData } from "@/schemas/userSchema";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { CreateInstitutionMemberForm } from "@/components/users/CreateInstitutionMemberForm";
import { PageHeader } from "@/components/PageHeader";

export const InstitutionMembers: React.FC = () => {
  const pageSize = 10;
  const [institutions, setInstitutions] = useState<OptionItem[]>([]);
  const [courses, setCourses] = useState<OptionItem[]>([]);
  const [userRoles, setUserRoles] = useState<OptionItem[]>([]);
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);
  const [editing, setIsEditing] = useState(false);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const [usersData, setUsersData] = useState<
    PagedResult<InstitutionMemberItem>
  >({
    items: [],
    totalItems: 0,
    pageNumber: 1,
    pageSize: pageSize,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  });
  const [currentUserBeingUpdated, setUserBeingUpdated] =
    useState<CreateInstitutionMemberFormData | null>(null);
  const fetchCourses = async (institutionId: string | null) => {
    await api
      .get<OptionItem[]>("/courses", { params: { institutionId } })
      .then((res) => {
        setCourses(res.data);
      });
  };
  const [isUsersLoading, setIsUsersLoading] = useState(false);
  const [filters, setFilters] = useState<InstitutionMemberFilterState>();
  const [currentPage, setCurrentPage] = useState(1);
  const { user } = useAuth();
  const userInstitutionId = user?.institutionId || null;

  useEffect(() => {
    const controller = new AbortController();

    const loadInitialData = async () => {
      try {
        const [instRes, userRoles] = await Promise.all([
          api.get<OptionItem[]>("/institutions"),
          api.get<OptionItem[]>("/users/roles/institution-members"),
        ]);
        setInstitutions(instRes.data);
        fetchCourses(userInstitutionId);
        setUserRoles(userRoles.data);
      } catch {
        if (controller.signal.aborted) return;
        setPageError("Não foi possível carregar as informações do sistema.");
      } finally {
        setIsInitialLoading(false);
      }
    };

    loadInitialData();

    return () => {
      controller.abort();
    };
  }, []);

  const fetchUsers = useCallback(
    async (appliedFilters = filters) => {
      setIsUsersLoading(true);
      try {
        const params: Record<string, string | number | undefined> = {
          pageNumber: currentPage,
          pageSize: pageSize,
        };
        if (appliedFilters?.userRoleId) {
          params.userRole = appliedFilters.userRoleId;
        }
        if (appliedFilters?.institutionId || userInstitutionId) {
          params.institutionId = userInstitutionId ?? appliedFilters?.institutionId;
        }

        const res = await api.get<PagedResult<InstitutionMemberItem>>(
          "/users/institution-members",
          {
            params,
          },
        );

        setUsersData(res.data);
      } catch (err) {
        console.error("Erro ao buscar usuários", err);
      } finally {
        setIsUsersLoading(false);
      }
    },
    [filters, currentPage],
  );

  useEffect(() => {
    if (!editing) {
      fetchUsers(filters);
    }
  }, [editing, currentPage, fetchUsers, filters]);

  //handle dos outros copmonentes
  const handleFilterChange = (newFilters: InstitutionMemberFilterState) => {
    setCurrentPage(1);
    setFilters(newFilters);
  };

  const handleInstitutionChange = (institutionId: string) => {
    fetchCourses(institutionId);
  };
  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };
  const handleStartEdition = (user: InstitutionMemberItem) => {
    fetchCourses(user.institutionId);
    setUserBeingUpdated({
      id: user.id,
      name: user.name,
      email: user.email,
      institutionId: user.institutionId,
      userRole: user.userRoleId,
      password: "",
      courses: user.coursesIds || [],
    });
    setIsEditing(true);
  };

  //chamadas api evento
  const handleCreateInstitutionMember = async (
    data: CreateInstitutionMemberFormData,
  ) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      if (currentUserBeingUpdated) {
        await api.put(
          `/users/institution-members/${currentUserBeingUpdated.id}`,
          data,
        );
      } else {
        await api.post("/users/institution-members", data);
      }
      setIsSuccess(true);
      setTimeout(() => {
        setIsSuccess(false);
        setIsEditing(false);
      }, 1500);
    } catch (err: any) {
      setSubmitError(err.response?.data?.message || "Erro ao criar o evento.");
    } finally {
      setIsSubmitting(false);
    }
  };
  const handleDelete = async (userId: string, isActive: boolean) => {
    await api.patch(`/users/${userId}/active`, {
      isActive: isActive,
    });
    setCurrentPage(1);
    fetchUsers();
  };
  if (isInitialLoading) {
    return <InitialLoading></InitialLoading>;
  }

  if (pageError) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-4">
        <div className="flex max-w-md flex-col items-center gap-4 rounded-xl bg-white p-8 text-center shadow-lg">
          <AlertCircle className="h-12 w-12 text-red-500" />
          <p className="text-slate-800 font-medium">{pageError}</p>
          <Button
            onClick={() => window.location.reload()}
            variant="outline"
            className="mt-2">
            Tentar novamente
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="app-page">
        <PageHeader
          title="Membros"
          description="Gerencie professores, secretarias e responsáveis das instituições."
          action={
          <Button
            onClick={() => {
              setIsEditing(!editing);
              setSubmitError(null);
              setUserBeingUpdated(null);
            }}
            variant={editing ? "outline" : "default"}
            className="primary-action">
            <Plus className="mr-2 h-4 w-4" />
            Novo membro
          </Button>
          }
        />

        <InstitutionMemberList
          data={usersData}
          institutions={institutions}
          roles={userRoles}
          currentFilters={filters}
          onFilter={handleFilterChange}
          onPageChange={handlePageChange}
          handleStartEdition={handleStartEdition}
          handleDelete={handleDelete}
          isLoading={isUsersLoading}
          userInstitutionId={userInstitutionId}
        />
        <Dialog open={editing} onOpenChange={() => setIsEditing(false)}>
          <DialogContent className={"sm:max-w-[500px]"}>
            <>
              <CreateInstitutionMemberForm
                apiError={submitError}
                institutions={institutions}
                courses={courses}
                userRoles={userRoles}
                userInstitutionId={userInstitutionId}
                currentUserData={currentUserBeingUpdated}
                onSubmit={handleCreateInstitutionMember}
                isSubmitting={isSubmitting}
                isSuccess={isSuccess}
                handleInstitutionChange={
                  handleInstitutionChange
                }></CreateInstitutionMemberForm>
            </>
          </DialogContent>
        </Dialog>
    </div>
  );
};
