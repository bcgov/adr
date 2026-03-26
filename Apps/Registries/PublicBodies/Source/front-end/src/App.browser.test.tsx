import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { expect, test, suite } from "vitest";
import { render } from "vitest-browser-react";

import App from "@/App";

suite("App contains semantic HTML elements", async () => {
  test("header", async () => {
    const screen = await render(
      <QueryClientProvider client={new QueryClient()}>
        <App />
      </QueryClientProvider>,
    );
    const header = screen.locator.getByRole("banner");
    expect(header).toBeInTheDocument();
  });

  test("main", async () => {
    const screen = await render(
      <QueryClientProvider client={new QueryClient()}>
        <App />
      </QueryClientProvider>,
    );
    const main = screen.locator.getByRole("main");
    expect(main).toBeInTheDocument();
  });

  test("footer", async () => {
    const screen = await render(
      <QueryClientProvider client={new QueryClient()}>
        <App />
      </QueryClientProvider>,
    );
    const footer = screen.locator.getByRole("contentinfo");
    expect(footer).toBeInTheDocument();
  });
});
