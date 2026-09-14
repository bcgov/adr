import {
    Button,
    InlineAlert,
    ProgressBar,
    TextArea,
    TextField,
} from "@bcgov/design-system-react-components";
import { useMemo, useState, type FormEvent } from "react";
import { Link, useLocation } from "wouter";

import Main from "../Main/Main";
import {
    useDeleteGlossaryTerm,
    useGlossaryDraft,
    useSaveGlossaryTerm,
} from "@/hooks/useGlossaryEditing";
import type { GlossaryEntry, GlossaryTermEdit } from "@/models/glossary";
import { apiErrorMessage } from "@/utils/apiError";

import "../GlossaryEditing/GlossaryEditing.css";

interface GlossaryTermEditorProps {
    draftId: string;
    isNew?: boolean;
    termSlug?: string;
}

interface TermFormState {
    id: string;
    slug: string;
    term: string;
    definition: string;
    example: string;
    schemaType: string;
    schemaConstraints: string;
    keywords: string;
    scope: string;
    scopeUrl: string;
    citations: string;
    teamSource: string;
    verifiedDefinitionFlag: boolean;
    publishToDevHub: boolean;
    breakingChange: boolean;
}

const emptyForm: TermFormState = {
    id: "",
    slug: "",
    term: "",
    definition: "",
    example: "",
    schemaType: "string",
    schemaConstraints: "",
    keywords: "",
    scope: "",
    scopeUrl: "",
    citations: "",
    teamSource: "",
    verifiedDefinitionFlag: false,
    publishToDevHub: false,
    breakingChange: false,
};

function formFromTerm(
    term?: GlossaryEntry,
    breakingChange = false,
): TermFormState {
    if (!term) return emptyForm;
    return {
        id: term.id ?? "",
        slug: term.name ?? "",
        term: term.term ?? "",
        definition: term.definition ?? "",
        example: term.example ?? "",
        schemaType: term.schemaType || "string",
        schemaConstraints:
            term.schemaConstraints &&
            Object.keys(term.schemaConstraints).length > 0
                ? JSON.stringify(term.schemaConstraints, null, 2)
                : "",
        keywords: term.keywords?.join(", ") ?? "",
        scope: term.scope ?? "",
        scopeUrl: term.scopeUrl ?? "",
        citations: term.citations ?? "",
        teamSource: term.teamSource ?? "",
        verifiedDefinitionFlag: term.verifiedDefinitionFlag ?? false,
        publishToDevHub: term.publishToDevHub ?? false,
        breakingChange,
    };
}

function parseExample(value: string, schemaType: string): unknown {
    if (!value) return undefined;
    if (schemaType === "number" || schemaType === "integer") {
        const parsed = Number(value);
        return Number.isNaN(parsed) ? value : parsed;
    }
    if (schemaType === "boolean") return value.toLocaleLowerCase() === "true";
    return value;
}

interface TermFormProps {
    draftId: string;
    initialTerm?: GlossaryEntry;
    initialBreakingChange?: boolean;
    isEditable: boolean;
    isNew: boolean;
}

