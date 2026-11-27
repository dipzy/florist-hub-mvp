import { fallbackSeo, isSeoPath, joinApiUrl, shouldProxyHtml } from './seoProxy.js';

export async function onRequest(context) {
  const url = new URL(context.request.url);
  const method = context.request.method;
  const seo = isSeoPath(url.pathname);
  const html = shouldProxyHtml(url.pathname, context.request.headers.get('user-agent') || '', method);
  if (!seo && !html) return context.next();

  const api = String(context.env.API_BASE_URL || context.env.VITE_API_BASE_URL || '').trim();
  if (!api) return seo ? textResponse(fallbackSeo(url.pathname)) : context.next();

  const target = seo
    ? joinApiUrl(api, url.pathname, url.search)
    : joinApiUrl(api, '/api/seo/documento', `?caminho=${encodeURIComponent(url.pathname)}`);

  const headers = new Headers();
  headers.set('X-Forwarded-Host', url.host);
  headers.set('X-Forwarded-Proto', url.protocol.replace(':', ''));
  headers.set('Accept', seo ? '*/*' : 'text/html');
  const userAgent = context.request.headers.get('user-agent');
  if (userAgent) headers.set('User-Agent', userAgent);

  try {
    const response = await fetch(target, { method: 'GET', headers, redirect: 'manual' });
    if (response.ok) return response;
  } catch {
    // A API no plano gratuito pode estar a acordar. O visitante fica com a SPA.
  }

  return seo ? textResponse(fallbackSeo(url.pathname)) : context.next();
}

function textResponse(result) {
  return new Response(result.body, {
    status: result.status,
    headers: {
      'content-type': result.contentType,
      'cache-control': 'public, max-age=300',
    },
  });
}
