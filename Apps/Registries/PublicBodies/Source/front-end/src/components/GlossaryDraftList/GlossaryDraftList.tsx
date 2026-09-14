import {
    Button,
    InlineAlert,
    ProgressBar,
} from "@bcgov/design-system-react-components";
import { useState } from "react";
import { Link, useLocation } from "wouter";

import Main from "../Main/Main";
import {
    useCreateGlossaryDraft,
    useGlossaryDrafts,
} from "@/hooks/useGlossaryEditing";
import type { GlossaryDraft } from "@/models/glossary";
import { apiErrorMessage } from "@/utils/apiError";

import "../GlossaryEditing/GlossaryEditing.css";

const statuses = [
    { value: "Open", label: "Draft and Stale" },
    { value: "", label: "All statuses" },
    { value: "Draft", label: "Draft" },
    { value: "Stale", label: "Stale" },
    { value: "Published", label: "Published" },
];

function formatDate(value?: string): string {
    return value
        ? new Intl.DateTimeFormat(undefined, {
              dateStyle: "medium",
              timeStyle: "short",
          }).format(new Date(value))
        : "—";
}

export default function GlossaryDraftList() {
    const [status, setStatus] = useState("Open");
    const { data, error, isFetching, isPending } = useGlossaryDrafts(
        status && status !== "Open" ? status : undefined,
    );
    const createDraft = useCreateGlossaryDraft();
    const [, setLocation] = useLocation();
    const drafts =
        data?.payload?.filter(
            (draft) => status !== "Open" || draft.status !== "Published",
        ) ?? [];

    const handleCreate = () => {
        createDraft.mutate(
            {},
            {
                onSuccess: (response) => {
                    const id = response.payload.draft.id;
                    if (id) setLocation(`/glossary/drafts/${id}`);
                },
            },
        );
    };

    return (
        <Main>
            <div className="glossary-page-heading">
                <div>
                    <Link href="/glossary" className="back-link">
                        ← Published glossary
                    </Link>
                    <h1>Glossary drafts</h1>
                    <p>
                        Stage and review glossary changes before publishing a
                        new version.
                    </p>
                </div>
                <Button
                    onPress={handleCreate}
                    isPending={createDraft.isPending}
                >
                    Create draft
                </Button>
            </div>

            {createDraft.error && (
                <InlineAlert
                    variant="danger"
                    title="The draft could not be created"
                    description={apiErrorMessage(createDraft.error)}
                />
            )}

            <div className="glossary-toolbar">
                <label>
                    Status
                    <select
                        value={status}
                        onChange={(event) => setStatus(event.target.value)}
                    >
                        {statuses.map((option) => (
                            <option
                                key={option.value || "all"}
                                value={option.value}
                            >
                                {option.label}
                            </option>
                        ))}
                    </select>
                </label>
                {isFetching && !isPending && <span>Refreshing drafts…</span>}
            </div>

            {isPending && (
                <ProgressBar
                    isIndeterminate
                    size="medium"
                    valueLabel="Loading drafts..."
                />
            )}

            {error && (
                <InlineAlert
                    variant="danger"
                    title="Drafts could not be loaded"
                    description={apiErrorMessage(error)}
                />
            )}

            {!isPending && !error && drafts.length === 0 && (
                <p className="empty-state">No matching glossary drafts.</p>
            )}

            {drafts.length > 0 && (
                <div className="glossary-table-wrapper">
                    <table className="glossary-editing-table">
                        <thead>
                            <tr>
                                <th>Version</th>
                                <th>Status</th>
                                <th>Change</th>
                                <th>Based on</th>
                                <th>Updated</th>
                                <th>Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            {drafts.map((draft: GlossaryDraft) => (
                                <tr key={draft.id}>
                                    <td>
                                        <strong>{draft.version}</strong>
                                        <span className="draft-id mono">
                                            {draft.id}
                                        </span>
                                    </td>
                                    <td>
                                        <span
                                            className={`status-badge status-${draft.status?.toLowerCase()}`}
                                        >
                                            {draft.status}
                                        </span>
                                    </td>
                                    <td>{draft.changeType}</td>
                                    <td>{draft.baseVersion}</td>
                                    <td>{formatDate(draft.updatedUtc)}</td>
                                    <td>
                                        <Link
                                            href={`/glossary/drafts/${draft.id}`}
                                        >
                                            {draft.status === "Draft"
                                                ? "Open"
                                                : "View"}
                                        </Link>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </Main>
    );
}
