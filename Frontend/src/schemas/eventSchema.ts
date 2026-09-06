import { z } from 'zod';

export const createEventSchema = z.object({
  name: z.string().min(3, 'O nome deve ter no mínimo 3 caracteres'),
  description: z.string().min(10, 'A descrição deve ter no mínimo 10 caracteres'),
  institutionId: z.uuid('Selecione uma instituição válida').min(1, 'Instituição é obrigatória'),
  startDate: z.string().min(1, 'Data de início é obrigatória'),
  endDate: z.string().min(1, 'Data de término é obrigatória'),
  capacity: z.coerce.number().min(1, 'A capacidade deve ser de pelo menos 1 pessoa'),
  allowDocuments: z.boolean().default(false),
  eventType: z.number().min(1, 'Selecione o tipo do evento'),
  allowedCourses: z.array(z.uuid()).min(1, 'Selecione pelo menos um curso permitido'),
}).refine((data) => new Date(data.endDate) > new Date(data.startDate), {
  message: "A data de término deve ser maior que a data de início",
  path: ["endDate"],
});

export type CreateEventFormData = z.infer<typeof createEventSchema>;