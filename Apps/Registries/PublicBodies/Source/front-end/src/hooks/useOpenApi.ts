import { useQuery } from "@tanstack/react-query";

import { getPublicBodiesOpenApiSchema } from "../api/public-bodies";

export default function UseOpenApi() {
  return useQuery({
    queryKey: ["pulbliBoodiesSpec"],
    queryFn: getPublicBodiesOpenApiSchema,
  });
}
