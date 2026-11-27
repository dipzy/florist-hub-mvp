export function hasWebGL() {
  if (typeof document === 'undefined') return false;
  if (new URLSearchParams(window.location.search).get('webgl') === '0') return false;
  try {
    const canvas = document.createElement('canvas');
    return Boolean(canvas.getContext('webgl2') || canvas.getContext('webgl'));
  } catch {
    return false;
  }
}

/** Telemóvel ou ecrã estreito: menos amostras e GPU em modo económico. */
export function prefersLowPower() {
  if (typeof window === 'undefined') return false;
  const coarse = window.matchMedia?.('(pointer: coarse)').matches ?? false;
  return coarse || window.innerWidth < 800;
}
