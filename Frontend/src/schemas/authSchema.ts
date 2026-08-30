import { z } from 'zod';

export const loginSchema = z.object({
    email: z
        .email('Insira um e-mail válido.')
        .trim()
        .min(1, 'O e-mail é obrigatório.'),
    password: z
        .string()
        .min(1, 'A senha é obrigatória.'),
    twoFactorCode: z
        .string()
        .optional(),
});

export type LoginFormData = z.infer<typeof loginSchema>;

export const registerStudentSchema = z
    .object({
        name: z
            .string()
            .trim()
            .min(3, 'O nome deve ter pelo menos 3 caracteres.')
            .max(150, 'O nome não pode exceder 150 caracteres.'),
        email: z
            .email('Insira um e-mail válido.')
            .trim()
            .max(150, 'E-mail muito longo.'),
        uniqueIdentifier: z
            .string()
            .trim()
            .min(1, 'O RGM / Matrícula é obrigatório.')
            .max(20, 'O identificador deve ter no máximo 20 caracteres.'),
        institutionId: z
            .uuid('Selecione uma instituição válida.'),
        courseId: z
            .uuid('Selecione um curso válido.'),
        password: z
            .string()
            .min(8, 'A senha deve ter no mínimo 8 caracteres.')
            .regex(/[A-Z]/, 'A senha deve conter ao menos uma letra maiúscula.')
            .regex(/[0-9]/, 'A senha deve conter ao menos um número.')
            .regex(/[\W_]/, 'A senha deve conter ao menos um caractere especial.'),
        confirmPassword: z
            .string()
            .min(1, 'Confirme a sua senha.'),
    })
    .refine((data) => data.password === data.confirmPassword, {
        message: 'As senhas não coincidem.',
        path: ['confirmPassword'],
    });

export type RegisterStudentFormData = z.infer<typeof registerStudentSchema>;