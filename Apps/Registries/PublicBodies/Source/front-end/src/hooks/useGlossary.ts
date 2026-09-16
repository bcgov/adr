import { useQuery } from "@tanstack/react-query";

import {
    getAllGlossaryOptions,
    getGlossaryTermHistoryOptions,
    getGlossaryVersionOptions,
    getGlossaryVersionsOptions,
} from "@/api/generated-semantics/@tanstack/react-query.gen";

export default function useGlossary() {
    return useQuery(getAllGlossaryOptions());
}

export function useGlossaryVersion(version?: string) {
    return useQuery({
        ...getGlossaryVersionOptions({
            path: { glossaryVersion: version ?? "" },
        }),
        enabled: Boolean(version),
    });
}

export function useGlossaryVersions() {
    return useQuery(getGlossaryVersionsOptions());
}

export function useGlossaryTermHistory(glossaryVersion: string, term: string) {
    return useQuery({
        ...getGlossaryTermHistoryOptions({
            path: { glossaryVersion, term },
        }),
        enabled: Boolean(glossaryVersion && term),
    });
}
