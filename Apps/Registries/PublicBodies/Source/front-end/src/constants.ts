declare global {
    interface Window {
        APP_CONFIG?: { PUBLIC_BODIES_API_URL?: string };
    }
}

// Resolution order:
//   - window.APP_CONFIG.PUBLIC_BODIES_API_URL written to /config.js at container startup by entrypoint.sh
//   - import.meta.env.VITE_PUBLIC_BODIES_API_URL (local-dev fallback read from .env by Vite at build time).
//   - if none set, default to http://localhost:5000 (tests).
export const API_URL: string =
    window.APP_CONFIG?.PUBLIC_BODIES_API_URL ||
    import.meta.env.VITE_PUBLIC_BODIES_API_URL ||
    "http://localhost:5000";
