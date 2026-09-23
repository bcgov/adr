# Semantics Service

The Semantics Service publishes the Connected Services glossary and derives data-dictionary views from configured API OpenAPI documents.

## Run locally

Install the .NET 10 SDK and Node.js 24 (the frontend `.nvmrc` version). Run the following from the repository root in three separate terminals:

1. Start Public Bodies at <http://localhost:5000>:

   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project Apps/Registries/PublicBodies/Source/Api/PublicBodies.csproj --urls http://localhost:5000
   ```

2. Start Semantics at <http://localhost:5001>:

   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project Apps/Registries/Semantics/Source/Api/Semantics.csproj --urls http://localhost:5001
   ```

3. Install and start the frontend at <http://localhost:5173>:

   ```bash
   cd Apps/Registries/PublicBodies/Source/front-end
   npm ci
   npm run dev -- --port 5173 --strictPort
   ```

The frontend defaults to these local API URLs. If `.env.local` already overrides them, set `VITE_PUBLIC_BODIES_API_URL=http://localhost:5000` and `VITE_SEMANTICS_API_URL=http://localhost:5001`.

The Dictionary loads Public Bodies' OpenAPI document from <http://localhost:5000/swagger/v1/swagger.json>, so start Public Bodies before opening the Dictionary. If the Dictionary was loaded while Public Bodies was unavailable, restart Semantics to clear the cached result. The glossary itself does not depend on Public Bodies; verify it at <http://localhost:5001/v1/Glossary>.

Development configuration sets `GlossaryBaseUrl` to `http://localhost:5001/v1/Glossary`. Dictionary semantic links append `/id/{uuid}` to this base URL, resolving terms through the stable UUID lookup endpoint.

## Glossary releases

The glossary is versioned as a vocabulary, and each term also has a monotonically increasing integer `version`. `GET /v1/Glossary` is a moving alias for the current glossary release and preserves the existing `payload` array. Every glossary response also includes release metadata:

```json
{
  "glossary": {
    "id": "connected-services",
    "name": "Connected Services Semantics Glossary",
    "version": "1.0.0",
    "publishedAt": "2026-09-09",
    "isCurrent": true
  },
  "payload": [],
  "datetimeRequested": "2026-09-08T18:00:00Z"
}
```

The API also identifies the release with an `ETag`.

Available endpoints are:

| Endpoint | Description |
| --- | --- |
| `GET /v1/Glossary` | Current published glossary release |
| `GET /v1/Glossary/{term}` | Current term by human-readable slug |
| `GET /v1/Glossary/id/{id}` | Current term by stable UUID |
| `GET /v1/Glossary/versions` | All available glossary releases |
| `GET /v1/Glossary/versions/{version}` | An immutable glossary release |
| `GET /v1/Glossary/versions/{version}/terms/{term}` | A term from a release by slug |
| `GET /v1/Glossary/versions/{version}/terms/{term}/versions` | All versions and invalid submissions for a term identified through a release |
| `GET /v1/Glossary/terms/{term}/versions` | All persisted valid versions of a known term |
| `GET /v1/Glossary/terms/{term}/versions/{termVersion}` | A specific integer term version |
| `GET /v1/Glossary/schema` | Current release as reusable OpenAPI schema components |
| `GET /v1/Glossary/versions/{version}/schema` | Versioned OpenAPI schema components |
| `GET /v1/Glossary/terms/{term}/versions/{termVersion}/schema` | An immutable term revision as an OpenAPI Schema Object |
| `GET /v1/Glossary/markdown` | Current release as a Markdown table |
| `GET /v1/Glossary/markdown-list` | Current release as a Markdown list |
| `GET /v1/Glossary/versions/{version}/markdown` | Versioned Markdown table |
| `GET /v1/Glossary/versions/{version}/markdown-list` | Versioned Markdown list |
| `POST /v1/Glossary/drafts` | Start a draft from the current glossary release |
| `GET /v1/Glossary/drafts` | List draft metadata; optionally filter by `status=Draft`, `Published`, or `Stale` |
| `GET /v1/Glossary/drafts/{draftId}` | Preview a draft and its calculated version |
| `PUT /v1/Glossary/drafts/{draftId}/terms/{term}` | Add or replace a term in a draft |
| `DELETE /v1/Glossary/drafts/{draftId}/terms/{term}` | Remove a term from a draft |
| `POST /v1/Glossary/versions` | Publish a draft identified in the request body; `ignoreInvalid=true` permits unresolved rejected submissions |

The published glossary UI links each displayed term to its version history. The link is scoped to the selected glossary release, so a term deleted from the current release can still be found in an older release and its later versions and invalid submissions can be inspected. The UI link also carries the selected term version so the history page can identify it without adding presentation context to the API response.

The API version in `/v1` and the glossary version are independent. `/v1` versions the HTTP contract; the semantic version in the response versions the glossary content.

### Release validation

