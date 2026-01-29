import { useQuery } from "@tanstack/react-query";

import { API_URL } from "../../constants";

import PublicBodyCard, {
  type PublicBodyCardProps,
} from "../PublicBodyCard/PublicBodyCard";

import "./PublicBodiesList.css";

export default function PublicBodiesList() {
  const { data, error, isFetching, isPending } = useQuery({
    queryKey: ["publicBodiesData"],
    queryFn: async () => {
      const response = await fetch(API_URL);
      return await response.json();
    },
  });

  if (isPending) return "Loading...";

  if (error) return "An error has occurred: " + error.message;

  return (
    <div>
      {isFetching && <span>Fetching data...</span>}
      <ul className="list-public-bodies">
        {data.payload.map((card: PublicBodyCardProps, index: number) => {
          return (
            <li key={`${card.id}-${index}`}>
              <PublicBodyCard
                id={card.id}
                name={card.name}
                acronym={card.acronym}
                effectiveDate={card.effectiveDate}
                retirementDate={card.effectiveDate}
              />
            </li>
          );
        })}
      </ul>
    </div>
  );
}
