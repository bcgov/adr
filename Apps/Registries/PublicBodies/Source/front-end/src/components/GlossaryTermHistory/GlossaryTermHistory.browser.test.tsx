import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";

import GlossaryTermHistory from "./GlossaryTermHistory";

vi.mock("@/hooks/useGlossary", () => ({
    useGlossaryTermHistory: () => ({
        data: {
            payload: [
                {
                    status: "Published",
                    version: 2,
                    name: "example-term",
                    sourceType: "API",
                    sourceReference: "draft-2",
                    operation: "Upsert",
                    glossaryVersions: ["2.0.0"],
                    recordedUtc: "2026-09-14T12:00:00Z",
                    term: {
                        id: "22222222-2222-2222-2222-222222222222",
                        version: 2,
                        name: "example-term",
                        term: "Example Term",
                        definition: "The later definition.",
                        schemaType: "integer",
                        schemaConstraints: { minimum: 0 },
                        verifiedDefinitionFlag: true,
                        publishToDevHub: true,
                    },
                },
                {
                    status: "Invalid",
                    name: "example-term",
                    submittedId: "22222222-2222-2222-2222-222222222222",
                    sourceType: "API",
                    sourceReference: "draft-invalid",
                    operation: "Upsert",
                    breakingChange: false,
                    invalidReasons: ["Published Definition is required"],
                    sourcePayload: '{"definition":""}',
                    recordedUtc: "2026-09-13T12:00:00Z",
                },
                {
                    status: "Published",
                    version: 1,
                    name: "example-term",
                    sourceType: "CSV",
                    sourceReference: "glossary.1.csv",
                    operation: "Upsert",
                    glossaryVersions: ["1.0.0", "1.1.0"],
                    recordedUtc: "2026-09-12T12:00:00Z",
                    term: {
                        id: "22222222-2222-2222-2222-222222222222",
                        version: 1,
                        name: "example-term",
                        term: "Example Term",
                        definition: "The selected definition.",
                        schemaType: "string",
                        verifiedDefinitionFlag: true,
                        publishToDevHub: true,
                    },
                },
            ],
        },
        error: null,
        isFetching: false,
        isPending: false,
    }),
}));

test("shows all term versions and invalid submissions newest to oldest", async () => {
    const screen = await render(
        <GlossaryTermHistory
            glossaryVersion="1.0.0"
            term="example-term"
            selectedTermVersion={1}
        />,
    );

    await expect
        .element(screen.getByRole("heading", { name: "Example Term history" }))
        .toBeVisible();
    await expect
        .element(screen.getByText("Selected glossary version"))
        .toBeVisible();
    await expect
        .element(screen.getByText("Published Definition is required"))
        .toBeVisible();
    await userEvent.click(screen.getByText("Submitted data"));
    await expect.element(screen.getByText(/"definition":""/)).toBeVisible();
    await expect.element(screen.getByText(/"minimum": 0/)).toBeVisible();

    const cards = screen.getByTestId("history-entry").elements();
    expect(cards).toHaveLength(3);
    await expect.element(cards[0]).toHaveTextContent("Example Term (2)");
    await expect.element(cards[1]).toHaveTextContent("Invalid submission");
    await expect.element(cards[2]).toHaveTextContent("Example Term (1)");
});
