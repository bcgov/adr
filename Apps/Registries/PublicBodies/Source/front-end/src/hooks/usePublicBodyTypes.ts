import { useQuery } from "@tanstack/react-query";

import { getPublicBodyTypesOptions } from "../api/generated-publicbodies/@tanstack/react-query.gen";

export default function usePublicBodyTypes() {
    return useQuery({
        ...getPublicBodyTypesOptions(),
        select: (data) => data?.payload ?? [],
    });
}