function TermForm({
    draftId,
    initialTerm,
    initialBreakingChange,
    isEditable,
    isNew,
}: TermFormProps) {
    const [form, setForm] = useState<TermFormState>(() =>
        formFromTerm(initialTerm, initialBreakingChange),
    );
    const [clientError, setClientError] = useState<string>();
    const [isConfirmingDelete, setIsConfirmingDelete] = useState(false);
    const saveTerm = useSaveGlossaryTerm();
    const deleteTerm = useDeleteGlossaryTerm();
    const [, setLocation] = useLocation();
    const update = <K extends keyof TermFormState>(
        key: K,
        value: TermFormState[K],
    ) => setForm((current) => ({ ...current, [key]: value }));

    const schemaPreview = useMemo(() => {
        const constraints: Record<string, unknown> = (() => {
            try {
                return form.schemaConstraints.trim()
                    ? JSON.parse(form.schemaConstraints)
                    : {};
            } catch {
                return {};
            }
        })();
        return {
            type: form.schemaType,
            title: form.term || "Term title",
            description: form.definition || "Term definition",
            ...constraints,
            ...(form.example
                ? { example: parseExample(form.example, form.schemaType) }
                : {}),
        };
    }, [form]);

    const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setClientError(undefined);
        const slug = form.slug.trim();
        if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug)) {
            setClientError(
                "Slug must contain lowercase letters or numbers separated by single hyphens.",
            );
            return;
        }
        if (!form.term.trim() || !form.definition.trim()) {
            setClientError("Term and published definition are required.");
            return;
        }
        if (form.schemaConstraints.trim()) {
            try {
                const constraints = JSON.parse(form.schemaConstraints);
                if (
                    typeof constraints !== "object" ||
                    constraints === null ||
                    Array.isArray(constraints)
                ) {
                    throw new Error();
                }
            } catch {
                setClientError("Schema constraints must be a JSON object.");
                return;
            }
        }

        const body: GlossaryTermEdit = {
            id: form.id.trim() || undefined,
            term: form.term.trim(),
            definition: form.definition.trim(),
            example: form.example.trim(),
            schemaType: form.schemaType,
            schemaConstraints: form.schemaConstraints.trim(),
            keywords: form.keywords
                .split(",")
                .map((keyword) => keyword.trim())
                .filter(Boolean),
            scope: form.scope.trim(),
            scopeUrl: form.scopeUrl.trim(),
            citations: form.citations.trim(),
            teamSource: form.teamSource.trim(),
            verifiedDefinitionFlag: form.verifiedDefinitionFlag,
            publishToDevHub: form.publishToDevHub,
            breakingChange: form.breakingChange,
        };
        saveTerm.mutate(
            { path: { draftId, term: slug }, body },
            {
                onSuccess: () => setLocation(`/glossary/drafts/${draftId}`),
            },
        );
    };

    const handleDelete = () => {
        deleteTerm.mutate(
            { path: { draftId, term: form.slug } },
            {
                onSuccess: () => setLocation(`/glossary/drafts/${draftId}`),
            },
        );
    };

    return (
        <Main>
            <Link href={`/glossary/drafts/${draftId}`} className="back-link">
                ← Glossary draft
            </Link>
            <div className="glossary-page-heading">
                <div>
                    <h1>{isNew ? "Add glossary term" : form.term}</h1>
                    {!isNew && (
                        <p>
                            <span className="mono">{form.slug}</span> · Term
                            version {initialTerm?.version}
                        </p>
                    )}
                </div>
            </div>

            {!isEditable && (
                <InlineAlert
                    variant="info"
                    title="Read-only term"
                    description="Terms can only be changed while their glossary draft has Draft status."
                />
            )}
            {(clientError || saveTerm.error || deleteTerm.error) && (
                <InlineAlert
                    variant="danger"
                    title="The term could not be saved"
                    description={
                        clientError ??
                        apiErrorMessage(saveTerm.error ?? deleteTerm.error)
                    }
                />
            )}

            <form className="term-form" onSubmit={handleSubmit}>
                <section>
                    <h2>Identity</h2>
                    <div className="form-grid">
                        <TextField
                            label="Slug"
                            description="Lowercase words separated by hyphens. Existing slugs cannot be renamed."
                            value={form.slug}
                            onChange={(value) => update("slug", value)}
                            isReadOnly={!isNew || !isEditable}
                            isRequired
                        />
                        <TextField
                            label="Display term"
                            value={form.term}
                            onChange={(value) => update("term", value)}
                            isReadOnly={!isEditable}
                            isRequired
                        />
                    </div>
                    <details>
                        <summary>Advanced identity</summary>
                        <TextField
                            label="UUID"
                            description={
                                isNew
                                    ? "Optional. The API generates one when left blank."
                                    : "The stable compatibility identifier cannot be changed."
                            }
                            value={form.id}
                            onChange={(value) => update("id", value)}
                            isReadOnly={!isNew || !isEditable}
                        />
                    </details>
                </section>

                <section>
                    <h2>Definition</h2>
                    <TextArea
                        label="Published definition"
                        value={form.definition}
                        onChange={(value) => update("definition", value)}
                        isReadOnly={!isEditable}
                        isRequired
                    />
                    <TextField
                        label="Keywords"
                        description="Separate multiple keywords with commas."
                        value={form.keywords}
                        onChange={(value) => update("keywords", value)}
                        isReadOnly={!isEditable}
                    />
                    <TextArea
                        label="Scope"
                        value={form.scope}
                        onChange={(value) => update("scope", value)}
                        isReadOnly={!isEditable}
                    />
                    <div className="form-grid">
                        <TextField
                            label="Scope URL"
                            type="url"
                            value={form.scopeUrl}
                            onChange={(value) => update("scopeUrl", value)}
                            isReadOnly={!isEditable}
                        />
                        <TextField
                            label="Citation URL"
                            type="url"
                            value={form.citations}
                            onChange={(value) => update("citations", value)}
                            isReadOnly={!isEditable}
                        />
                    </div>
                    <TextField
                        label="Team source"
                        value={form.teamSource}
                        onChange={(value) => update("teamSource", value)}
                        isReadOnly={!isEditable}
                    />
                </section>

                <section>
                    <h2>OpenAPI schema</h2>
                    <div className="schema-editor-grid">
                        <div className="schema-fields">
                            <label>
                                Schema type
                                <select
                                    value={form.schemaType}
                                    onChange={(event) =>
                                        update("schemaType", event.target.value)
                                    }
                                    disabled={!isEditable}
                                >
                                    <option value="string">string</option>
                                    <option value="number">number</option>
                                    <option value="integer">integer</option>
                                    <option value="boolean">boolean</option>
                                </select>
                            </label>
                            <TextField
                                label="Example"
                                value={form.example}
                                onChange={(value) => update("example", value)}
                                isReadOnly={!isEditable}
                            />
                            <TextArea
                                label="Schema constraints"
                                description='Enter a JSON object, for example {"minimum":0,"maximum":100}.'
                                value={form.schemaConstraints}
                                onChange={(value) =>
                                    update("schemaConstraints", value)
                                }
                                isReadOnly={!isEditable}
                            />
                        </div>
                        <div>
                            <h3>Schema preview</h3>
                            <pre className="schema-preview">
                                {JSON.stringify(schemaPreview, null, 2)}
                            </pre>
                        </div>
                    </div>
                </section>

                <section>
                    <h2>Publication</h2>
                    <label className="checkbox-row">
                        <input
                            type="checkbox"
                            checked={form.verifiedDefinitionFlag}
                            onChange={(event) =>
                                update(
                                    "verifiedDefinitionFlag",
                                    event.target.checked,
                                )
                            }
                            disabled={!isEditable}
                        />
                        Definition is verified
                    </label>
                    <label className="checkbox-row">
                        <input
                            type="checkbox"
                            checked={form.publishToDevHub}
                            onChange={(event) =>
                                update("publishToDevHub", event.target.checked)
                            }
                            disabled={!isEditable}
                        />
                        Include this term in published glossary schemas
                    </label>
                    <label className="checkbox-row">
                        <input
                            type="checkbox"
                            checked={form.breakingChange}
                            onChange={(event) =>
                                update("breakingChange", event.target.checked)
                            }
                            disabled={!isEditable}
                        />
                        Treat this update as a breaking change
                    </label>
                    {form.breakingChange && (
                        <InlineAlert
                            variant="warning"
                            title="Major version change"
                            description="If the term content changes, publishing this draft will increment the glossary major version."
                        />
                    )}
                </section>

                {isEditable && (
                    <div className="form-actions">
                        <Button type="submit" isPending={saveTerm.isPending}>
                            Save term
                        </Button>
                        <Link
                            href={`/glossary/drafts/${draftId}`}
                            className="button-link secondary"
                        >
                            Cancel
                        </Link>
                        {!isNew && (
                            <Button
                                type="button"
                                variant="secondary"
                                danger
                                onPress={() => setIsConfirmingDelete(true)}
                            >
                                Delete term
                            </Button>
                        )}
                    </div>
                )}
            </form>

            {isConfirmingDelete && (
                <section
                    className="confirmation-panel destructive"
                    aria-label="Delete term"
                >
                    <h2>Remove {form.term} from this draft?</h2>
                    <p>
                        Publishing a draft with a removed term is a breaking
                        glossary change.
                    </p>
                    <div className="form-actions">
                        <Button
                            danger
                            onPress={handleDelete}
                            isPending={deleteTerm.isPending}
                        >
                            Confirm deletion
                        </Button>
                        <Button
                            variant="secondary"
                            onPress={() => setIsConfirmingDelete(false)}
                        >
                            Cancel
                        </Button>
                    </div>
                </section>
            )}
        </Main>
    );
}

