import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

import { client } from "./api/generated/client.gen";
import App from "./App.tsx";
import "./index.css";

client.setConfig({ baseUrl: import.meta.env.VITE_PUBLIC_BODIES_API_URL });
import "@bcgov/bc-sans/css/BC_Sans.css";

const queryClient = new QueryClient();

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </StrictMode>,
);
