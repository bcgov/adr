import { beforeEach, expect, test, vi } from "vitest";
import { render } from "vitest-browser-react";

import GlossaryDraftEditor from "./GlossaryDraftEditor";

const publishDraft = vi.fn();
const rebaseDraft = vi.fn();
const saveTerm = vi.fn();
let draftStatus = "Draft";

vi.mock("@/hooks/useGlossaryEditing", () => ({
    useGlossaryDraft: () => ({
        data: {
            payload: {
                draft: {
                    id: "draft-1",
                    baseVersion: "0.2.0",
                    status: draftStatus,
                    version: "0.3.0-alpha",
                    changeType: "Minor",
                },
                terms: [
                    {
                        id: "a3ac060a-be57-4e70-af4c-bb7bd91bc7aa",
                        version: 2,
                        name: "access-control",
                        term: "Access Control",
                        definition: "Controls access to a resource.",
                        schemaType: "string",
                        verifiedDefinitionFlag: true,
                        publishToDevHub: true,
                    },
                    {
                        id: "unchanged-term-id",
                        version: 1,
                        name: "api-catalogue",
                        term: "API Catalogue",
                        definition: "A list of APIs.",
                        schemaType: "string",
                        verifiedDefinitionFlag: true,
                        publishToDevHub: true,
                    },
                ],
                termChanges: [
                    {
                        term: {
                            id: "a3ac060a-be57-4e70-af4c-bb7bd91bc7aa",
                            version: 2,
                            name: "access-control",
                            term: "Access Control",
                            definition: "Controls access to a resource.",
                            schemaType: "string",
                            verifiedDefinitionFlag: true,
                            publishToDevHub: true,
                        },
                        originalTerm: {
                            id: "a3ac060a-be57-4e70-af4c-bb7bd91bc7aa",
                            version: 1,
                            name: "access-control",
                            term: "Access Control",
                            definition: "Original access control definition.",
                            schemaType: "string",
                            keywords: [],
                            verifiedDefinitionFlag: true,
                            publishToDevHub: true,
                        },
                        changeType: "Bugfix",
                    },
                    {
                        term: {
                            id: "deleted-term-id",
                            version: 1,
                            name: "applicant",
                            term: "Applicant",
                            definition: "A person who applies.",
                            schemaType: "string",
                            verifiedDefinitionFlag: true,
                            publishToDevHub: true,
                        },
                        originalTerm: {
                            id: "deleted-term-id",
                            version: 1,
                            name: "applicant",
                            term: "Applicant",
                            definition: "A person who applies.",
                            schemaType: "string",
                            keywords: [],
                            verifiedDefinitionFlag: true,
                            publishToDevHub: true,
                        },
                        changeType: "Deleted",
                    },
                ],
            },
        },
        error: null,
        isFetching: false,
        isPending: false,
    }),
    usePublishGlossaryDraft: () => ({
        mutate: publishDraft,
        isPending: false,
    }),
    useRebaseGlossaryDraft: () => ({
        mutate: rebaseDraft,
        error: null,
        isPending: false,
    }),
    useSaveGlossaryTerm: () => ({
        mutate: saveTerm,
        error: null,
        isPending: false,
    }),
}));

beforeEach(() => {
    draftStatus = "Draft";
    publishDraft.mockClear();
    rebaseDraft.mockClear();
    saveTerm.mockClear();
});

test("reviews terms and confirms publication from an editable draft", async () => {
    const screen = await render(<GlossaryDraftEditor draftId="draft-1" />);

    await expect
        .element(
            screen.getByRole("heading", {
                name: "Glossary draft 0.3.0-alpha",
            }),
        )
        .toBeVisible();
    await expect.element(screen.getByText("Access Control")).toBeVisible();
    await expect
        .element(
            screen
                .getByRole("row", { name: /Access Control/ })
                .getByRole("link", { name: "Edit" }),
        )
        .toHaveAttribute(
            "href",
            "/glossary/drafts/draft-1/terms/access-control",
        );
    await expect.element(screen.getByText("Bugfix")).toBeVisible();
    await expect.element(screen.getByText("Deleted")).toBeVisible();
    await expect
        .element(screen.getByRole("cell", { name: "2", exact: true }))
        .toHaveClass("changed-term-version");

    await screen.getByLabelText("Show changed terms only").click();
    await expect.element(screen.getByText("2 of 3 terms")).toBeVisible();
    await expect
        .element(screen.getByText("API Catalogue"))
        .not.toBeInTheDocument();
    await screen.getByRole("button", { name: "Restore original" }).click();
    expect(saveTerm).toHaveBeenCalledWith({
        path: { draftId: "draft-1", term: "access-control" },
        body: {
            id: "a3ac060a-be57-4e70-af4c-bb7bd91bc7aa",
            term: "Access Control",
            definition: "Original access control definition.",
            example: "",
            schemaType: "string",
            schemaConstraints: "",
            keywords: [],
            scope: "",
            scopeUrl: "",
            citations: "",
            teamSource: "",
            verifiedDefinitionFlag: true,
            publishToDevHub: true,
            breakingChange: false,
        },
    });
    await screen.getByRole("button", { name: "Restore", exact: true }).click();
    expect(saveTerm).toHaveBeenLastCalledWith({
        path: { draftId: "draft-1", term: "applicant" },
        body: {
            id: "deleted-term-id",
            term: "Applicant",
            definition: "A person who applies.",
            example: "",
            schemaType: "string",
            schemaConstraints: "",
            keywords: [],
            scope: "",
            scopeUrl: "",
            citations: "",
            teamSource: "",
            verifiedDefinitionFlag: true,
            publishToDevHub: true,
            breakingChange: false,
        },
    });
    await expect
        .element(screen.getByRole("link", { name: "Add term" }))
        .toHaveAttribute("href", "/glossary/drafts/draft-1/terms/new");

    await screen.getByRole("button", { name: "Publish" }).click();
    await expect
        .element(
            screen.getByRole("heading", {
                name: "Publish glossary 0.3.0?",
            }),
        )
        .toBeVisible();
    await expect
        .element(screen.getByRole("button", { name: "Confirm publish" }))
        .toBeVisible();

    await screen.getByRole("button", { name: "Confirm publish" }).click();
    expect(publishDraft).toHaveBeenCalledWith(
        { body: { draftId: "draft-1", ignoreInvalid: false } },
        expect.objectContaining({
            onSuccess: expect.any(Function),
            onError: expect.any(Function),
        }),
    );
});

test("offers to apply a stale draft's changes to the latest glossary", async () => {
    draftStatus = "Stale";
    const screen = await render(<GlossaryDraftEditor draftId="draft-1" />);

    await expect.element(screen.getByText("This draft is stale")).toBeVisible();
    await expect
        .element(screen.getByRole("link", { name: "Add term" }))
        .not.toBeInTheDocument();
    await screen
        .getByRole("button", { name: "Apply changes to latest" })
        .click();

    expect(rebaseDraft).toHaveBeenCalledWith(
        { path: { draftId: "draft-1" } },
        { onSuccess: expect.any(Function) },
    );
});
