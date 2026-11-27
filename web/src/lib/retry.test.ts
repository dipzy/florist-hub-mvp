import { describe, expect, it } from 'vitest';
import { ApiRequestError, isTransientFailure, retryAsync } from './retry';

describe('retryAsync', () => {
  it('repete falhas transitórias e fica com a resposta que sucede', async () => {
    let calls = 0;
    const value = await retryAsync(
      async () => {
        calls += 1;
        if (calls < 3) throw new TypeError('Failed to fetch');
        return 'ok';
      },
      { attempts: 3, delaysMs: [0, 0, 0] },
    );
    expect(value).toBe('ok');
    expect(calls).toBe(3);
  });

  it('não repete um 400', async () => {
    let calls = 0;
    await expect(
      retryAsync(
        async () => {
          calls += 1;
          throw new ApiRequestError('Loja não resolvida.', 400);
        },
        { attempts: 3, delaysMs: [0, 0, 0] },
      ),
    ).rejects.toThrow('Loja não resolvida.');
    expect(calls).toBe(1);
  });

  it('trata 502 e erros de rede como transitórios', () => {
    expect(isTransientFailure(new ApiRequestError('bad gateway', 502))).toBe(true);
    expect(isTransientFailure(new TypeError('Failed to fetch'))).toBe(true);
    expect(isTransientFailure(new ApiRequestError('nope', 400))).toBe(false);
    expect(isTransientFailure(new ApiRequestError('missing', 404))).toBe(false);
  });
});
