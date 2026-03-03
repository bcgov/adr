import type { PublicBody } from "../../models/public-body";
import usePublicBodies from "../../hooks/usePublicBodies";

import PublicBodyCard from "../PublicBodyCard/PublicBodyCard";

import "./PublicBodiesList.css";

export default function PublicBodiesList() {
  const { data, error, isFetching, isPending } = usePublicBodies();

  if (isPending) return "Loading...";

  if (error) return "An error has occurred: " + error.message;

  console.log(data);

  return (
    <div>
      {isFetching && <span>Fetching data...</span>}
      <ul className="list-public-bodies">
        {data.map((publicBody: PublicBody, index: number) => {
          return (
            <li key={index}>
              <PublicBodyCard
                id={publicBody.id}
                name={publicBody.name}
                acronym={publicBody.acronym}
                publicBodyType={publicBody.publicBodyType.name}
                effectiveDate={publicBody.effectiveDate}
                retirementDate={publicBody.effectiveDate}
              />
            </li>
          );
        })}
      </ul>
    </div>
  );
}
