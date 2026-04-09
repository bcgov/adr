import { useQuery } from "@tanstack/react-query";

import { getPublicBodiesOpenApiSchema } from "../api/public-bodies";

export default function useOpenApi() {
    return useQuery({
        queryKey: ["publicBodiesSpec"],
        queryFn: getPublicBodiesOpenApiSchema,
    });
}
