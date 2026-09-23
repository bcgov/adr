import {
    Button,
    InlineAlert,
    ProgressBar,
    TextField,
} from "@bcgov/design-system-react-components";
import { useMemo, useState } from "react";
import { Link } from "wouter";

import Main from "../Main/Main";
import {
    useGlossaryDraft,
    usePublishGlossaryDraft,
    useRebaseGlossaryDraft,
    useSaveGlossaryTerm,
} from "@/hooks/useGlossaryEditing";
import type { GlossaryEntry, GlossaryTermEdit } from "@/models/glossary";
import {
    apiErrorMessage,
    conflictingTermsFromError,
    invalidTermsFromError,
} from "@/utils/apiError";

import "../GlossaryEditing/GlossaryEditing.css";

interface GlossaryDraftEditorProps {
    draftId: string;
}

function editFromTerm(term: GlossaryEntry): GlossaryTermEdit {
    return {
        id: term.id ?? undefined,
        term: term.term,
        definition: term.definition ?? "",
        example: term.example ?? "",
        schemaType: term.schemaType || "string",
        schemaConstraints:
            term.schemaConstraints &&
            Object.keys(term.schemaConstraints).length > 0
                ? JSON.stringify(term.schemaConstraints)
                : "",
        keywords: term.keywords ?? [],
        scope: term.scope ?? "",
        scopeUrl: term.scopeUrl ?? "",
        citations: term.citations ?? "",
        teamSource: term.teamSource ?? "",
        verifiedDefinitionFlag: term.verifiedDefinitionFlag ?? false,
        publishToDevHub: term.publishToDevHub ?? false,
        breakingChange: false,
    };
}

