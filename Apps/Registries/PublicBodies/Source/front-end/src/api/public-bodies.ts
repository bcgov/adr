import { API_URL } from "../constants";

export async function getPublicBodiesOpenApiSchema(): Promise<unknown> {
  const response = await fetch(`${API_URL}/swagger/v1/swagger.json`);
  return response.json();
}
