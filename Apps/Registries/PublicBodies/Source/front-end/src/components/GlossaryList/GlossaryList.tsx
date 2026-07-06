import { useEffect } from "react";

import Main from "../Main/Main";
import useGlossary from "@/hooks/useGlossary";
import type { GlossaryEntry } from "@/models/glossary";

import "../PublicBodyCard/PublicBodyCard.css";

export default function GlossaryList() {
    const { data, error, isFetching, isPending } = useGlossary();

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

    if (error) return "An error has occurred: " + error.message;

    return (
        <Main>
            {isFetching && <span>Fetching data...</span>}
            <ul className="list-glossary">
                {(data ?? []).map((entry: GlossaryEntry) => (
                    <li
                        key={entry.id}
                        id={entry.id ?? undefined}
                        className="card"
                    >
                        <div className="card-body">
                            <h3 className="card-name">{entry.term}</h3>
                            {entry.keywords && entry.keywords.length > 0 && (
                                <div>
                                    {entry.keywords.map((keyword) => (
                                        <span key={keyword}>{keyword}</span>
                                    ))}
                                </div>
                            )}
                            <p>{entry.definition}</p>
                            {entry.citations && (
                                <p className="card-id">
                                    Source:{" "}
                                    <a
                                        href={entry.citations}
                                        target="_blank"
                                        rel="noreferrer"
                                    >
                                        {entry.citations}
                                    </a>
                                </p>
                            )}
                        </div>
                    </li>
                ))}
            </ul>
        </Main>
    );
}
