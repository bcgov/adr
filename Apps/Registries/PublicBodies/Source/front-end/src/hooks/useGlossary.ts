import { useQuery } from "@tanstack/react-query";

import { getAllGlossaryOptions } from "@/api/generated-semantics/@tanstack/react-query.gen";

export default function useGlossary() {
    return useQuery({
        ...getAllGlossaryOptions(),
        select: (data) => data?.payload ?? [],
    });
}
