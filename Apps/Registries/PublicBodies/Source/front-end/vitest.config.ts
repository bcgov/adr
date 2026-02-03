import { defineConfig } from "vitest/config";
import { playwright } from "@vitest/browser-playwright";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  test: {
    projects: [
      {
        test: {
          name: "unit",
          include: ["**/*.unit.{test,spec}.ts"],
          environment: "node",
        },
      },
      {
        test: {
          name: "browser",
          include: ["**/*.browser.{test,spec}.tsx"],
          browser: {
            enabled: true,
            provider: playwright(),
            // https://vitest.dev/config/browser/playwright
            instances: [{ browser: "chromium" }],
          },
        },
      },
    ],
  },
});
