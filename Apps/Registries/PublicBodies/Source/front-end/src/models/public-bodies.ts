import { z } from "zod";

import { ApiPublicBodySchema } from "./public-body";

// Shape of raw API response
export const ApiPublicBodiesResponseSchema = z.object({
  payload: z.array(ApiPublicBodySchema).describe("Array of public bodies"),
  datetimeRequested: z.string().describe("Datetime of the API request"),
});

export type ApiPublicBodiesResponse = z.infer<
  typeof ApiPublicBodiesResponseSchema
>;
