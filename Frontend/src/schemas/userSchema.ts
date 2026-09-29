import z from "zod";

export const createInstitutionMemberSchema = z
  .object({
    id: z.uuid().optional(),
    name: z
      .string()
      .trim()
      .min(3, "O nome deve ter no mínimo 3 caracteres")
      .max(150, "O nome não pode exceder 150 caracteres"),
    email: z.email("Digite um email válido").trim(),
    institutionId: z
      .uuid("Selecione uma instituição válida")
      .min(1, "Instituição é obrigatória"),
    courses: z.array(z.uuid()).optional(),
    userRole: z.preprocess(
      (value) => (value === "" ? undefined : value),
      z.coerce
        .number({
          error: "Selecione um cargo",
        })
        .min(1, "Selecione um cargo"),
    ),
  })
  .superRefine((data, ctx) => {
    if (data.userRole === 2 && (!data.courses || data.courses.length === 0)) {
      ctx.addIssue({
        code: "custom",
        message: "O professor deve estar vinculado a pelo menos um curso",
        path: ["courses"],
      });
    }
    console.log("Dados no refine:", data);
    }
);

export type CreateInstitutionMemberFormData = z.infer<
  typeof createInstitutionMemberSchema
>;
