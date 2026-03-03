import { ApiPublicBodiesResponseSchema } from "../models/public-bodies";
import type { PublicBody } from "../models/public-body";

import { API_URL } from "../constants";

export default async function getPublicBodies(): Promise<PublicBody[]> {
  const response = await fetch(`${API_URL}/publicbodies/names`);
  const data = await response.json();

  // If Zod's .parse() throws an error here, it will be caught by TanStack Query.
  return ApiPublicBodiesResponseSchema.parse(data).payload;
}

export async function getPublicBodiesOpenApiSchema(): Promise<any> {
  const response = await fetch(`${API_URL}/swagger/v1/swagger.json`);
  const data = await response.json();

  // If Zod's .parse() throws an error here, it will be caught by TanStack Query.
  return data;
}
