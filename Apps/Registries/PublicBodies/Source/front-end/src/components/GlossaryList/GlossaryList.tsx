import { TextField } from "@bcgov/design-system-react-components";
import { useEffect, useMemo, useState } from "react";
import { Link } from "wouter";

import Main from "../Main/Main";
import GlossaryTermDetails from "../GlossaryTermDetails/GlossaryTermDetails";
import useGlossary, {
    useGlossaryVersion,
    useGlossaryVersions,
} from "@/hooks/useGlossary";
import type { GlossaryEntry } from "@/models/glossary";
import { apiErrorMessage } from "@/utils/apiError";

import "../PublicBodyCard/PublicBodyCard.css";
import "../GlossaryEditing/GlossaryEditing.css";
import "./GlossaryList.css";

export default function GlossaryList() {
    const [selectedVersion, setSelectedVersion] = useState("");
    const currentGlossary = useGlossary();
    const selectedGlossary = useGlossaryVersion(selectedVersion || undefined);
    const { data, error, isFetching, isPending } = selectedVersion
        ? selectedGlossary
        : currentGlossary;
    const {
        data: versionsData,
        error: versionsError,
        isPending: versionsPending,
    } = useGlossaryVersions();
    const [search, setSearch] = useState("");
    const filteredEntries = useMemo(() => {
        const filter = search.trim().toLocaleLowerCase();
        return (data?.payload ?? []).filter((entry) =>
            [
                entry.term,
                entry.name,
                entry.definition,
                ...(entry.keywords ?? []),
            ]
                .filter(Boolean)
                .some((value) => value?.toLocaleLowerCase().includes(filter)),
        );
    }, [data?.payload, search]);

    // Scroll to the hash anchor after entries render
    useEffect(() => {
        // Early returns if no data
        if (isPending || error) return;

        const hash = window.location.hash.slice(1);

        if (!hash) return;

        const el = document.getElementById(decodeURIComponent(hash));

        el?.scrollIntoView({ behavior: "smooth", block: "start" });
    }, [isPending, error, data]);

    if (isPending) return "Loading...";

    if (error) return "An error has occurred: " + apiErrorMessage(error);

    const glossary = data?.glossary;
    const releases = versionsData?.payload ?? [];
    const currentRelease = releases.find((release) => release.isCurrent);

    return (
        <Main>
            <div className="glossary-heading">
                <h1>Glossary</h1>
                <Link href="/glossary/drafts" className="button-link secondary">
                    Manage drafts
                </Link>
            </div>
            {isFetching && <span>Fetching data...</span>}
            {glossary && (
                <p className="glossary-version">
                    {glossary.name} version {glossary.version}, published{" "}
                    <time dateTime={glossary.publishedAt}>
                        {glossary.publishedAt}
                    </time>
                </p>
            )}
            <div className="glossary-release-selector">
                <label>
                    Glossary version
                    <select
                        value={selectedVersion}
                        onChange={(event) =>
                            setSelectedVersion(event.target.value)
                        }
                        disabled={versionsPending || Boolean(versionsError)}
                    >
                        <option value="">
                            Latest
                            {currentRelease?.version
                                ? ` (${currentRelease.version})`
                                : ""}
                        </option>
                        {releases
                            .filter((release) => !release.isCurrent)
                            .map((release) => (
                                <option
                                    key={release.version}
                                    value={release.version ?? ""}
                                >
                                    {release.version} — {release.publishedAt}
                                </option>
                            ))}
                    </select>
                </label>
                {versionsError && (
                    <span role="alert">
                        Published version history could not be loaded.
                    </span>
                )}
            </div>
            <div className="glossary-search">
                <TextField
                    label="Search terms"
                    type="search"
                    value={search}
                    onChange={setSearch}
                />
                <span>{filteredEntries.length} terms</span>
            </div>
            <ul className="list-glossary">
                {filteredEntries.map((entry: GlossaryEntry) => (
                    <li
                        key={entry.id}
                        id={entry.id ?? undefined}
                        className="card"
                    >
                        <div className="card-body">
                            <div className="term-title-row">
                                <h3 className="card-name">
                                    {entry.term}
                                    {entry.version !== undefined && (
                                        <span className="term-version">
                                            {" "}
                                            ({entry.version})
                                        </span>
                                    )}
                                </h3>
                                <Link
                                    href={`/glossary/versions/${encodeURIComponent(
                                        glossary?.version ?? "",
                                    )}/terms/${encodeURIComponent(
                                        entry.name ?? "",
                                    )}/versions?selectedTermVersion=${entry.version}`}
                                >
                                    View History
                                </Link>
                            </div>
                            <GlossaryTermDetails entry={entry} />
                        </div>
                    </li>
                ))}
            </ul>
        </Main>
    );
}
