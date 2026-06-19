import { useState, useEffect } from "react";

type Status = "loading" | "idle" | "ready" | "error";

export function useScript(src: string): Status {
    const [status, setStatus] = useState<Status>(src ? "loading" : "idle");
    const existingScriptStatus =
        src && typeof document !== "undefined"
            ? ((document
                  .querySelector<HTMLScriptElement>(`script[src="${src}"]`)
                  ?.getAttribute("data-status") as Status | null) ?? "loading")
            : "idle";

    useEffect(() => {
        if (!src) {
            return;
        }

        // Check if the script already exists in the document
        let script = document.querySelector<HTMLScriptElement>(
            `script[src="${src}"]`,
        );

        if (!script) {
            // Create script element
            const createdScript = document.createElement("script");
            createdScript.src = src;
            createdScript.async = true;
            createdScript.setAttribute("data-status", "loading");
            document.body.appendChild(createdScript);
            script = createdScript;

            // Store status in data attribute for other hook instances
            const setScriptStatus = (event: Event): void => {
                createdScript.setAttribute(
                    "data-status",
                    event.type === "load" ? "ready" : "error",
                );
            };
            createdScript.addEventListener("load", setScriptStatus);
            createdScript.addEventListener("error", setScriptStatus);
        }

        // Event handler to update local component state
        const setStateFromEvent = (event: Event): void => {
            setStatus(event.type === "load" ? "ready" : "error");
        };

        script.addEventListener("load", setStateFromEvent);
        script.addEventListener("error", setStateFromEvent);

        // Cleanup logic when component unmounts
        return () => {
            if (script) {
                script.removeEventListener("load", setStateFromEvent);
                script.removeEventListener("error", setStateFromEvent);
                // Optional: Remove script from body if it's strictly single-use
                // document.body.removeChild(script);
            }
        };
    }, [src]);

    return src
        ? status === "loading"
            ? existingScriptStatus
            : status
        : "idle";
}
