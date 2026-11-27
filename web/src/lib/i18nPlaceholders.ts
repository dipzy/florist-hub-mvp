const PLACEHOLDER_RE = /\{\{(\w+)\}\}/g;

export function translationPlaceholders(value: string): string[] {
  const names: string[] = [];
  let match = PLACEHOLDER_RE.exec(value);
  while (match) {
    names.push(match[1]);
    match = PLACEHOLDER_RE.exec(value);
  }
  PLACEHOLDER_RE.lastIndex = 0;
  return names;
}

export function hasUnresolvedPlaceholders(value: string): boolean {
  return /\{\{\w+\}\}/.test(value);
}

export function mockPlaceholderValues(names: string[]): Record<string, string> {
  return Object.fromEntries(names.map((name) => [name, `__${name}__`]));
}

export function assertNoRenderedPlaceholders(text: string, context?: string): void {
  if (!hasUnresolvedPlaceholders(text)) return;
  const snippet = text.replace(/\s+/g, ' ').slice(0, 120);
  throw new Error(
    context
      ? `Texto com placeholders i18n não resolvidos (${context}): ${snippet}`
      : `Texto com placeholders i18n não resolvidos: ${snippet}`,
  );
}
