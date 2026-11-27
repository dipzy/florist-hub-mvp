const CRAWLER =
  /googlebot|bingbot|duckduckbot|baiduspider|yandexbot|slurp|facebookexternalhit|twitterbot|linkedinbot|whatsapp|telegrambot|applebot|petalbot|embedly|pinterest|slackbot|discordbot|semrushbot|ahrefsbot|dotbot/i;

export function isSeoPath(pathname) {
  return pathname === '/robots.txt' || pathname === '/sitemap.xml';
}

export function isCrawler(userAgent) {
  return CRAWLER.test(userAgent || '');
}

export function shouldProxyHtml(pathname, userAgent, method) {
  if (method !== 'GET' && method !== 'HEAD') return false;
  if (!isCrawler(userAgent)) return false;
  if (!pathname || pathname.startsWith('/assets/') || pathname.startsWith('/api/')) return false;
  if (pathname.includes('.') && !pathname.endsWith('.html')) return false;
  return true;
}

export function joinApiUrl(apiBase, pathname, search) {
  const base = String(apiBase || '').replace(/\/+$/, '');
  return `${base}${pathname}${search || ''}`;
}

export function fallbackSeo(pathname) {
  if (pathname === '/robots.txt') {
    return {
      status: 200,
      contentType: 'text/plain; charset=utf-8',
      body: 'User-agent: *\nAllow: /\n',
    };
  }
  return {
    status: 404,
    contentType: 'text/plain; charset=utf-8',
    body: 'Sitemap indisponível.\n',
  };
}
