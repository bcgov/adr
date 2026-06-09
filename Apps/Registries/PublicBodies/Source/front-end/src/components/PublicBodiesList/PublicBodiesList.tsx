import type { PublicBody } from "../../models/public-body";
import usePublicBodies from "../../hooks/usePublicBodies";
import usePublicBodyTypes from "../../hooks/usePublicBodyTypes";

import PublicBodyCard from "../PublicBodyCard/PublicBodyCard";

import "./PublicBodiesList.css";

// Only BC Government Ministries participate in the parent-child lineage graph.
const HISTORY_ENABLED_TYPE_NAME = "BC Government Ministry";

export default function PublicBodiesList() {
    const { data, error, isFetching, isPending } = usePublicBodies();
    const { data: types } = usePublicBodyTypes();

    if (isPending) return "Loading...";

    if (error) return "An error has occurred: " + error.message;

    const typesById = new Map(
        types?.map((t) => [t.publicBodyTypeId, t]) ?? [],
    );

    return (
        <div>
            {isFetching && <span>Fetching data...</span>}
            <ul className="list-public-bodies">
                {data.map((publicBody: PublicBody, index: number) => {
                    const type = typesById.get(publicBody.typeId ?? "");
                    return (
                        <li key={index}>
                            <PublicBodyCard
                                staticId={publicBody.staticId}
                                name={publicBody.name}
                                acronym={publicBody.acronym}
                                publicBodyType={type?.shortName ?? type?.name ?? null}
                                hasHistory={
                                    type?.name === HISTORY_ENABLED_TYPE_NAME
                                }
                                effectiveDate={
                                    publicBody.publicBodyEffectiveDate
                                }
                                retirementDate={
                                    publicBody.publicBodyRetiredDate
                                }
                                createdDate={
                                    publicBody.recordCreatedDatetime
                                }
                                updatedDate={
                                    publicBody.recordEndedDatetime
                                }
                            />
                        </li>
                    );
                })}
            </ul>
        </div>
    );
}