export default function GlossaryDraftEditor({
    draftId,
}: GlossaryDraftEditorProps) {
    const { data, error, isFetching, isPending } = useGlossaryDraft(draftId);
    const publishDraft = usePublishGlossaryDraft();
    const rebaseDraft = useRebaseGlossaryDraft();
    const restoreTerm = useSaveGlossaryTerm();
    const [search, setSearch] = useState("");
    const [showChangedOnly, setShowChangedOnly] = useState(false);
    const [isConfirmingPublish, setIsConfirmingPublish] = useState(false);
    const [ignoreInvalid, setIgnoreInvalid] = useState(false);
    const [publishError, setPublishError] = useState<unknown>();
    const [publishedVersion, setPublishedVersion] = useState<string>();
    const [rebasedVersion, setRebasedVersion] = useState<string>();

    const preview = data?.payload;
    const draft = preview?.draft;
    const isEditable = draft?.status === "Draft";
    const invalidTerms = invalidTermsFromError(publishError);
    const rebaseConflicts = conflictingTermsFromError(rebaseDraft.error);
    const allTerms = useMemo(() => {
        const changes = preview?.termChanges ?? [];
        const changesById = new Map(
            changes.map((change) => [
                change.term.id ?? change.term.name,
                change,
            ]),
        );
        const rows = (preview?.terms ?? []).map((term) => {
            const change = changesById.get(term.id ?? term.name);
            return {
                term,
                changeType: change?.changeType ?? "",
                originalTerm: change?.originalTerm,
            };
        });
        rows.push(
            ...changes
                .filter((change) => change.changeType === "Deleted")
                .map((change) => ({
                    term: change.term,
                    changeType: change.changeType ?? "",
                    originalTerm: change.originalTerm,
                })),
        );
        return rows.sort((left, right) =>
            (left.term.name ?? "").localeCompare(right.term.name ?? ""),
        );
    }, [preview?.termChanges, preview?.terms]);
    const terms = useMemo(() => {
        const filter = search.trim().toLocaleLowerCase();
        return allTerms.filter(
            ({ term, changeType }) =>
                (!showChangedOnly || Boolean(changeType)) &&
                [term.term, term.name, term.definition]
                    .filter(Boolean)
                    .some((value) =>
                        value?.toLocaleLowerCase().includes(filter),
                    ),
        );
    }, [allTerms, search, showChangedOnly]);

    const handlePublish = () => {
        setPublishError(undefined);
        publishDraft.mutate(
            { body: { draftId, ignoreInvalid } },
            {
                onSuccess: (response) => {
                    setPublishedVersion(
                        response.payload.release?.version ??
                            draft?.version ??
                            "",
                    );
                    setIsConfirmingPublish(false);
                },
                onError: setPublishError,
            },
        );
    };

    const handleRestore = (term: GlossaryEntry) => {
        restoreTerm.mutate({
            path: { draftId, term: term.name ?? "" },
            body: editFromTerm(term),
        });
    };

    const handleRebase = () => {
        rebaseDraft.mutate(
            { path: { draftId } },
            {
                onSuccess: (response) =>
                    setRebasedVersion(
                        response.payload.preview?.draft.baseVersion ?? "latest",
                    ),
            },
        );
    };

    if (isPending) {
        return (
            <Main>
                <ProgressBar
                    isIndeterminate
                    size="medium"
                    valueLabel="Loading draft..."
                />
            </Main>
        );
    }

    if (error || !preview || !draft) {
        return (
            <Main>
                <InlineAlert
                    variant="danger"
                    title="The draft could not be loaded"
                    description={apiErrorMessage(error)}
                />
                <p>
                    <Link href="/glossary/drafts">Return to drafts</Link>
                </p>
            </Main>
        );
    }

    return (
        <Main>
            <Link href="/glossary/drafts" className="back-link">
                ← Glossary drafts
            </Link>

            <div className="glossary-page-heading">
                <div>
                    <div className="heading-with-status">
                        <h1>Glossary draft {draft.version}</h1>
                        <span
                            className={`status-badge status-${draft.status?.toLowerCase()}`}
                        >
                            {draft.status}
                        </span>
                    </div>
                    <p>
                        Based on {draft.baseVersion} · {draft.changeType} change
                        {isFetching ? " · Refreshing…" : ""}
                    </p>
                </div>
                {isEditable && (
                    <div className="heading-actions">
                        <Link
                            href={`/glossary/drafts/${draftId}/terms/new`}
                            className="button-link secondary"
                        >
                            Add term
                        </Link>
                        <Button onPress={() => setIsConfirmingPublish(true)}>
                            Publish
                        </Button>
                    </div>
                )}
            </div>

            {publishedVersion && (
                <InlineAlert
                    variant="success"
                    title={`Glossary ${publishedVersion} was published`}
                    description="This draft is now an immutable publication record."
                />
            )}

            {rebasedVersion && (
                <InlineAlert
                    variant="success"
                    title={`Draft recovered onto glossary ${rebasedVersion}`}
                    description="Its changes are now based on the latest published glossary and can be edited or published."
                />
            )}

            {draft.status === "Stale" && (
                <div className="stale-draft-recovery">
                    <InlineAlert
                        variant="warning"
                        title="This draft is stale"
                        description="A newer glossary was published after this draft was created. Apply this draft's changes to the latest glossary before editing or publishing it."
                    />
                    <Button
                        variant="secondary"
                        onPress={handleRebase}
                        isPending={rebaseDraft.isPending}
                    >
                        Apply changes to latest
                    </Button>
                </div>
            )}

            {rebaseDraft.error && (
                <InlineAlert
                    variant="danger"
                    title="The stale draft could not be recovered"
                    description={
                        rebaseConflicts.length > 0
                            ? `Changed differently in both versions: ${rebaseConflicts.join(", ")}.`
                            : apiErrorMessage(rebaseDraft.error)
                    }
                />
            )}

            {restoreTerm.error && (
                <InlineAlert
                    variant="danger"
                    title="The term could not be restored"
                    description={apiErrorMessage(restoreTerm.error)}
                />
            )}

            {isConfirmingPublish && (
                <section
                    className="confirmation-panel"
                    aria-label="Publish draft"
                >
                    <h2>
                        Publish glossary {draft.version?.replace("-alpha", "")}?
                    </h2>
                    <p>
                        The draft contains {preview.terms?.length ?? 0} terms
                        and represents a{" "}
                        {draft.changeType?.toLocaleLowerCase() ?? "pending"}{" "}
                        change from {draft.baseVersion}.
                    </p>
                    {publishError !== undefined && publishError !== null && (
                        <InlineAlert
                            variant="danger"
                            title="The draft could not be published"
                            description={apiErrorMessage(publishError)}
                        >
                            {invalidTerms.length > 0 && (
                                <ul>
                                    {invalidTerms.map((term) => (
                                        <li key={term.name ?? "invalid-term"}>
                                            <strong>{term.name}</strong>:{" "}
                                            {term.invalidReasons?.join(" ")}
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </InlineAlert>
                    )}
                    {invalidTerms.length > 0 && (
                        <label className="checkbox-row">
                            <input
                                type="checkbox"
                                checked={ignoreInvalid}
                                onChange={(event) =>
                                    setIgnoreInvalid(event.target.checked)
                                }
                            />
                            Ignore rejected invalid submissions and publish the
                            last valid content
                        </label>
                    )}
                    <div className="form-actions">
                        <Button
                            onPress={handlePublish}
                            isPending={publishDraft.isPending}
                        >
                            Confirm publish
                        </Button>
                        <Button
                            variant="secondary"
                            onPress={() => {
                                setIsConfirmingPublish(false);
                                setPublishError(undefined);
                            }}
                        >
                            Cancel
                        </Button>
                    </div>
                </section>
            )}

            <div className="glossary-toolbar">
                <TextField
                    label="Search terms"
                    type="search"
                    value={search}
                    onChange={setSearch}
                />
                <label className="checkbox-row changed-only-filter">
                    <input
                        type="checkbox"
                        checked={showChangedOnly}
                        onChange={(event) =>
                            setShowChangedOnly(event.target.checked)
                        }
                    />
                    Show changed terms only
                </label>
                <span>
                    {terms.length}
                    {terms.length !== allTerms.length
                        ? ` of ${allTerms.length}`
                        : ""}{" "}
                    terms
                </span>
            </div>

            <div className="glossary-table-wrapper">
                <table className="glossary-editing-table">
                    <thead>
                        <tr>
                            <th>Term</th>
                            <th>Slug</th>
                            <th>Change</th>
                            <th>Version</th>
                            <th>Schema type</th>
                            <th>Publication</th>
                            <th>Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        {terms.map(({ term, changeType, originalTerm }) => (
                            <tr key={`${term.id ?? term.name}-${changeType}`}>
                                <td>
                                    <strong>{term.term}</strong>
                                </td>
                                <td className="mono">{term.name}</td>
                                <td>{changeType}</td>
                                <td
                                    className={
                                        changeType && changeType !== "Deleted"
                                            ? "changed-term-version"
                                            : undefined
                                    }
                                >
                                    {changeType === "Deleted"
                                        ? "—"
                                        : (term.version ?? "—")}
                                </td>
                                <td>{term.schemaType || "string"}</td>
                                <td>
                                    {changeType === "Deleted" ? (
                                        "Removed from draft"
                                    ) : (
                                        <>
                                            {term.verifiedDefinitionFlag
                                                ? "Verified"
                                                : "Unverified"}
                                            {term.publishToDevHub
                                                ? " · Published"
                                                : " · Hidden"}
                                        </>
                                    )}
                                </td>
                                <td>
                                    <div className="table-actions">
                                        {changeType !== "Deleted" && (
                                            <Link
                                                href={`/glossary/drafts/${draftId}/terms/${encodeURIComponent(term.name ?? "")}`}
                                            >
                                                {isEditable ? "Edit" : "View"}
                                            </Link>
                                        )}
                                        {isEditable && originalTerm && (
                                            <button
                                                type="button"
                                                className="table-action-button"
                                                disabled={restoreTerm.isPending}
                                                onClick={() =>
                                                    handleRestore(originalTerm)
                                                }
                                            >
                                                {changeType === "Deleted"
                                                    ? "Restore"
                                                    : "Restore original"}
                                            </button>
                                        )}
                                        {changeType === "Deleted" &&
                                            (!isEditable || !originalTerm) &&
                                            "—"}
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </Main>
    );
}
