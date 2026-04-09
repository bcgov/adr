import "./PublicBodyCard.css";

interface PublicBodyCardProps {
    /** GUID */
    id: string | null;
    /** Display name */
    name: string | null;
    /** Acronym */
    acronym?: string | null | undefined;
    /** Public Body Type */
    publicBodyType?: string | null;
    /** (optional) Effective date */
    effectiveDate?: string | null;
    /** (optional) Retirement date */
    retirementDate?: string | null;
}

export default function PublicBodyCard({
    id,
    name,
    acronym,
    publicBodyType,
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

                {id && <div className="card-id mono">{id}</div>}
            </div>
        </div>
    );
}
