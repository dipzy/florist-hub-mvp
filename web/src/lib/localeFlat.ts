type JsonLeaf = string | number | boolean | null;
type JsonNode = JsonLeaf | { [key: string]: JsonNode } | JsonNode[];

export function flattenTranslationStrings(obj: JsonNode, prefix = ''): Record<string, string> {
  const out: Record<string, string> = {};
  if (obj === null || typeof obj !== 'object' || Array.isArray(obj)) return out;
  for (const [key, value] of Object.entries(obj)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (typeof value === 'string') out[path] = value;
    else if (value && typeof value === 'object' && !Array.isArray(value)) {
      Object.assign(out, flattenTranslationStrings(value, path));
    }
  }
  return out;
}
