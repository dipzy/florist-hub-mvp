import { useSyncExternalStore } from 'react';

function readResolvedTheme(): 'light' | 'dark' {
  if (typeof document === 'undefined') return 'light';
  return document.documentElement.getAttribute('data-fh-resolved-theme') === 'dark' ? 'dark' : 'light';
}

function subscribe(onChange: () => void) {
  const observer = new MutationObserver(onChange);
  observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-fh-resolved-theme'] });
  return () => observer.disconnect();
}

export function useStagePalette() {
  const mode = useSyncExternalStore(subscribe, readResolvedTheme, () => 'light' as const);
  const accent =
    typeof document === 'undefined'
      ? '#c45c74'
      : getComputedStyle(document.documentElement).getPropertyValue('--store-accent').trim() || '#c45c74';
  if (mode === 'dark') {
    return {
      background: '#161b18',
      ambient: '#fff4e8',
      ground: '#1c2420',
      hemisphereGround: '#24302a',
      accent,
    };
  }
  return {
    background: '#f7f3ec',
    ambient: '#fff8f0',
    ground: '#efe6da',
    hemisphereGround: '#c5b7a4',
    accent,
  };
}
