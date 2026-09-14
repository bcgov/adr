import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";

import GlossaryDraftList from "./GlossaryDraftList";

const createDraft = vi.fn();
const drafts = [
    {
        id: "draft-1",
        baseVersion: "0.2.0",
        status: "Draft",
        version: "0.3.0-alpha",
        changeType: "Minor",
        updatedUtc: "2026-09-12T18:30:00Z",
    },
    {
        id: "draft-2",
        baseVersion: "0.1.0",
        status: "Published",
        version: "0.2.0",
        changeType: "Minor",
        updatedUtc: "2026-09-10T17:00:00Z",
    },
    {
        id: "draft-3",
        baseVersion: "0.1.0",
        status: "Stale",
        version: "0.2.0-alpha",
        changeType: "Patch",
        updatedUtc: "2026-09-11T17:00:00Z",
    },
];

vi.mock("@/hooks/useGlossaryEditing", () => ({
    useGlossaryDrafts: (status?: string) => ({
        data: {
            payload: status
                ? drafts.filter((draft) => draft.status === status)
                : drafts,
        },
        error: null,
        isFetching: false,
        isPending: false,
    }),
    useCreateGlossaryDraft: () => ({
        mutate: createDraft,
        error: null,
        isPending: false,
    }),
}));

test("shows draft and stale records by default and published on request", async () => {
    const screen = await render(<GlossaryDraftList />);

    await expect
        .element(screen.getByRole("heading", { name: "Glossary drafts" }))
        .toBeVisible();
    await expect.element(screen.getByText("0.3.0-alpha")).toBeVisible();
    await expect.element(screen.getByLabelText("Status")).toHaveValue("Open");
    await expect.element(screen.getByText("0.2.0-alpha")).toBeVisible();
    await expect
        .element(screen.getByRole("row", { name: /draft-2 Published/ }))
        .not.toBeInTheDocument();
    await expect
        .element(screen.getByRole("link", { name: "Open" }))
        .toHaveAttribute("href", "/glossary/drafts/draft-1");

    await userEvent.selectOptions(screen.getByLabelText("Status"), "");

    await expect
        .element(screen.getByRole("row", { name: /draft-2 Published/ }))
        .toBeVisible();
    await expect
        .element(screen.getByRole("button", { name: "Create draft" }))
        .toBeVisible();

    await screen.getByRole("button", { name: "Create draft" }).click();
    expect(createDraft).toHaveBeenCalledWith(
        {},
        expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
});
