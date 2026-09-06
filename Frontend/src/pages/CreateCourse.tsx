import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { createCourseSchema, type CreateCourseFormData } from '../schemas/courseSchema';
import { BookOpen, Building2, AlertCircle, CheckCircle2 } from 'lucide-react';

interface OptionItem {
    id: string;
    name: string;
}

export const CreateCourse: React.FC = () => {
    const [institutions, setInstitutions] = useState<OptionItem[]>([]);
    const [apiError, setApiError] = useState<string | null>(null);
    const [isSuccess, setIsSuccess] = useState(false);
    const navigate = useNavigate();

    const {
        register,
        handleSubmit,
        formState: { errors, isSubmitting },
    } = useForm<CreateCourseFormData>({
        resolver: zodResolver(createCourseSchema),
        mode: 'onBlur',
    });

    useEffect(() => {
        api.get<OptionItem[]>('/institutions').then((res) => setInstitutions(res.data));
    }, []);

    const onSubmit = async (data: CreateCourseFormData) => {
        setApiError(null);
        try {
            await api.post('/courses', data);
            setIsSuccess(true);
            setTimeout(() => navigate('/dashboard'), 2000);
        } catch (err: any) {
            setApiError(err.response?.data?.message || 'Erro ao cadastrar curso.');
        }
    };

    return (
        <div className="flex min-h-screen items-center justify-center bg-slate-100 p-4 py-12">
            <div className="w-full max-w-lg rounded-2xl bg-white p-8 shadow-xl">
                <div className="mb-6 text-center">
                    <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-indigo-50 text-indigo-600">
                        <BookOpen className="h-6 w-6" />
                    </div>
                    <h1 className="text-2xl font-bold tracking-tight text-slate-900">Cadastro de Curso</h1>
                </div>

                {apiError && (
                    <div className="mb-6 flex items-center gap-2 rounded-lg bg-red-50 p-4 text-sm text-red-700">
                        <AlertCircle className="h-5 w-5 shrink-0" />
                        <span>{apiError}</span>
                    </div>
                )}

                {isSuccess && (
                    <div className="mb-6 flex items-center gap-2 rounded-lg bg-emerald-50 p-4 text-sm text-emerald-700">
                        <CheckCircle2 className="h-5 w-5 shrink-0" />
                        <span>Curso cadastrado com sucesso!</span>
                    </div>
                )}

                <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-slate-700">Nome do Curso</label>
                        <input
                            {...register('name')}
                            type="text"
                            placeholder="Ex: Desenvolvimento de Software Corporativo"
                            className="mt-1 w-full rounded-lg border border-slate-300 py-2.5 px-4 text-sm focus:border-indigo-600 focus:outline-none focus:ring-1 focus:ring-indigo-600"
                        />
                        {errors.name && <p className="mt-1 text-xs text-red-600">{errors.name.message}</p>}
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-slate-700">Instituição</label>
                        <div className="relative mt-1">
                            <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                                <Building2 className="h-5 w-5" />
                            </span>
                            <select
                                {...register('institutionId')}
                                defaultValue=""
                                className="w-full rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 text-sm focus:border-indigo-600 focus:outline-none focus:ring-1 focus:ring-indigo-600"
                            >
                                <option value="" disabled>Selecione a Instituição</option>
                                {institutions.map((inst) => (
                                    <option key={inst.id} value={inst.id}>{inst.name}</option>
                                ))}
                            </select>
                        </div>
                        {errors.institutionId && <p className="mt-1 text-xs text-red-600">{errors.institutionId.message}</p>}
                    </div>

                    <button
                        type="submit"
                        disabled={isSubmitting}
                        className="w-full rounded-lg bg-indigo-600 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
                    >
                        {isSubmitting ? 'Cadastrando...' : 'Cadastrar Curso'}
                    </button>
                </form>
            </div>
        </div>
    );
};