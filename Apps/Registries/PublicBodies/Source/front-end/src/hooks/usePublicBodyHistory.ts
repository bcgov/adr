import { useQuery } from "@tanstack/react-query";

import { getPublicBodyHistoryOptions } from "../api/generated-publicbodies/@tanstack/react-query.gen";

export default function usePublicBodyHistory(id: string) {
    return useQuery({
        ...getPublicBodyHistoryOptions({ path: { id } }),
        select: (data) => data?.payload ?? null,
    });
}
