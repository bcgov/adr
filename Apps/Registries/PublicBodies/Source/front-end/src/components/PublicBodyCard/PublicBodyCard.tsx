import { Link } from "wouter";

import "./PublicBodyCard.css";

interface PublicBodyCardProps {
    /** Static ID for API lookups */
    staticId?: string | null;
    /** Display name */
    name: string | null;
    /** Acronym */
    acronym?: string | null | undefined;
    /** Public Body Type */
    publicBodyType?: string | null;
    /** Whether lineage history is available for this public body */
    hasHistory?: boolean;
    /** (optional) Effective date */
    effectiveDate?: string | null;
    /** (optional) Retirement date */
    retirementDate?: string | null;
}

export default function PublicBodyCard({
    staticId,
    name,
    acronym,
    publicBodyType,
    hasHistory,
    effectiveDate,
    retirementDate,
}: PublicBodyCardProps) {
    return (
        <div className="card">
            <div className="card-body">
                {name && (
                    <div className="card-name">
                        <span className="name">{name}</span>
                        {acronym && (
                            <>
                                {" "}
                                <span className="acronym">({acronym})</span>
                            </>
                        )}
                    </div>
                )}
                {publicBodyType && (
                    <div className="card-publicbody-type">
                        <span className="card-publicbody-type">
                            {publicBodyType}
                        </span>
                    </div>
                )}

                {(effectiveDate || retirementDate) && (
                    <div className="card-dates">
                        {effectiveDate && (
                            <div className="card-date-effective">
                                <span>
                                    Effective date:{" "}
                                    <time dateTime={effectiveDate}>
                                        {effectiveDate}
                                    </time>
                                </span>
                            </div>
                        )}
                        {retirementDate && (
                            <div className="card-date-retirement">
                                <span>
                                    Retirement date:{" "}
                                    <time dateTime={retirementDate}>
                                        {retirementDate}
                                    </time>
                                </span>
                            </div>
                        )}
                    </div>
                )}

                {staticId && <div className="card-id mono">{staticId}</div>}

                {staticId && hasHistory && (
                    <Link
                        href={`/public-bodies/${staticId}/history`}
                        className="card-history-link"
                    >
                        View History
                    </Link>
                )}
            </div>
        </div>
    );
}
