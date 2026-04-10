import { defineConfig } from "@hey-api/openapi-ts";

const publicBodiesUrl = process.env.VITE_PUBLIC_BODIES_API_URL;
const semanticsUrl = process.env.VITE_SEMANTICS_API_URL;

if (!publicBodiesUrl) {
    throw new Error(
        "VITE_PUBLIC_BODIES_API_URL is not set. Copy .env.sample to .env and set the value.",
    );
}

if (!semanticsUrl) {
    throw new Error(
        "VITE_SEMANTICS_API_URL is not set. Copy .env.sample to .env and set the value.",
    );
}

export default defineConfig([
    {
        input: `${publicBodiesUrl}/swagger/v1/swagger.json`,
        output: "src/api/generated-publicbodies",
        plugins: ["zod", "@tanstack/react-query"],
    },
    {
        input: `${semanticsUrl}/swagger/v1/swagger.json`,
        output: "src/api/generated-semantics",
        plugins: ["zod", "@tanstack/react-query"],
    },
]);
