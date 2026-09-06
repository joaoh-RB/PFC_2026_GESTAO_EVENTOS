import { z } from 'zod';
import { isValidCnpj, unmaskPhone } from '../utils/masks';

export const createInstitutionSchema = z.object({
    name: z
        .string()
        .trim()
        .min(1, 'O nome da instituição é obrigatório.')
        .max(150, 'O nome não pode exceder 150 caracteres.'),
    cnpj: z
        .string()
        .optional()
        .refine((val) => !val || isValidCnpj(val), {
            message: 'CNPJ inválido.',
        }),
    address: z
        .string()
        .max(250, 'O endereço não pode exceder 250 caracteres.')
        .optional(),
    phone: z
        .string()
        .optional()
        .transform((val) => (val ? unmaskPhone(val) : val)),
});
export type CreateInstitutionFormData = z.infer<typeof createInstitutionSchema>;