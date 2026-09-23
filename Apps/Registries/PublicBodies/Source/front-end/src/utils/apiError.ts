import type { GlossaryInvalidTerm } from "@/models/glossary";

type ApiErrorPayload = {
    payload?: {
        invalidReasons?: string[] | null;
        invalidTerms?: GlossaryInvalidTerm[] | null;
        conflictingTerms?: string[] | null;
        status?: string | null;
    };
    detail?: string;
    title?: string;
};

function asApiError(error: unknown): ApiErrorPayload | undefined {
    return typeof error === "object" && error !== null
        ? (error as ApiErrorPayload)
        : undefined;
}

export function apiErrorMessage(
    error: unknown,
    fallback = "The request could not be completed.",
): string {
    if (error instanceof Error) return error.message;
    if (typeof error === "string" && error.trim()) return error;

    const apiError = asApiError(error);
    const reasons = apiError?.payload?.invalidReasons?.filter(Boolean);
    if (reasons?.length) return reasons.join(" ");
    return apiError?.detail ?? apiError?.title ?? fallback;
}

export function invalidTermsFromError(error: unknown): GlossaryInvalidTerm[] {
    return asApiError(error)?.payload?.invalidTerms ?? [];
}

export function conflictingTermsFromError(error: unknown): string[] {
    return asApiError(error)?.payload?.conflictingTerms ?? [];
}
