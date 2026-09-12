import React, { useEffect } from "react";
import { useForm, Controller, type Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
    createInstitutionSchema,
    type CreateInstitutionFormData,
} from "@/schemas/institutionSchema";
import { maskCnpj, maskPhone } from "@/utils/masks";
import { Loader2, AlertCircle, CheckCircle2, Building2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

interface CreateInstitutionFormProps {
    currentInstitutionData?: CreateInstitutionFormData | null;
    onSubmit: (data: CreateInstitutionFormData) => void;
    isSubmitting: boolean;
    isSuccess: boolean;
    apiError: string | null;
}

export const CreateInstitutionForm: React.FC<CreateInstitutionFormProps> = ({
    currentInstitutionData,
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
    } = useForm<CreateInstitutionFormData>({
        resolver: zodResolver(createInstitutionSchema) as Resolver<CreateInstitutionFormData>,
        mode: "onBlur",
    });

    useEffect(() => {
        if (currentInstitutionData) {
            reset({
                name: currentInstitutionData.name || "",
                cnpj: currentInstitutionData.cnpj || "",
                address: currentInstitutionData.address || "",
                phone: currentInstitutionData.phone || "",
            });
        }
    }, [currentInstitutionData, reset]);

    return (
        <Card className="surface-card max-w-2xl mx-auto shadow-none">
            <CardHeader className="text-center pb-8 pt-6">
                <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600">
                    <Building2 className="h-6 w-6" />
                </div>
                <CardTitle className="text-2xl font-bold text-slate-900">
                    {currentInstitutionData ? "Atualizar instituição" : "Nova instituição"}
                </CardTitle>
                <p className="mt-1 text-sm text-slate-600">
                    {currentInstitutionData
                        ? "Atualize os dados da instituição."
                        : "Registre uma nova instituição parceira."}
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
                            {currentInstitutionData
                                ? "Instituição atualizada com sucesso!"
                                : "Instituição cadastrada com sucesso!"}
                        </span>
                    </div>
                )}

                <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <div className="sm:col-span-2 space-y-1">
                            <Label htmlFor="name">Nome da instituição</Label>
                            <Input id="name" {...register("name")} placeholder="Ex: UMC" />
                            {errors.name && <p className="text-xs text-red-600">{errors.name.message}</p>}
                        </div>
                        <div className="space-y-1">
                            <Label>CNPJ</Label>
                            <Controller
                                name="cnpj"
                                control={control}
                                render={({ field }) => (
                                    <Input
                                        value={field.value ? maskCnpj(field.value) : ""}
                                        onChange={(e) => field.onChange(maskCnpj(e.target.value))}
                                        placeholder="00.000.000/0000-00"
                                        maxLength={18}
                                    />
                                )}
                            />
                            {errors.cnpj && <p className="text-xs text-red-600">{errors.cnpj.message}</p>}
                        </div>

                        <div className="space-y-1">
                            <Label>Telefone</Label>
                            <Controller
                                name="phone"
                                control={control}
                                render={({ field }) => (
                                    <Input
                                        value={field.value ? maskPhone(field.value) : ""}
                                        onChange={(e) => field.onChange(maskPhone(e.target.value))}
                                        placeholder="(11) 91234-5678"
                                        maxLength={15}
                                    />
                                )}
                            />
                            {errors.phone && <p className="text-xs text-red-600">{errors.phone.message}</p>}
                        </div>

                        <div className="sm:col-span-2 space-y-1">
                            <Label htmlFor="address">Endereço</Label>
                            <Input id="address" {...register("address")} placeholder="Rua, número, bairro, cidade" />
                            {errors.address && <p className="text-xs text-red-600">{errors.address.message}</p>}
                        </div>
                    </div>

                    <Button
                        type="submit"
                        disabled={isSubmitting || isSuccess}
                        className="primary-action mt-2 h-11 w-full">
                        {isSubmitting ? (
                            <>
                                <Loader2 className="mr-2 h-4 w-4 animate-spin" /> Salvando...
                            </>
                        ) : currentInstitutionData ? (
                            "Atualizar instituição"
                        ) : (
                            "Cadastrar instituição"
                        )}
                    </Button>
                </form>
            </CardContent>
        </Card>
    );
};