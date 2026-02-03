export interface PublicBody {
  /** System GUID for the public body */
  id: string;
  /** Legal name for the public body (ex: `Ministry of Citizens' Services` */
  name: string;
  /** Internal acronym for the public body (ex: `CITZ` for Ministry of Citizens' Services) */
  acronym: string;
  /** Date when the public body name took effect */
  effectiveDate?: string;
  /** Date when the public body name was retired */
  retirementDate?: string;
}
