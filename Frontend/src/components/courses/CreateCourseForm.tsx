import React, { useEffect } from "react";
import { useForm, Controller, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { createCourseSchema, type CreateCourseFormData } from "@/schemas/courseSchema";
import { Loader2, AlertCircle, CheckCircle2, BookOpen } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import type { OptionItem } from "@/types/optionItem";

interface CreateCourseFormProps {
    institutions: OptionItem[];
    userInstitutionId?: string | null;
    currentCourseData?: CreateCourseFormData | null;
    onSubmit: (data: CreateCourseFormData) => void;
    isSubmitting: boolean;
    isSuccess: boolean;
    apiError: string | null;
}

export const CreateCourseForm: React.FC<CreateCourseFormProps> = ({
    institutions,
    userInstitutionId,
    currentCourseData,
    onSubmit,
    isSubmitting,
    isSuccess,
    apiError,
}) => {
    const {
        register,
        handleSubmit,
        control,
        reset,
        formState: { errors },
    } = useForm<CreateCourseFormData>({
        resolver: zodResolver(createCourseSchema) as Resolver<CreateCourseFormData>,
        mode: "onBlur",
        defaultValues: {
            name: "",
            institutionId: userInstitutionId || "",
        },
    });

    useEffect(() => {
        if (currentCourseData) {
            reset({
                name: currentCourseData.name || "",
                institutionId: currentCourseData.institutionId || userInstitutionId || "",
            });
        }
    }, [currentCourseData, reset, userInstitutionId]);

    return (
        <Card className="surface-card max-w-2xl mx-auto shadow-none">
            <CardHeader className="text-center pb-8 pt-6">
                <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-50 text-amber-600">
                    <BookOpen className="h-6 w-6" />
                </div>
                <CardTitle className="text-2xl font-bold text-slate-900">
                    {currentCourseData ? "Atualizar curso" : "Novo curso"}
                </CardTitle>
                <p className="mt-1 text-sm text-slate-600">
                    {currentCourseData
                        ? "Atualize os dados do curso."
                        : "Preencha os dados para cadastrar um novo curso."}
                </p>
            </CardHeader>

            <CardContent>
                {apiError && (
                    <div className="mb-6 flex items-center gap-2 rounded-lg bg-red-50 p-4 text-sm text-red-700">
                        <AlertCircle className="h-5 w-5 shrink-0" />
                        <span>{apiError}</span>
                    </div>
                )}
                {isSuccess && (
                    <div className="mb-6 flex items-center gap-2 rounded-lg bg-emerald-50 p-4 text-sm text-emerald-700">
                        <CheckCircle2 className="h-5 w-5 shrink-0" />
                        <span>
                            {currentCourseData ? "Curso atualizado com sucesso!" : "Curso cadastrado com sucesso!"}
                        </span>
                    </div>
                )}

                <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                    <div className="space-y-1">
                        <Label htmlFor="name">Nome do curso</Label>
                        <Input id="name" {...register("name")} placeholder="Ex: Engenharia de Software" />
                        {errors.name && <p className="text-xs text-red-600">{errors.name.message}</p>}
                    </div>
                    {!currentCourseData && !userInstitutionId && (
                        <div className="space-y-1">
                            <Label>Instituição</Label>
                            <Controller
                                name="institutionId"
                                control={control}
                                render={({ field }) => (
                                    <Select items={institutions} onValueChange={field.onChange} value={field.value}>
                                        <SelectTrigger className="w-full">
                                            <SelectValue placeholder="Selecione uma instituição" />
                                        </SelectTrigger>
                                        <SelectContent alignItemWithTrigger={true}>
                                            {institutions.map((inst) => (
                                                <SelectItem key={inst.value} value={inst.value}>
                                                    {inst.label}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                )}
                            />
                            {errors.institutionId && (
                                <p className="text-xs text-red-600">{errors.institutionId.message}</p>
                            )}
                        </div>
                    )}

                    <Button
                        type="submit"
                        disabled={isSubmitting || isSuccess}
                        className="primary-action mt-2 h-11 w-full">
                        {isSubmitting ? (
                            <>
                                <Loader2 className="mr-2 h-4 w-4 animate-spin" /> Salvando...
                            </>
                        ) : currentCourseData ? (
                            "Atualizar curso"
                        ) : (
                            "Cadastrar curso"
                        )}
                    </Button>
                </form>
            </CardContent>
        </Card>
    );
};