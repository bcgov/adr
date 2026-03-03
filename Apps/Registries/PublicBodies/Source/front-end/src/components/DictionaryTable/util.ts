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

function extractField(name, field): DictionaryField | undefined {
  if (field["x-bc-field"] === undefined) {
    console.log("probably a ref...", name);
    return undefined;
  }
  const dictionaryField: DictionaryField = {
    fieldName: field["x-bc-field"],
    fieldDescription: field["x-bc-desc"],
    schemaNameTableName: field["x-bc-schema-table"],
    dataSource: field["x-bc-source"],
    dataType: field["x-bc-type"],
    keyRelationships: field["x-bc-key"],
    systemOfRecord: field["x-bc-sor"],
    designatedAsRequired: field["x-bc-req"],
  };
  return dictionaryField;
}

function extractProperties(properties): DictionaryField[] {
  const dictionaryFields = [];
  for (const [key, value] of Object.entries(properties)) {
    const field = extractField(key, value);
    if (field) {
      dictionaryFields.push(field);
    }
  }
  return dictionaryFields;
}

function extractSchema(name: string, schema): DictionaryField[] | undefined {
  if (schema.allOf && schema.allOf.length === 2 && schema.allOf[1].properties) {
    const properties = schema.allOf[1].properties;
    return extractProperties(properties);
  }
}

export function extractDictionary(openApiSpec): DictionaryField[] {
  const schemas: any[] = openApiSpec?.components?.schemas;
  let dictionary: DictionaryField[] = [];

  if (schemas === undefined) {
    return dictionary;
  }

  for (const [key, value] of Object.entries(schemas)) {
    const fields = extractSchema(key, value);
    if (fields) {
      dictionary = dictionary.concat(fields);
    }
  }

  return dictionary;
}
