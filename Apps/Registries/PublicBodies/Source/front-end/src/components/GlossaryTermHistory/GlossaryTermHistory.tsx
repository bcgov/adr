import { Link } from "wouter";

import GlossaryTermDetails from "../GlossaryTermDetails/GlossaryTermDetails";
import Main from "../Main/Main";
import { useGlossaryTermHistory } from "@/hooks/useGlossary";
import type { GlossaryTermHistory as HistoryEntry } from "@/models/glossary";
import { apiErrorMessage } from "@/utils/apiError";

import "../PublicBodyCard/PublicBodyCard.css";
import "../GlossaryList/GlossaryList.css";
import "./GlossaryTermHistory.css";

interface GlossaryTermHistoryProps {
    glossaryVersion: string;
    term: string;
    selectedTermVersion: number;
}

export default function GlossaryTermHistory({
    glossaryVersion,
    term,
    selectedTermVersion,
}: GlossaryTermHistoryProps) {
    const { data, error, isFetching, isPending } = useGlossaryTermHistory(
        glossaryVersion,
        term,
    );

    if (isPending) return "Loading...";

    if (error) return "An error has occurred: " + apiErrorMessage(error);

    const history = data?.payload ?? [];
    const selected = history.find(
        (entry) => entry.version === selectedTermVersion,
    );
    const displayName = selected?.term?.term ?? term;

    return (
        <Main>
            <Link href="/glossary">Back to glossary</Link>
            <h1>{displayName} history</h1>
            <p>
                All versions and invalid submissions for the term selected from
                glossary version {glossaryVersion}, newest to oldest.
            </p>
            {isFetching && <span>Fetching data...</span>}
            <ul className="list-glossary term-history-list">
                {history.map((entry, index) => (
                    <HistoryItem
                        key={`${entry.recordedUtc}-${entry.version ?? "invalid"}-${index}`}
                        entry={entry}
                        selectedTermVersion={selectedTermVersion}
                    />
                ))}
            </ul>
        </Main>
    );
}

function HistoryItem({
    entry,
    selectedTermVersion,
}: {
    entry: HistoryEntry;
    selectedTermVersion: number;
}) {
    const recorded = entry.recordedUtc;

    return (
        <li className="card" data-testid="history-entry">
            <div className="card-body">
                {entry.status === "Published" && entry.term ? (
                    <>
                        <div className="term-title-row">
                            <h2 className="card-name">
                                {entry.term.term} ({entry.version})
                            </h2>
                            {entry.version === selectedTermVersion && (
                                <strong className="selected-version">
                                    Selected glossary version
                                </strong>
                            )}
                        </div>
                        <GlossaryTermDetails entry={entry.term} />
                        <dl className="term-details history-metadata">
                            <div>
                                <dt>Included in glossaries</dt>
                                <dd>
                                    {(entry.glossaryVersions ?? []).join(", ")}
                                </dd>
                            </div>
                            {recorded && (
                                <div>
                                    <dt>Recorded</dt>
                                    <dd>
                                        <time dateTime={recorded}>
                                            {recorded}
                                        </time>
                                    </dd>
                                </div>
                            )}
                        </dl>
                    </>
                ) : (
                    <>
                        <h2 className="card-name">Invalid submission</h2>
                        <dl className="term-details">
                            <div>
                                <dt>Submitted slug</dt>
                                <dd className="mono">{entry.name}</dd>
                            </div>
                            {entry.submittedId && (
                                <div>
                                    <dt>Submitted UUID</dt>
                                    <dd className="mono">
                                        {entry.submittedId}
                                    </dd>
                                </div>
                            )}
                            <div>
                                <dt>Operation</dt>
                                <dd>{entry.operation}</dd>
                            </div>
                            <div>
                                <dt>Source</dt>
                                <dd>
                                    {entry.sourceType}: {entry.sourceReference}
                                </dd>
                            </div>
                            <div>
                                <dt>Breaking change</dt>
                                <dd>{entry.breakingChange ? "Yes" : "No"}</dd>
                            </div>
                            {recorded && (
                                <div>
                                    <dt>Recorded</dt>
                                    <dd>
                                        <time dateTime={recorded}>
                                            {recorded}
                                        </time>
                                    </dd>
                                </div>
                            )}
                        </dl>
                        <section>
                            <h3>Validation errors</h3>
                            <ul>
                                {(entry.invalidReasons ?? []).map((reason) => (
                                    <li key={reason}>{reason}</li>
                                ))}
                            </ul>
                        </section>
                        {entry.sourcePayload && (
                            <details>
                                <summary>Submitted data</summary>
                                <pre className="history-source-payload">
                                    {entry.sourcePayload}
                                </pre>
                            </details>
                        )}
                    </>
                )}
            </div>
        </li>
    );
}
