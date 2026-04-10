import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

import { client as publicBodiesClient } from "@/api/generated-publicbodies/client.gen";
import { client as semanticsClient } from "@/api/generated-semantics/client.gen";
import App from "@/App.tsx";
import { API_URL, SEMANTICS_API_URL } from "@/constants.ts";
import "@/index.css";

publicBodiesClient.setConfig({ baseUrl: API_URL });
semanticsClient.setConfig({ baseUrl: SEMANTICS_API_URL });
import "@bcgov/bc-sans/css/BC_Sans.css";

const queryClient = new QueryClient();

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <QueryClientProvider client={queryClient}>
            <App />
        </QueryClientProvider>
    </StrictMode>,
);
