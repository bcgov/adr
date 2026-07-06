declare global {
    interface Window {
        APP_CONFIG?: {
            PUBLIC_BODIES_API_URL?: string;
            SEMANTICS_API_URL?: string;
            DEVHUB_URL?: string;
        };
    }
}

// Resolution order:
//   - window.APP_CONFIG.* written to /config.js at container startup by entrypoint.sh
//   - import.meta.env.VITE_* (local-dev fallback read from .env by Vite at build time).
//   - if none set, default to localhost (tests).
export const API_URL: string =
    window.APP_CONFIG?.PUBLIC_BODIES_API_URL ||
    import.meta.env.VITE_PUBLIC_BODIES_API_URL ||
    "http://localhost:5000";

export const SEMANTICS_API_URL: string =
    window.APP_CONFIG?.SEMANTICS_API_URL ||
    import.meta.env.VITE_SEMANTICS_API_URL ||
    "http://localhost:5001";

export const DEVHUB_URL: string =
    window.APP_CONFIG?.DEVHUB_URL ||
    import.meta.env.VITE_DEVHUB_URL ||
    "https://dev.developer.gov.bc.ca";
