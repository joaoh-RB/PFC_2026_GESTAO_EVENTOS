import { z } from 'zod';

export const createCourseSchema = z.object({
    name: z
        .string()
        .trim()
        .min(1, 'O nome do curso é obrigatório.')
        .max(150, 'O nome não pode exceder 150 caracteres.'),
    institutionId: z
        .uuid('Selecione uma instituição válida.'),
});
export type CreateCourseFormData = z.infer<typeof createCourseSchema>;