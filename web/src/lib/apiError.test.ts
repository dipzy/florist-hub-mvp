import { describe, expect, it } from 'vitest';
import { parseApiErrorBody } from './apiError';

describe('parseApiErrorBody', () => {
  it('extrai message de JSON com unicode escapado', () => {
    const raw = '{"message":"Credenciais inv\\u00E1lidas."}';
    expect(parseApiErrorBody(raw)).toBe('Credenciais inválidas.');
  });

  it('devolve texto simples quando não é JSON', () => {
    expect(parseApiErrorBody('Serviço indisponível')).toBe('Serviço indisponível');
  });

  it('devolve fallback para JSON sem message', () => {
    expect(parseApiErrorBody('{"code":401}', 'Falha')).toBe('Falha');
  });

  it('devolve fallback para corpo vazio', () => {
    expect(parseApiErrorBody('   ', 'Vazio')).toBe('Vazio');
  });
});
