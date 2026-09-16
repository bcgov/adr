import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import {
    createGlossaryDraftMutation,
    deleteGlossaryDraftTermMutation,
    getGlossaryDraftOptions,
    getGlossaryDraftsOptions,
    publishGlossaryVersionMutation,
    putGlossaryDraftTermMutation,
    rebaseGlossaryDraftMutation,
} from "@/api/generated-semantics/@tanstack/react-query.gen";

function useInvalidateGlossaryEditing() {
    const queryClient = useQueryClient();

    return () =>
        queryClient.invalidateQueries({
            predicate: ({ queryKey }) => {
                const operation = (queryKey[0] as { _id?: string } | undefined)
                    ?._id;
                return (
                    operation === "getAllGlossary" ||
                    operation === "getGlossaryDraft" ||
                    operation === "getGlossaryDrafts" ||
                    operation === "getGlossaryVersions"
                );
            },
        });
}

export function useGlossaryDrafts(status?: string) {
    return useQuery({
        ...getGlossaryDraftsOptions({
            query: status ? { status } : undefined,
        }),
    });
}

export function useGlossaryDraft(draftId: string) {
    return useQuery({
        ...getGlossaryDraftOptions({ path: { draftId } }),
        enabled: Boolean(draftId),
    });
}

export function useCreateGlossaryDraft() {
    const invalidate = useInvalidateGlossaryEditing();
    return useMutation({
        ...createGlossaryDraftMutation(),
        onSuccess: invalidate,
    });
}

export function useSaveGlossaryTerm() {
    const invalidate = useInvalidateGlossaryEditing();
    return useMutation({
        ...putGlossaryDraftTermMutation(),
        onSuccess: invalidate,
    });
}

export function useDeleteGlossaryTerm() {
    const invalidate = useInvalidateGlossaryEditing();
    return useMutation({
        ...deleteGlossaryDraftTermMutation(),
        onSuccess: invalidate,
    });
}

export function useRebaseGlossaryDraft() {
    const invalidate = useInvalidateGlossaryEditing();
    return useMutation({
        ...rebaseGlossaryDraftMutation(),
        onSuccess: invalidate,
    });
}

export function usePublishGlossaryDraft() {
    const invalidate = useInvalidateGlossaryEditing();
    return useMutation({
        ...publishGlossaryVersionMutation(),
        onSuccess: invalidate,
    });
}
