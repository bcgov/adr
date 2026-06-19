import { createElement, useEffect } from "react";
import { useScript } from "../../hooks/useScript";

import { API_URL, CHEFS_FORM_SCRIPT_SRC_URL } from "@/constants";

interface ChefsFormInterface {
    /** UUID for the CHEFS form being loaded */
    formId: string;
    /** On-page `id` for the `<chefs-form-viewer>` element */
    id: string;
    /** API key for direct CHEFS authentication (skips the backend token fetch) */
    apiKey?: string;
}

type ChefsFormViewerElement = HTMLElement & {
    load: () => void;
};

export default function ChefsForm({
    formId,
    id,
    ...props
}: ChefsFormInterface) {
    const scriptStatus = useScript(CHEFS_FORM_SCRIPT_SRC_URL);
    const apiKey = props["apiKey"];

    useEffect(() => {
        if (scriptStatus !== "ready") {
            return;
        }

        let cancelled = false;

        async function init() {
            const el = document.getElementById(
                id,
            ) as ChefsFormViewerElement | null;
            if (!el) return;

            if (!apiKey) {
                const response = await fetch(
                    `${API_URL}/api/chefs-token/${encodeURIComponent(formId)}`,
                );
                if (!response.ok) {
                    console.error(
                        `Failed to fetch CHEFS auth token for form ${formId}`,
                    );
                    return;
                }

                const { authToken } = await response.json();
                if (cancelled) return;

                el.setAttribute("auth-token", authToken);
            }

            if (cancelled) return;

            el.setAttribute("form-id", formId);
            el.setAttribute("language", "en");
            el.setAttribute("isolate-styles", "");
            el.load();
        }

        void init();

        return () => {
            cancelled = true;
        };
    }, [formId, id, scriptStatus, apiKey]);

    if (scriptStatus === "error") {
        return null;
    }

    if (scriptStatus !== "ready") {
        return null;
    }

    return createElement("chefs-form-viewer", {
        id: id,
        "form-id": formId,
        ...props,
    });
}