CSV snapshots listed under `imports` in `GlossaryReleases.json` are processed in declared order when the application starts. The catalog does not assign versions. Imported releases, computed glossary versions, term versions, and the source import ledger are persisted in SQLite. Previously processed source hashes are not reapplied after a restart.

A valid term must have a `Term`, lowercase slug in `Name`, and `Published Definition`. The slug is the definitive term identifier and maps to a separate internal GUID used for database relationships. `StaticId` is optional: when it is blank or the column is absent, an existing term reuses the UUID persisted for its slug and a new term receives a generated UUID. An explicitly supplied `StaticId` must be a GUID. Slugs and UUIDs must each be unique within an import. `StaticId` is versioned legacy metadata: correcting it creates a new term revision under the same slug and is a breaking glossary change, but does not change existing versioned lookups. Records are evaluated in source order: the first valid occurrence is accepted, while malformed records and later duplicate slugs or UUIDs are logged at critical severity and retained as invalid submissions with their reasons. CSV imports always continue past invalid records. Invalid submissions do not create term revisions, appear in a glossary release, or affect term or glossary versions; an invalid update to an existing term leaves its last valid revision in the imported snapshot. Every valid term remains in the release regardless of its publication flags. Collection, Markdown, and schema outputs include only terms with both `Verified Definition` and `Publish to DevHub` set to `Yes`; direct slug or UUID lookup and term history expose retained terms together with their flags. An import must leave at least one valid published, verified term; otherwise that import is rejected. If the database has no valid release after all declared imports are processed, startup fails.

The optional `Breaking Change` column accepts `Yes` or `No` and declares that a changed term is incompatible even when that cannot be inferred from its schema metadata. A changed term marked `Yes` makes the glossary release a major version. The flag is submission metadata: it is retained in the audit history but is not part of the published term or exported schema. It has no effect when the term content is unchanged.

The optional `Example` column supplies the `example` value in an exported term schema. When the column or value is absent, the generated schema omits `example`.

`Schema Type` controls the OpenAPI type and supports `string`, `number`, `integer`, and `boolean`; a missing or blank value defaults to `string`. An unsupported value produces a warning and falls back to `string` for that term. Examples for numeric and boolean types must contain a value valid for their schema type.

The optional `Schema Constraints` column contains a JSON object. Because it is stored in CSV, its double quotes are escaped by doubling them; for example, `"{""minimum"":0,""maximum"":100}"`. Supported constraints are `enum` for every type; `format` for strings and numeric types; `minLength`, `maxLength`, and `pattern` for strings; and `minimum`, `maximum`, `exclusiveMinimum`, `exclusiveMaximum`, and `multipleOf` for numbers and integers. The exclusive-bound values follow OpenAPI 3.0 and are booleans paired with `minimum` or `maximum`. Each constraint is checked for compatibility with the schema type and for a valid value. Invalid or unsupported entries produce a warning and are omitted without discarding other valid constraints for the term.

### Referencing glossary terms from OpenAPI

The glossary schema endpoints return an OpenAPI 3.0.3 document with one Schema Object per published, verified glossary term. Its `info.title` is the glossary name and its standard `info.version` is the glossary release version. Components are keyed by the human-readable term slug; slugs must be unique within the glossary and must not be reassigned to a different meaning. Use the versioned endpoint when publishing an OpenAPI document so that validation remains reproducible:

```yaml
components:
  schemas:
    Example:
      type: object
      properties:
        accessControl:
          $ref: "https://<semantics-api>/v1/Glossary/terms/access-control/versions/1/schema"
```

The standalone term endpoint returns a Schema Object rather than an OpenAPI document, so it has no `info` section. Its `title` is the term's display name and `x-bc-semantic-term-version` identifies the revision. Because the referenced resource is a complete Schema Object, it can be used anywhere OpenAPI permits a schema. For example, `access-control` version 1 is exported as:

```yaml
type: string
title: Access Control
description: Access control is a set of security measures that regulate who can access a system, services, data, or resource. Access control policies use authentication and authorization to verify users and enable access.
x-bc-semantic-urn: urn:bcgov:glossary:connected-services:access-control
x-bc-semantic-term-version: 1
x-bc-semantic-ref: a3ac060a-be57-4e70-af4c-bb7bd91bc7aa
```

`x-bc-semantic-urn` is the readable, stable identity of the term and does not change between revisions. `x-bc-semantic-term-version` identifies the exact revision, and `x-bc-semantic-ref` retains the existing UUID for compatibility. `x-bc-field` is not part of the glossary component because it describes the consuming API's property, not the glossary term.

Generated schema documents are cached in memory by glossary ID and version. Individual term-revision schemas are cached by glossary ID, term UUID, and term version. The current `/schema` alias is publicly cacheable for five minutes and must then be revalidated. Versioned glossary schema URLs and standalone term-revision schemas are immutable and publicly cacheable for one year.

