import type { GlossaryEntry } from "@/models/glossary";

interface GlossaryTermDetailsProps {
    entry: GlossaryEntry;
}

export default function GlossaryTermDetails({
    entry,
}: GlossaryTermDetailsProps) {
    return (
        <>
            <p>{entry.definition}</p>
            {entry.keywords && entry.keywords.length > 0 && (
                <div className="term-keywords">
                    {entry.keywords.map((keyword) => (
                        <span key={keyword}>{keyword}</span>
                    ))}
                </div>
            )}
            <dl className="term-details">
                {entry.name && (
                    <div>
                        <dt>Slug</dt>
                        <dd className="mono">{entry.name}</dd>
                    </div>
                )}
                {entry.id && (
                    <div>
                        <dt>UUID</dt>
                        <dd className="mono">{entry.id}</dd>
                    </div>
                )}
                {entry.scope && (
                    <div>
                        <dt>Scope</dt>
                        <dd>{entry.scope}</dd>
                    </div>
                )}
                {entry.scopeUrl && (
                    <div>
                        <dt>Scope reference</dt>
                        <dd>
                            <a
                                href={entry.scopeUrl}
                                target="_blank"
                                rel="noreferrer"
                            >
                                {entry.scopeUrl}
                            </a>
                        </dd>
                    </div>
                )}
                {entry.citations && (
                    <div>
                        <dt>Source</dt>
                        <dd>
                            <a
                                href={entry.citations}
                                target="_blank"
                                rel="noreferrer"
                            >
                                {entry.citations}
                            </a>
                        </dd>
                    </div>
                )}
                {entry.teamSource && (
                    <div>
                        <dt>Team source</dt>
                        <dd>{entry.teamSource}</dd>
                    </div>
                )}
                <div>
                    <dt>Definition status</dt>
                    <dd>
                        {entry.verifiedDefinitionFlag
                            ? "Verified"
                            : "Unverified"}
                    </dd>
                </div>
                <div>
                    <dt>DevHub status</dt>
                    <dd>
                        {entry.publishToDevHub ? "Published" : "Not published"}
                    </dd>
                </div>
            </dl>

            <section className="term-schema">
                <h4>Schema</h4>
                <dl className="term-details">
                    <div>
                        <dt>Type</dt>
                        <dd>{entry.schemaType || "string"}</dd>
                    </div>
                    {entry.example && (
                        <div>
                            <dt>Example</dt>
                            <dd>
                                <code>{entry.example}</code>
                            </dd>
                        </div>
                    )}
                    {entry.schemaConstraints &&
                        Object.keys(entry.schemaConstraints).length > 0 && (
                            <div>
                                <dt>Constraints</dt>
                                <dd>
                                    <pre>
                                        {JSON.stringify(
                                            entry.schemaConstraints,
                                            null,
                                            2,
                                        )}
                                    </pre>
                                </dd>
                            </div>
                        )}
                </dl>
            </section>
        </>
    );
}
