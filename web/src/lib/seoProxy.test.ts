import { describe, expect, it } from 'vitest';
import { fallbackSeo, isCrawler, isSeoPath, joinApiUrl, shouldProxyHtml } from '../../functions/seoProxy.js';

describe('encaminhamento SEO', () => {
  it('reconhece robots e sitemap', () => {
    expect(isSeoPath('/robots.txt')).toBe(true);
    expect(isSeoPath('/sitemap.xml')).toBe(true);
    expect(isSeoPath('/portfolio')).toBe(false);
  });

  it('só pede HTML à API para crawlers em rotas da SPA', () => {
    expect(shouldProxyHtml('/portfolio/buque', 'Mozilla/5.0 (compatible; Googlebot/2.1)', 'GET')).toBe(true);
    expect(shouldProxyHtml('/portfolio/buque', 'Mozilla/5.0', 'GET')).toBe(false);
    expect(shouldProxyHtml('/assets/app.js', 'Googlebot', 'GET')).toBe(false);
    expect(shouldProxyHtml('/portfolio', 'Googlebot', 'POST')).toBe(false);
  });

  it('junta o URL da API sem barras a mais', () => {
    expect(joinApiUrl('https://api.exemplo.pt/', '/robots.txt', '')).toBe('https://api.exemplo.pt/robots.txt');
    expect(joinApiUrl('https://api.exemplo.pt', '/api/seo/documento', '?caminho=%2F')).toBe(
      'https://api.exemplo.pt/api/seo/documento?caminho=%2F',
    );
  });

  it('tem um robots mínimo quando a API não responde', () => {
    expect(fallbackSeo('/robots.txt').status).toBe(200);
    expect(fallbackSeo('/robots.txt').body).toContain('Allow: /');
    expect(fallbackSeo('/sitemap.xml').status).toBe(404);
  });

  it('reconhece o Googlebot', () => {
    expect(isCrawler('Googlebot')).toBe(true);
    expect(isCrawler('Mozilla/5.0')).toBe(false);
  });
});
