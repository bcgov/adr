# OpenAPI Typescript: Example Public Bodies

Our deployed API: https://publicbodies-c474bb-dev.apps.silver.devops.gov.bc.ca/publicbodies/ministries

We used Copilot to write a yaml specification for openAPI to consume.

## Usage

To generate a schema with OpenAPI typescript:

```bash
npx openapi-typescript ./Apps/Registries/PublicBodies/Source/Api/Specification/openapi.yaml -o ./Apps/Registries/PublicBodies/Source/Api/Specification/schema.d.ts
```

# Orval

A tool to convert OpenAPI Specification to Zod

## Usage

```bash
orval --input ./Apps/Registries/PublicBodies/Source/Api/Specification/openapi.yaml --output ./zod
```

Or you can create an `orval.config.ts` file and use the following:

```bash
npx orval
```