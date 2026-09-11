import React, { useState } from 'react';
import { useForm, Controller, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { unmaskCnpj, unmaskPhone, maskCnpj, maskPhone } from '../utils/masks';
import { createInstitutionSchema, type CreateInstitutionFormData } from '../schemas/institutionSchema';
import { Building2, Hash, MapPin, Phone, AlertCircle, CheckCircle2 } from 'lucide-react';

export const CreateInstitution: React.FC = () => {
  const [apiError, setApiError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<CreateInstitutionFormData>({
    resolver: zodResolver(createInstitutionSchema) as Resolver<CreateInstitutionFormData>,
    mode: 'onBlur',
  });

  const onSubmit = async (data: CreateInstitutionFormData) => {
    setApiError(null);
    try {
        await api.post('/institutions', {
            name: data.name,
            cnpj: data.cnpj ? unmaskCnpj(data.cnpj) : undefined,
            address: data.address,
            phone: data.phone ? unmaskPhone(data.phone) : undefined,
        });
        setIsSuccess(true);
        setTimeout(() => navigate('/dashboard'), 2000);
    } catch (err: any) {
        setApiError(err.response?.data?.message || 'Erro ao cadastrar instituição.');
    }
};

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 p-4 py-12">
      <div className="w-full max-w-2xl rounded-2xl bg-white p-8 shadow-xl">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-indigo-50 text-indigo-600">
            <Building2 className="h-6 w-6" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">Cadastro de Instituição</h1>
          <p className="mt-1 text-sm text-slate-600">Registre uma nova instituição parceira</p>
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
            <span>Instituição cadastrada com sucesso!</span>
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {/* Nome */}
            <div className="sm:col-span-2">
              <label className="block text-sm font-medium text-slate-700">Nome da Instituição</label>
              <div className="relative mt-1">
                <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                  <Building2 className="h-5 w-5" />
                </span>
                <input
                  {...register('name')}
                  type="text"
                  placeholder="Ex: UMC"
                  className="w-full rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 text-sm focus:border-indigo-600 focus:outline-none focus:ring-1 focus:ring-indigo-600"
                />
              </div>
              {errors.name && <p className="mt-1 text-xs text-red-600">{errors.name.message}</p>}
            </div>

            {/* CNPJ — usa Controller porque precisamos interceptar o onChange pra aplicar a máscara */}
            <div>
              <label className="block text-sm font-medium text-slate-700">CNPJ</label>
              <div className="relative mt-1">
                <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                  <Hash className="h-5 w-5" />
                </span>
                <Controller
                  name="cnpj"
                  control={control}
                  render={({ field }) => (
                    <input
                      {...field}
                      value={field.value ? maskCnpj(field.value) : ''}
                      onChange={(e) => field.onChange(maskCnpj(e.target.value))}
                      type="text"
                      placeholder="00.000.000/0000-00"
                      maxLength={18}
                      className="w-full rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 text-sm focus:border-indigo-600 focus:outline-none focus:ring-1 focus:ring-indigo-600"
                    />
                  )}
                />
              </div>
              {errors.cnpj && <p className="mt-1 text-xs text-red-600">{errors.cnpj.message}</p>}
            </div>

            {/* Telefone — mesma ideia do CNPJ */}
            <div>
              <label className="block text-sm font-medium text-slate-700">Telefone</label>
              <div className="relative mt-1">
                <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                  <Phone className="h-5 w-5" />
                </span>
                <Controller
                  name="phone"
                  control={control}
                  render={({ field }) => (
                    <input
                      {...field}
                      value={field.value ? maskPhone(field.value) : ''}
                      onChange={(e) => field.onChange(maskPhone(e.target.value))}
                      type="text"
                      placeholder="(11) 91234-5678"
                      maxLength={15}
                      className="w-full rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 text-sm focus:border-indigo-600 focus:outline-none focus:ring-1 focus:ring-indigo-600"
                    />
                  )}
                />
              </div>
            </div>

            {/* Endereço */}
            <div className="sm:col-span-2">
              <label className="block text-sm font-medium text-slate-700">Endereço</label>
              <div className="relative mt-1">
                <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                  <MapPin className="h-5 w-5" />
                </span>
                <input
                  {...register('address')}
                  type="text"
                  placeholder="Rua, número, bairro, cidade"
                  className="w-full rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 text-sm focus:border-indigo-600 focus:outline-none focus:ring-1 focus:ring-indigo-600"
                />
              </div>
            </div>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full rounded-lg bg-indigo-600 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
          >
            {isSubmitting ? 'Cadastrando...' : 'Cadastrar Instituição'}
          </button>
        </form>
      </div>
    </div>
  );
};
