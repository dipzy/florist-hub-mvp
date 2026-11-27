/** Gerador determinístico (Mulberry32). A mesma semente produz a mesma sequência. */
export function mulberry32(seed: number) {
  let a = seed >>> 0;
  return function next() {
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/** Inteiro positivo que cabe num `int` do backend. */
export function randomSeed(): number {
  const n = Math.floor(Math.random() * 0x7fffffff);
  return n === 0 ? 1 : n;
}
