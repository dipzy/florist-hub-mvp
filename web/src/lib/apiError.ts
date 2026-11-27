import i18n from '../i18n';

export function defaultApiErrorFallback(): string {
  return i18n.t('errors.generic');
}

export function defaultRequestDeniedFallback(): string {
  return i18n.t('errors.requestDenied');
}

/** Converte corpo de erro HTTP (JSON ou texto) numa mensagem legível. */
export function parseApiErrorBody(text: string, fallback?: string): string {
  const resolvedFallback = fallback ?? defaultApiErrorFallback();
  const trimmed = text.trim();
  if (!trimmed) return resolvedFallback;

  try {
    const parsed: unknown = JSON.parse(trimmed);
    if (parsed && typeof parsed === 'object') {
      const record = parsed as Record<string, unknown>;
      for (const key of ['message', 'title', 'detail', 'error'] as const) {
        const value = record[key];
        if (typeof value === 'string' && value.trim()) return value.trim();
      }
    }
  } catch {
    /* corpo não é JSON */
  }

  if (trimmed.startsWith('{') || trimmed.startsWith('[')) return resolvedFallback;
  if (trimmed.length > 280) return resolvedFallback;
  return trimmed;
}