export default function GlossaryTermEditor({
    draftId,
    isNew = false,
    termSlug,
}: GlossaryTermEditorProps) {
    const { data, error, isPending } = useGlossaryDraft(draftId);

    if (isPending) {
        return (
            <Main>
                <ProgressBar
                    isIndeterminate
                    size="medium"
                    valueLabel="Loading term..."
                />
            </Main>
        );
    }

    const preview = data?.payload;
    const term = preview?.terms?.find((entry) => entry.name === termSlug);
    const termChange = preview?.termChanges?.find(
        (change) => change.term.id === term?.id,
    );
    if (error || !preview || (!isNew && !term)) {
        return (
            <Main>
                <InlineAlert
                    variant="danger"
                    title="The term could not be loaded"
                    description={apiErrorMessage(
                        error,
                        "The requested term does not exist in this draft.",
                    )}
                />
                <p>
                    <Link href={`/glossary/drafts/${draftId}`}>
                        Return to draft
                    </Link>
                </p>
            </Main>
        );
    }

    return (
        <TermForm
            key={term?.id ?? "new-term"}
            draftId={draftId}
            initialTerm={term}
            initialBreakingChange={termChange?.breakingChange}
            isEditable={preview.draft.status === "Draft"}
            isNew={isNew}
        />
    );
}
