import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";

import GlossaryList from "./GlossaryList";

vi.mock("@/hooks/useGlossary", () => ({
    default: () => ({
        data: {
            glossary: {
                id: "connected-services",
                name: "Connected Services Glossary",
                version: "0.2.0",
                publishedAt: "2026-09-09",
                isCurrent: true,
            },
            payload: [
                {
                    id: "a3ac060a-be57-4e70-af4c-bb7bd91bc7aa",
                    version: 1,
                    name: "access-control",
                    term: "Access Control",
                    definition: "Controls access to a resource.",
                    example: "role-based access control",
                    schemaType: "string",
                    schemaConstraints: { minLength: 1 },
                    keywords: ["authorization", "security"],
                    scope: "Connected Services",
                    scopeUrl: "https://example.gov.bc.ca/scope",
                    citations: "https://example.gov.bc.ca/source",
                    teamSource: "Architecture",
                    verifiedDefinitionFlag: true,
                    publishToDevHub: true,
                },
            ],
            datetimeRequested: "2026-09-08T12:00:00Z",
        },
        error: null,
        isFetching: false,
        isPending: false,
    }),
    useGlossaryVersion: () => ({
        data: {
            glossary: {
                id: "connected-services",
                name: "Connected Services Glossary",
                version: "0.1.0",
                publishedAt: "2026-08-15",
                isCurrent: false,
            },
            payload: [
                {
                    id: "old-term-id",
                    version: 1,
                    name: "api-catalogue",
                    term: "API Catalogue",
                    definition: "An earlier glossary definition.",
                },
            ],
            datetimeRequested: "2026-09-08T12:00:00Z",
        },
        error: null,
        isFetching: false,
        isPending: false,
    }),
    useGlossaryVersions: () => ({
        data: {
            payload: [
                {
                    id: "connected-services",
                    name: "Connected Services Glossary",
                    version: "0.2.0",
                    publishedAt: "2026-09-09",
                    isCurrent: true,
                },
                {
                    id: "connected-services",
                    name: "Connected Services Glossary",
                    version: "0.1.0",
                    publishedAt: "2026-08-15",
                    isCurrent: false,
                },
            ],
        },
        error: null,
        isPending: false,
    }),
}));

test("defaults to the latest glossary and can select a published version", async () => {
    const screen = await render(<GlossaryList />);

    await expect
        .element(screen.getByLabelText("Glossary version"))
        .toHaveValue("");
    await expect
        .element(
            screen.getByText(/Connected Services Glossary version 0\.2\.0/),
        )
        .toBeVisible();
    await expect
        .element(screen.getByRole("heading", { name: "Access Control (1)" }))
        .toBeVisible();
    await expect.element(screen.getByText("access-control")).toBeVisible();
    await expect
        .element(screen.getByText("a3ac060a-be57-4e70-af4c-bb7bd91bc7aa"))
        .toBeVisible();
    await expect.element(screen.getByText("authorization")).toBeVisible();
    await expect
        .element(screen.getByText("Connected Services", { exact: true }))
        .toBeVisible();
    await expect
        .element(
            screen.getByRole("link", {
                name: "https://example.gov.bc.ca/scope",
            }),
        )
        .toHaveAttribute("href", "https://example.gov.bc.ca/scope");
    await expect.element(screen.getByText("Architecture")).toBeVisible();
    await expect
        .element(screen.getByText("role-based access control"))
        .toBeVisible();
    await expect.element(screen.getByText(/"minLength": 1/)).toBeVisible();
    await expect
        .element(screen.getByRole("link", { name: "View History" }))
        .toHaveAttribute(
            "href",
            "/glossary/versions/0.2.0/terms/access-control/versions?selectedTermVersion=1",
        );

    await userEvent.selectOptions(
        screen.getByLabelText("Glossary version"),
        "0.1.0",
    );

    await expect
        .element(
            screen.getByText(/Connected Services Glossary version 0\.1\.0/),
        )
        .toBeVisible();
    await expect
        .element(screen.getByRole("heading", { name: "API Catalogue (1)" }))
        .toBeVisible();
    await expect
        .element(screen.getByRole("link", { name: "View History" }))
        .toHaveAttribute(
            "href",
            "/glossary/versions/0.1.0/terms/api-catalogue/versions?selectedTermVersion=1",
        );
});

test("links from the published glossary to draft management", async () => {
    const screen = await render(<GlossaryList />);

    await expect
        .element(screen.getByRole("link", { name: "Manage drafts" }))
        .toHaveAttribute("href", "/glossary/drafts");
});
