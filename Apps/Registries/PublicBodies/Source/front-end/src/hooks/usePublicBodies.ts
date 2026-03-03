import { useQuery } from "@tanstack/react-query";

import getPublicBodies from "../api/public-bodies";

export default function usePublicBodies() {
  return useQuery({
    queryKey: ["publicBodiesData"],
    queryFn: getPublicBodies,
  });
}
