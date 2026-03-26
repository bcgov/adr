export interface DictionaryField {
  fieldName: string;
  fieldDescription: string;
  schemaNameTableName: string;
  dataSource: string;
  dataType: string;
  keyRelationships: string;
  systemOfRecord: string;
  designatedAsRequired: string;
}

function extractField(
  name: string,
  field: Record<string, unknown>,
): DictionaryField | undefined {
  if (field["x-bc-field"] === undefined) {
    console.log("probably a ref...", name);
    return undefined;
  }
  const dictionaryField: DictionaryField = {
    fieldName: field["x-bc-field"] as string,
    fieldDescription: field["x-bc-desc"] as string,
    schemaNameTableName: field["x-bc-schema-table"] as string,
    dataSource: field["x-bc-source"] as string,
    dataType: field["x-bc-type"] as string,
    keyRelationships: field["x-bc-key"] as string,
    systemOfRecord: field["x-bc-sor"] as string,
    designatedAsRequired: field["x-bc-req"] as string,
  };
  return dictionaryField;
}

function extractProperties(
  properties: Record<string, unknown>,
): DictionaryField[] {
  const dictionaryFields = [];
  for (const [key, value] of Object.entries(properties)) {
    const field = extractField(key, value as Record<string, unknown>);
    if (field) {
      dictionaryFields.push(field);
    }
  }
  return dictionaryFields;
}

function extractSchema(
  schema: Record<string, unknown>,
): DictionaryField[] | undefined {
  const allOf = schema.allOf as Record<string, unknown>[] | undefined;
  if (allOf && allOf.length === 2 && allOf[1].properties) {
    const properties = allOf[1].properties as Record<string, unknown>;
    return extractProperties(properties);
  }
}

export function extractDictionary(openApiSpec: unknown): DictionaryField[] {
  const spec = openApiSpec as {
    components?: { schemas?: Record<string, unknown> };
  };
  const schemas = spec?.components?.schemas;
  let dictionary: DictionaryField[] = [];

  if (schemas === undefined) {
    return dictionary;
  }

  for (const value of Object.values(schemas)) {
    const fields = extractSchema(value as Record<string, unknown>);
    if (fields) {
      dictionary = dictionary.concat(fields);
    }
  }

  return dictionary;
}
