import { useQuery } from "@tanstack/react-query";

import { getAllDictionariesOptions } from "@/api/generated-semantics/@tanstack/react-query.gen";

export default function useDictionary() {
    return useQuery({
        ...getAllDictionariesOptions(),
        select: (data) => data?.payload ?? [],
    });
}
