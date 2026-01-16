# OpenAPI Typescript: Example Public Bodies

Our deployed API: https://publicbodies-c474bb-dev.apps.silver.devops.gov.bc.ca/publicbodies/ministries

We used Copilot to write a yaml specification for openAPI to consume.

## Installation

```bash
npm i -D openapi-typescript typescript
```

After installation edit the `tsconfig.json` file to have the following:

```json
{
  "compilerOptions": {
    "module": "ESNext", // or "NodeNext"
    "moduleResolution": "Bundler" // or "NodeNext"
  }
}
```

## Usage

To generate a schema with OpenAPI typescript:

```bash
npx openapi-typescript ./Apps/Registries/PublicBodies/Source/Api/Specification/openapi.yaml -o ./Apps/Registries/PublicBodies/Source/Api/Specification/schema.d.ts
```

# Orval

A tool to convert OpenAPI Specification to Zod.

## Installation

Since we want to use Zod for type safety we also install it:

```bash
npm i orval -D
npm install zod
```

After installation edit the `tsconfig.json` file to have the following:

```json
{
  // ...
  "compilerOptions": {
    // ...
    "strict": true
  }
}
```

## Usage

```bash
orval --input ./Apps/Registries/PublicBodies/Source/Api/Specification/openapi.yaml --output ./zod
```

Or you can create an `orval.config.ts` file and use the following:

```bash
npx orval
```