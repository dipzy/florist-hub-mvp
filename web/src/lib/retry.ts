export class ApiRequestError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiRequestError';
    this.status = status;
  }
}

const TRANSIENT_STATUS = new Set([408, 429, 500, 502, 503, 504]);

/** Falhas de rede, timeout ou cold start. Um 4xx de negócio não se repete. */
export function isTransientFailure(error: unknown): boolean {
  if (error instanceof ApiRequestError) return TRANSIENT_STATUS.has(error.status);
  if (error instanceof DOMException && error.name === 'AbortError') return true;
  if (error instanceof TypeError) return true;
  return false;
}

function delay(ms: number) {
  if (ms <= 0) return Promise.resolve();
  return new Promise<void>((resolve) => {
    setTimeout(resolve, ms);
  });
}

export async function retryAsync<T>(
  task: () => Promise<T>,
  options?: { attempts?: number; delaysMs?: number[]; retryOn?: (error: unknown) => boolean },
): Promise<T> {
  const attempts = options?.attempts ?? 3;
  const delays = options?.delaysMs ?? (import.meta.env.MODE === 'test' ? [0, 0, 0] : [400, 1200, 2800]);
  const retryOn = options?.retryOn ?? isTransientFailure;
  let last: unknown;
  for (let attempt = 0; attempt < attempts; attempt += 1) {
    try {
      return await task();
    } catch (error) {
      last = error;
      if (attempt === attempts - 1 || !retryOn(error)) throw error;
      await delay(delays[attempt] ?? 0);
    }
  }
  throw last;
}
