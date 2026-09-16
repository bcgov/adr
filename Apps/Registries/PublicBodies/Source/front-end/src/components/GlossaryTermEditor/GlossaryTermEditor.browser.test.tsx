import { expect, test, vi } from "vitest";
import { render } from "vitest-browser-react";

import GlossaryTermEditor from "./GlossaryTermEditor";

const saveTerm = vi.fn();

vi.mock("@/hooks/useGlossaryEditing", () => ({
    useGlossaryDraft: () => ({
        data: {
            payload: {
                draft: {
                    id: "draft-1",
                    baseVersion: "0.2.0",
                    status: "Draft",
                    version: "0.3.0-alpha",
                    changeType: "Patch",
                },
                terms: [
                    {
                        id: "a3ac060a-be57-4e70-af4c-bb7bd91bc7aa",
                        version: 1,
                        name: "access-control",
                        term: "Access Control",
                        definition: "Controls access to a resource.",
                        example: "role-based access",
                        schemaType: "string",
                        schemaConstraints: { minLength: 1 },
                        keywords: ["authorization", "security"],
                        verifiedDefinitionFlag: true,
                        publishToDevHub: true,
                    },
                ],
                termChanges: [
                    {
                        term: {
                            id: "a3ac060a-be57-4e70-af4c-bb7bd91bc7aa",
                        },
                        changeType: "Breaking Change",
                        breakingChange: true,
                    },
                ],
            },
        },
        error: null,
        isPending: false,
    }),
    useSaveGlossaryTerm: () => ({
        mutate: saveTerm,
        error: null,
        isPending: false,
    }),
    useDeleteGlossaryTerm: () => ({
        mutate: vi.fn(),
        error: null,
        isPending: false,
    }),
}));

test("edits all glossary term fields and previews its OpenAPI schema", async () => {
    const screen = await render(
        <GlossaryTermEditor draftId="draft-1" termSlug="access-control" />,
    );

    await expect
        .element(screen.getByRole("heading", { name: "Access Control" }))
        .toBeVisible();
    await expect
        .element(screen.getByLabelText("Slug"))
        .toHaveValue("access-control");
    await expect
        .element(screen.getByLabelText("Display term"))
        .toHaveValue("Access Control");
    await expect
        .element(screen.getByLabelText("Published definition"))
        .toHaveValue("Controls access to a resource.");
    await expect
        .element(screen.getByLabelText("Definition is verified"))
        .toBeChecked();
    await expect
        .element(
            screen.getByLabelText(
                "Include this term in published glossary schemas",
            ),
        )
        .toBeChecked();
    await expect
        .element(
            screen.getByText(/"title": "Access Control"[\s\S]*"minLength": 1/),
        )
        .toBeVisible();

    await expect
        .element(
            screen.getByLabelText("Treat this update as a breaking change"),
        )
        .toBeChecked();
    await expect
        .element(screen.getByText("Major version change"))
        .toBeVisible();
    await expect
        .element(screen.getByRole("button", { name: "Save term" }))
        .toBeVisible();

    await screen.getByLabelText("Display term").fill("Access Control Policy");
    await screen.getByRole("button", { name: "Save term" }).click();
    expect(saveTerm).toHaveBeenCalledWith(
        expect.objectContaining({
            path: { draftId: "draft-1", term: "access-control" },
            body: expect.objectContaining({
                term: "Access Control Policy",
                schemaType: "string",
                schemaConstraints: '{\n  "minLength": 1\n}',
                breakingChange: true,
            }),
        }),
        expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
});
