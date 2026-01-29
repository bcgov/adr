import type { PublicBody } from "../../types/schema";

import "./PublicBodyCard.css";

export type PublicBodyCardProps = PublicBody;

export default function PublicBodyCard({
  id,
  name,
  acronym,
  effectiveDate,
  retirementDate,
}: PublicBodyCardProps) {
  return (
    <div className="card">
      <div className="card-body">
        <div className="card-name">
          <span className="name">{name}</span>
          {acronym && (
            <>
              {" "}
              <span className="acronym">({acronym})</span>
            </>
          )}
        </div>

        <div className="card-dates">
          {effectiveDate && (
            <div className="card-date-effective">
              <span>
                Effective date:{" "}
                <time dateTime={effectiveDate}>{effectiveDate}</time>
              </span>
            </div>
          )}
          {retirementDate && (
            <div className="card-date-retirement">
              <span>
                Retirement date:{" "}
                <time dateTime={retirementDate}>{retirementDate}</time>
              </span>
            </div>
          )}
        </div>

        <div className="card-id mono">{id}</div>
      </div>
    </div>
  );
}
