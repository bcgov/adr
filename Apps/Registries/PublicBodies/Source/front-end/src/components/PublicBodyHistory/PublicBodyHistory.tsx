import { useEffect, useRef } from "react";
import { Link, useLocation } from "wouter";
import mermaid from "mermaid";
import usePublicBodyHistory from "../../hooks/usePublicBodyHistory";

import "./PublicBodyHistory.css";

// Edge styling: three independent visual dimensions per flag
const EDGE_COLOR_RENAMED = "#1a5a96";
const EDGE_COLOR_DEFAULT = "#6c757d";
const EDGE_WIDTH_MERGED = "4px";
const EDGE_WIDTH_DEFAULT = "1.5px";
const EDGE_DASH_SPLIT = "6 4";

// Highlight style for the queried node
const QUERIED_NODE_FILL = "#003366";
const QUERIED_NODE_TEXT = "#fff";

// Mermaid config
const NODE_ALIAS_PREFIX = "n";
const MERMAID_RENDER_ID_PREFIX = "mermaid-";
const FALLBACK_NODE_NAME = "Unknown";

// Mermaid's `click ... call` directive resolves callbacks via window[name].
const NAVIGATE_CALLBACK = "__publicBodyHistoryNavigate";

declare global {
    interface Window {
        __publicBodyHistoryNavigate?: (staticId: string) => void;
    }
}

// Safe to embed unescaped in a mermaid click directive. GUIDs match.
const SAFE_ID_PATTERN = /^[a-zA-Z0-9_-]+$/;

mermaid.initialize({
    startOnLoad: false,
    theme: "default",
    securityLevel: "loose",
    flowchart: {
        curve: "basis",
        nodeSpacing: 30,
        rankSpacing: 50,
    },
});

interface PublicBodyHistoryProps {
    id: string;
}

