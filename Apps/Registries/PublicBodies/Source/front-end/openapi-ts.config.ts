import { defineConfig } from "@hey-api/openapi-ts";

const apiUrl = process.env.VITE_PUBLIC_BODIES_API_URL;
if (!apiUrl) {
  throw new Error(
    "VITE_PUBLIC_BODIES_API_URL is not set. Copy .env.sample to .env and set the value.",
  );
}

export default defineConfig({
  input: `${apiUrl}/swagger/v1/swagger.json`,
  output: "src/api/generated",
  plugins: ["zod", "@tanstack/react-query"],
});