Complete locally runnable examples are available for both the [`standalone term schema`](Examples/semantic-reference.openapi.yaml) and a [`versioned glossary component`](Examples/glossary-reference.openapi.yaml). The Spectral compatibility suite verifies both reference forms in parameters, headers, request and response bodies, and reusable schema components under OpenAPI 3.0 and 3.1.

### Version numbering

Glossary releases use computed semantic versions. The highest-impact net change determines the next version:

- Major: remove a published term, change the GUID-to-slug mapping, change to an incompatible or more restrictive schema type, tighten a schema constraint, or explicitly mark changed content as a `Breaking Change`.
- Minor: add a published term when there are no breaking changes.
- Patch: adjust existing term content or widen its schema when there are no additions or breaking changes.
- No content change: a valid CSV file still creates a patch release because each file is an explicit release boundary; an unchanged editing draft cannot be published.

The first valid import is `0.1.0`. A new term starts at version `1`. A term receives its next integer revision only when its final content differs from the current release; unchanged CSV rows reuse the existing revision. A manually published draft likewise creates one term revision for each changed term, so repeated edits do not create intermediate published revisions. Source filenames do not determine either version.

### Editing and publishing a draft

`POST /v1/Glossary/drafts` creates a persisted draft from the current release. The draft preview reports the semantic version that its current net changes would produce, with an `-alpha` prerelease suffix. For example, a new term based on `1.0.0` is previewed as `1.1.0-alpha`. An unchanged draft uses the minimum next candidate, such as `1.0.1-alpha`, but cannot be published until it contains a net change.

Term updates and deletions are staged in the draft. Each request is retained in the submission history. Invalid updates return `422`, include their reasons in the audit history, and leave the effective draft unchanged. The optional API `breakingChange` property has the same release-version effect as the CSV column. Omitting `id` reuses the UUID for an existing slug or generates one for a new slug.

Draft previews project the term version that publication would assign and include a `termChanges` collection. Each changed term is classified as `New`, `Bugfix`, `Breaking Change`, or `Deleted`; unchanged terms are omitted from that collection. Changed and deleted entries also include the term from the draft's base release so clients can restore the complete original term through the existing update operation. The editing UI displays these classifications, can hide unchanged terms, and provides the restore action while a draft remains editable.

Publishing is explicit: `POST /v1/Glossary/versions` accepts `draftId` and `ignoreInvalid` in the request body, validates the complete effective snapshot, creates one glossary release, and removes the `-alpha` suffix. Publication returns `422` while the latest submission for any term is invalid. Setting `ignoreInvalid` to `true` ignores those rejected submissions and publishes the last valid effective content; it does not bypass validation of that effective snapshot. Repeating publication for an already-published draft returns the same release without creating another version. The published draft remains available as an immutable audit record. Publishing a release immediately marks every other open draft based on the superseded release as `Stale`.

`POST /v1/Glossary/drafts/{draftId}/rebase` recovers a stale draft by applying its changes to the latest published glossary with a three-way merge. Changes made only in the stale draft are retained, changes made only in newer releases are incorporated, and identical changes are accepted. If the stale draft and a newer release changed the same term differently, the operation returns `409` with the conflicting term slugs and leaves the draft stale. A successful rebase updates the draft's base release and returns it to editable `Draft` status.

### Publishing a glossary release from CSV

Glossary snapshots are embedded CSV assets and are immutable after import. To import another snapshot:

1. Add a new, uniquely named CSV under `Source/Api/Assets`; do not replace or edit an imported asset.
2. Add the asset as an `EmbeddedResource` in `Source/Api/Semantics.csproj`.
3. Append its publication date and asset name to `Source/Api/Assets/GlossaryReleases.json`. Import order is significant.
4. Start the service so the migration and import pipeline persist the computed release and term versions.
5. Regenerate `Documentation/Devhub/Pages/glossary.md` from `GET /v1/Glossary/markdown-list`.
6. Run the Semantics API test suite.

The default local SQLite file is `semantics.db`. Override `ConnectionStrings__Semantics` with a writable persistent path in another environment. EF Core migrations under `Source/Api/Data/Migrations` are applied before imports.

## Tests

Run the Semantics API tests from the repository root:

```bash
dotnet test Apps/Registries/Semantics/Tests/Api/Semantics.Tests.csproj
```

Run the Spectral compatibility test after building the API:

```bash
dotnet build Apps/Registries/Semantics/Source/Api/Semantics.csproj
npm ci --prefix Apps/Registries/Semantics/Tests/Spectral
npm test --prefix Apps/Registries/Semantics/Tests/Spectral
```

The test starts the API on an available local port and verifies that Spectral resolves a property
reference from OpenAPI 3.0 and 3.1 consumer documents to the versioned JSON glossary schema.

Run the consuming frontend checks from `Apps/Registries/PublicBodies/Source/front-end`:

```bash
npm run lint
npm run test:browser-headless
npm run build
```