function buildMermaidDef(
    data: NonNullable<ReturnType<typeof usePublicBodyHistory>["data"]>,
): string {
    const bodies = data.publicBodies ?? [];
    const relationships = data.relationships ?? [];

    // NodeIds are the staticIds from the model
    const nodeIds = new Map<string, string>();
    bodies.forEach((b, i) => {
        if (b.staticId) {
            nodeIds.set(b.staticId, `${NODE_ALIAS_PREFIX}${i}`);
        }
    });

    const lines: string[] = ["graph TD"];

    // Define nodes; skip click directive for any unsafe-to-embed staticId
    for (const body of bodies) {
        const alias = nodeIds.get(body.staticId ?? "");
        if (!alias) continue;

        const name = (body.name ?? FALLBACK_NODE_NAME).replace(/"/g, "#quot;");
        lines.push(`    ${alias}["${name}"]`);

        if (body.staticId && SAFE_ID_PATTERN.test(body.staticId)) {
            lines.push(
                `    click ${alias} call ${NAVIGATE_CALLBACK}("${body.staticId}") "View history"`,
            );
        }
    }

    // Track per-edge flags for linkStyle below
    const edgeFlags: {
        renamed: boolean;
        merged: boolean;
        split: boolean;
    }[] = [];
    for (const rel of relationships) {
        const sourceAlias = nodeIds.get(rel.parentUniqueId ?? "");
        const targetAlias = nodeIds.get(rel.childUniqueId ?? "");
        if (!sourceAlias || !targetAlias) continue;

        edgeFlags.push({
            renamed: !!rel.wasRenamed,
            merged: !!rel.wasMerged,
            split: !!rel.wasSplit,
        });

        const date = rel.actionDatetime ?? "";

        if (date) {
            lines.push(`    ${sourceAlias} -->|"${date}"| ${targetAlias}`);
        } else {
            lines.push(`    ${sourceAlias} --> ${targetAlias}`);
        }
    }

    // Each flag maps to an independent visual dimension:
    //   renamed -> color, merged -> width, split -> dash
    edgeFlags.forEach((flags, i) => {
        const stroke = flags.renamed ? EDGE_COLOR_RENAMED : EDGE_COLOR_DEFAULT;
        const width = flags.merged ? EDGE_WIDTH_MERGED : EDGE_WIDTH_DEFAULT;
        const dash = flags.split ? `,stroke-dasharray:${EDGE_DASH_SPLIT}` : "";
        lines.push(
            `    linkStyle ${i} stroke:${stroke},stroke-width:${width}${dash}`,
        );
    });

    // Highlight the queried node
    const queriedAlias = nodeIds.get(data.publicBodyId ?? "");
    if (queriedAlias) {
        lines.push(
            `    style ${queriedAlias} fill:${QUERIED_NODE_FILL},color:${QUERIED_NODE_TEXT}`,
        );
    }

    return lines.join("\n");
}

export default function PublicBodyHistory({ id }: PublicBodyHistoryProps) {
    const { data, error, isPending } = usePublicBodyHistory(id);
    const diagramRef = useRef<HTMLDivElement>(null);
    const [, setLocation] = useLocation();

    useEffect(() => {
        // Local handler so cleanup only removes ours (StrictMode / races)
        const handler = (staticId: string) => {
            setLocation(`/public-bodies/${staticId}/history`);
        };
        window.__publicBodyHistoryNavigate = handler;
        return () => {
            if (window.__publicBodyHistoryNavigate === handler) {
                delete window.__publicBodyHistoryNavigate;
            }
        };
    }, [setLocation]);

    // `id` omitted: react-query keys `data` on `id`, so `data` is sufficient
    useEffect(() => {
        if (!data || !diagramRef.current) return;

        const el = diagramRef.current;
        const def = buildMermaidDef(data);
        const renderId = `${MERMAID_RENDER_ID_PREFIX}${Math.random().toString(36).slice(2)}`;
        let cancelled = false;

        el.innerHTML = "";

        mermaid
            .render(renderId, def)
            .then(({ svg, bindFunctions }) => {
                if (cancelled) return;
                el.innerHTML = svg;
                bindFunctions?.(el);
            })
            .catch((err) => {
                if (cancelled) return;
                console.error("Mermaid render failed:", err);
            });

        return () => {
            cancelled = true;
            // Clean up mermaid's orphan render element
            document.getElementById(renderId)?.remove();
        };
    }, [data]);

    if (isPending) return "Loading...";

    if (error) return "An error has occurred: " + error.message;

    if (!data) return "Public body not found.";

    const bodiesById = new Map(
        data.publicBodies?.map((b) => [b.staticId, b]) ?? [],
    );

    const queriedBody = bodiesById.get(data.publicBodyId ?? "");
    const hasRelationships = (data.relationships?.length ?? 0) > 0;

    return (
        <div className="history-page">
            <Link href="/public-bodies" className="history-back">
                Back to Public Bodies
            </Link>

            <div className="history-header">
                <h2>{queriedBody?.name ?? id}</h2>
                <span className="history-subtitle">Lineage History</span>
            </div>

            {hasRelationships ? (
                <div className="history-diagram-wrapper">
                    <div className="history-legend">
                        <span className="legend-item">
                            <svg
                                className="legend-swatch"
                                viewBox="0 0 32 8"
                                aria-hidden="true"
                            >
                                <line
                                    x1="2"
                                    y1="4"
                                    x2="30"
                                    y2="4"
                                    stroke={EDGE_COLOR_RENAMED}
                                    strokeWidth={EDGE_WIDTH_DEFAULT}
                                />
                            </svg>
                            Renamed (blue)
                        </span>
                        <span className="legend-item">
                            <svg
                                className="legend-swatch"
                                viewBox="0 0 32 8"
                                aria-hidden="true"
                            >
                                <line
                                    x1="2"
                                    y1="4"
                                    x2="30"
                                    y2="4"
                                    stroke={EDGE_COLOR_DEFAULT}
                                    strokeWidth={EDGE_WIDTH_MERGED}
                                />
                            </svg>
                            Merged (thick)
                        </span>
                        <span className="legend-item">
                            <svg
                                className="legend-swatch"
                                viewBox="0 0 32 8"
                                aria-hidden="true"
                            >
                                <line
                                    x1="2"
                                    y1="4"
                                    x2="30"
                                    y2="4"
                                    stroke={EDGE_COLOR_DEFAULT}
                                    strokeWidth={EDGE_WIDTH_DEFAULT}
                                    strokeDasharray={EDGE_DASH_SPLIT}
                                />
                            </svg>
                            Split (dashed)
                        </span>
                    </div>
                    <div className="history-diagram" ref={diagramRef} />
                </div>
            ) : (
                <div className="history-empty">
                    No lineage history found for this public body.
                </div>
            )}
        </div>
    );
}
