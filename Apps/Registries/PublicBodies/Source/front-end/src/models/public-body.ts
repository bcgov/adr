import { z } from "zod";

import { ApiPublicBodyTypeSchema } from "./public-body-type";

// Shape of one public body object in API response
export const ApiPublicBodySchema = z.object({
  id: z.uuid().nullable(),
  staticId: z.uuid().nullable(),
  name: z.string().nullable(),
  acronym: z.string().nullable(),
  publicBodyTypeId: z.uuid().nullable(),
  publicBodyType: ApiPublicBodyTypeSchema,
  effectiveDate: z.string().nullable(),
  retirementDate: z.string().nullable(),
});

export type ApiPublicBody = z.infer<typeof ApiPublicBodySchema>;

// This is where comments get applied to the `PublicBody` type. Any comments
// added with `.describe()` on the `z` calls above will get stripped away in the
// `.transform()` step below, so any comments we want to flow through the type
// have to go here.
export const PublicBodySchema = ApiPublicBodySchema.transform((data) => ({
  /** System GUID for the public body */
  id: data.id,
  /** Legal name for the public body (ex: `Ministry of Citizens' Services`) */
  name: data.name,
  /** Internal acronym for the public body (ex: `CITZ` for Ministry of Citizens' Services) */
  acronym: data.acronym,
  /** Public body type information object for the public body */
  publicBodyType: {
    /** System GUID for the type */
    id: data.publicBodyType.id,
    /** Type name (ex: `Ministry`) */
    name: data.publicBodyType.name,
  },
  /** Date when the public body name took effect */
  effectiveDate: data.effectiveDate,
  /** Date when the public body name was retired */
  retirementDate: data.retirementDate,
}));

export type PublicBody = z.infer<typeof PublicBodySchema>;
