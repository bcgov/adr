import { useQuery } from "@tanstack/react-query";

import { getAllPublicBodiesOptions } from "../api/generated-publicbodies/@tanstack/react-query.gen";

export default function usePublicBodies() {
    return useQuery({
        ...getAllPublicBodiesOptions(),
        select: (data) => data?.payload ?? [],
    });
}
