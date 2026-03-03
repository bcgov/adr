import { z } from "zod";

// Shape of one public body type object in API response
export const ApiPublicBodyTypeSchema = z.object({
  id: z.uuid().nullable().describe("System GUID for the type"),
  staticId: z.uuid().nullable().describe("Static system GUID for the type"),
  code: z.string().nullable().describe("Code for the type"),
  name: z.string().nullable().describe("Type name (ex: `Ministry`)"),
  description: z
    .string()
    .nullable()
    .describe("Written description of the public body type"),
  effectiveDate: z
    .string()
    .nullable()
    .describe("Date when the public body name took effect"),
  retirementDate: z
    .string()
    .nullable()
    .describe("Date when the public body name was retired"),
});

export type ApiPublicBodyType = z.infer<typeof ApiPublicBodyTypeSchema>;
