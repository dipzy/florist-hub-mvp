import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const outDir = path.join(__dirname, '../src/i18n/locales');

function setNested(obj, keyPath, value) {
  const parts = keyPath.split('.');
  let cur = obj;
  for (let i = 0; i < parts.length - 1; i++) {
    cur[parts[i]] ??= {};
    cur = cur[parts[i]];
  }
  cur[parts[parts.length - 1]] = value;
}

const ptFlat = {
  'common.loading': 'A carregar…',
  'common.save': 'Guardar',
  'common.cancel': 'Cancelar',
  'common.close': 'Fechar',
  'common.edit': 'Editar',
  'common.new': 'Novo',
  'common.yes': 'Sim',
  'common.no': 'Não',
  'common.search': 'Pesquisar…',
  'common.retry': 'Tentar novamente',
  'common.document': 'Documento',
  'common.error': 'Erro',
  'common.name': 'Nome',
  'common.active': 'Ativo',
  'common.inactive': 'Inativo',
  'common.unavailableNow': 'Indisponível neste momento',
  'common.paid': 'pago',
  'common.payment': 'pagamento',
  'common.outOfStock': 'Esgotada',
  'common.outOfStockSuffix': 'esgotada',
  'common.noSlot': 'sem vaga',
  'common.hidden': 'oculta',
  'common.floristChoice': 'Escolha da florista',
  'common.floristChoiceParen': '(escolha da florista)',
  'common.required': '(obrigatório)',
  'common.lowStock': 'baixo',
  'common.dash': '—',
  'common.roles.cliente': 'Cliente',
  'common.roles.florista': 'Florista',
  'common.roles.superAdmin': 'SuperAdmin',
  'common.portfolio': 'Portfolio',
  'common.floristHub': 'Florist Hub',

  'nav.ariaMain': 'Navegação principal',
  'nav.store': 'Loja',
  'nav.portfolio': 'Portfolio',
  'nav.buildArrangement': 'Montar arranjo',
  'nav.orders': 'Encomendas',
  'nav.recurring': 'Recorrências',
  'nav.floristPanel': 'Painel da florista',
  'nav.superAdmin': 'SuperAdmin',
  'nav.login': 'Entrar',
  'nav.logout': 'Sair',
  'nav.account': 'Conta',

  'footer.terms': 'Termos',
  'footer.privacy': 'Privacidade',
  'footer.cookies': 'Cookies',
  'footer.myData': 'Os meus dados',

  'adminShell.managementMenu': 'Menu de gestão',
  'adminShell.storeManagement': 'Gestão da loja',
  'adminShell.viewStoreAsCustomer': 'Ver loja (cliente)',
  'adminShell.platform': 'Plataforma',
  'adminShell.superAdminAria': 'SuperAdmin',

  'admin.nav.dashboard': 'Painel',
  'admin.nav.onboarding': 'Configuração inicial',
  'admin.nav.settings': 'Definições da loja',
  'admin.nav.catalog': 'Catálogo',
  'admin.nav.capacity': 'Capacidade',
  'admin.nav.portfolio': 'Portfolio',
  'admin.nav.orders': 'Encomendas',
  'admin.nav.recurring': 'Recorrências',
  'admin.nav.payments': 'Pagamentos',
  'admin.nav.invoicing': 'Faturação',
  'admin.nav.customers': 'Clientes',
  'admin.nav.coupons': 'Cupões',
  'admin.nav.campaigns': 'Datas especiais',
  'admin.nav.reviews': 'Avaliações',
  'admin.nav.gdpr': 'RGPD',

  'superadmin.nav.tenants': 'Lojas',
  'superadmin.nav.billing': 'Comissão',
  'superadmin.nav.metrics': 'Métricas',
  'superadmin.nav.gdpr': 'RGPD',

  'auth.login.title': 'Entrar',
  'auth.login.titleWithStore': 'Entrar — {{storeName}}',
  'auth.login.lede': 'Use o email e a palavra-passe da sua conta.',
  'auth.login.email': 'Correio eletrónico',
  'auth.login.password': 'Palavra-passe',
  'auth.login.submit': 'Entrar',
  'auth.login.noAccount': 'Ainda não tem conta?',
  'auth.login.registerLink': 'Registar',
  'auth.login.errorGeneric': 'Email ou palavra-passe incorretos.',

  'auth.register.title': 'Registar',
  'auth.register.lede': 'Cliente nesta loja ou nova loja de florista.',
  'auth.register.role': 'Perfil',
  'auth.register.cliente': 'Cliente',
  'auth.register.floristaNewStore': 'Florista (nova loja)',
  'auth.register.storeName': 'Nome da loja',
  'auth.register.storeSlug': 'Slug (subdomínio)',
  'auth.register.linkedToVisitedStore': 'A conta ficará associada à loja que está a visitar.',
  'auth.register.fullName': 'Nome completo',
  'auth.register.legalRequired': 'Tem de aceitar os Termos e a Política de Privacidade.',
  'auth.register.tenantUnknown': 'Não foi possível identificar a loja. Abra o site da florista e tente de novo.',
  'auth.register.submit': 'Criar conta',
  'auth.register.failure': 'Falha no registo',
  'auth.register.hasAccount': 'Já tem conta?',

  'cookies.bannerAria': 'Consentimento de cookies',
  'cookies.message': 'Cookies essenciais para a sessão; opcionalmente monitorização de erros.',
  'cookies.policyLink': 'Política de Cookies',
  'cookies.essentialOnly': 'Apenas essenciais',
  'cookies.acceptAll': 'Aceitar todos',

  'preferences.sectionTitle': 'Aparência e idioma',
  'preferences.sectionDescription': 'Escolha o tema da interface e o idioma preferido neste dispositivo.',
  'preferences.language.label': 'Idioma',
  'preferences.language.pt': 'Português (Portugal)',
  'preferences.language.en': 'English',
  'preferences.appearance.label': 'Tema',
  'preferences.appearance.light': 'Claro',
  'preferences.appearance.dark': 'Escuro',
  'preferences.appearance.system': 'Sistema',

  'errors.generic': 'Ocorreu um erro. Tente novamente.',
  'errors.requestDenied': 'Pedido recusado.',
  'errors.loadFailed': 'Não foi possível carregar',
  'errors.loadFailedTitle': 'Não foi possível carregar',

  'session.superAdminPlatform': 'SuperAdmin (plataforma)',
  'session.storeFallback': 'Loja',

  'account.title': 'Perfil',
  'account.lede': 'Informação da conta e, quando aplicável, mudança entre a plataforma e as suas lojas.',
  'account.sectionAccount': 'Conta',
  'account.email': 'E-mail',
  'account.contextSection': 'Contexto de trabalho',
  'account.refreshingContexts': 'A atualizar contextos…',
  'account.singleContext': 'Só tem um contexto disponível',
  'account.activeBadge': 'Ativo',
  'account.switchTo': 'Mudar para aqui',
  'account.contextsLoadError': 'Não foi possível carregar os contextos disponíveis.',
  'account.contextSwitchError': 'Não foi possível mudar de contexto.',

  'app.embedConfiguratorLoading': 'A preparar o configurador…',

  'loading.order': 'A carregar a encomenda…',
  'loading.arrangement': 'A carregar arranjo…',
  'loading.wizard': 'A carregar assistente…',
  'loading.campaign': 'A carregar campanha…',
  'loading.settings': 'A carregar definições…',
  'loading.document': 'A carregar documento…',
  'loading.invoicing': 'A carregar faturação…',
  'loading.seasonal': 'A carregar flores da época…',
  'loading.store': 'A carregar loja…',
  'loading.metrics': 'A carregar métricas…',
  'loading.dashboard': 'A carregar o dashboard…',

  'store.defaultName': 'A sua florista online',
  'store.heroCtaBuild': 'Montar o meu arranjo',
  'store.portfolioHeading': 'Portfolio',
  'store.inspireLede': 'Inspire-se e monte o seu arranjo.',
  'store.inspireLedeCheckout': 'Inspire-se e personalize numa nova encomenda.',
  'store.emptyPortfolioTitle': 'Ainda sem arranjos no portfolio',
  'store.emptyPortfolioDescription': 'Volte em breve — a florista está a preparar novas criações.',
  'store.useInspiration': 'Usar inspiração',
  'store.soonNewTitle': 'Em breve novos arranjos',
  'store.soonNewDescription': 'Explore o portfolio ou monte o seu arranjo com o catálogo da loja.',
  'store.reviews_one': 'avaliação',
  'store.reviews_other': 'avaliações',
  'store.seasonalTitle': 'Flores da época',
  'store.seasonalMeta': 'Flores de época disponíveis nesta loja.',
  'store.notFound': 'Loja não encontrada',
  'store.openFromFlorist': 'Abra a página a partir da florista.',
  'store.noSeasonalTitle': 'Sem flores de época este mês',
  'store.noSeasonalDescription': 'O catálogo completo continua disponível ao montar um arranjo.',
  'store.campaignUnavailableTitle': 'Campanha indisponível',
  'store.campaignUnavailableDescription': 'Esta campanha não está ativa.',
  'store.campaignDefaultMeta': 'Campanha da florista.',
  'store.arrangementNotFoundTitle': 'Arranjo não encontrado',
  'store.arrangementNotFoundDescription': 'Este endereço não corresponde a um arranjo publicado.',
  'store.arrangementPortfolioMeta': 'Arranjo do portfolio.',
  'store.orderFromFloristMeta': 'Encomende flores e arranjos nesta florista.',
  'store.floristSuffix': 'florista',
  'store.campaignSuffix': 'campanha',
  'store.inspireFromPortfolio': 'Inspirar-me neste arranjo',

  'order.status.0': 'Pendente de confirmação',
  'order.status.1': 'Confirmada',
  'order.status.2': 'Em preparação',
  'order.status.3': 'Pronta',
  'order.status.4': 'Entregue',
  'order.status.5': 'Cancelada',
  'order.status.6': 'Recusada',

  'order.paymentStatus.0': 'Pendente',
  'order.paymentStatus.1': 'Pago',
  'order.paymentStatus.2': 'Falhado',
  'order.paymentStatus.3': 'Reembolsado parcialmente',
  'order.paymentStatus.4': 'Reembolsado',
  'order.paymentStatus.5': 'Cancelado',

  'order.deliveryType.0': 'Levantamento na loja',
  'order.deliveryType.1': 'Entrega ao domicílio',
  'order.deliveryType.2': 'Cemitério / outro local',

  'invoice.kind.0': 'Fatura-recibo',
  'invoice.kind.1': 'Nota de crédito',
  'invoice.status.0': 'Pendente',
  'invoice.status.1': 'Emitida',
  'invoice.status.2': 'Falhada',

  'configurator.base.0': 'Jarra',
  'configurator.base.1': 'Esponja',
  'configurator.base.2': 'Embrulho',
  'configurator.shape.0': 'Esponja',
  'configurator.shape.1': 'Comprido',
  'configurator.shape.2': 'Curto',
  'configurator.format': 'Formato',

  'plural.day_one': 'dia',
  'plural.day_other': 'dias',
  'plural.order_one': 'encomenda',
  'plural.order_other': 'encomendas',

  'tenant.createSuccess.inviteSent':
    'Loja {{name}} criada. Foi enviado um convite por email à florista para definir a palavra-passe (válido 72 horas).',
  'tenant.createSuccess.existingAccount':
    'Loja {{name}} criada. A conta existente foi associada como florista; não foi enviado convite por email.',

  'checkout.submitHint.guest': 'Entra ou cria uma conta de cliente para submeter a encomenda.',
  'checkout.submitHint.wrongRole':
    'Para submeter uma encomenda nesta loja, entra com uma conta de cliente (ou cria registo de cliente).',
};

const enFlat = {
  'common.loading': 'Loading…',
  'common.save': 'Save',
  'common.cancel': 'Cancel',
  'common.close': 'Close',
  'common.edit': 'Edit',
  'common.new': 'New',
  'common.yes': 'Yes',
  'common.no': 'No',
  'common.search': 'Search…',
  'common.retry': 'Try again',
  'common.document': 'Document',
  'common.error': 'Error',
  'common.name': 'Name',
  'common.active': 'Active',
  'common.inactive': 'Inactive',
  'common.unavailableNow': 'Unavailable right now',
  'common.paid': 'paid',
  'common.payment': 'payment',
  'common.outOfStock': 'Sold out',
  'common.outOfStockSuffix': 'sold out',
  'common.noSlot': 'no slot',
  'common.hidden': 'hidden',
  'common.floristChoice': "Florist's choice",
  'common.floristChoiceParen': "(florist's choice)",
  'common.required': '(required)',
  'common.lowStock': 'low',
  'common.dash': '—',
  'common.roles.cliente': 'Customer',
  'common.roles.florista': 'Florist',
  'common.roles.superAdmin': 'SuperAdmin',
  'common.portfolio': 'Portfolio',
  'common.floristHub': 'Florist Hub',

  'nav.ariaMain': 'Main navigation',
  'nav.store': 'Shop',
  'nav.portfolio': 'Portfolio',
  'nav.buildArrangement': 'Build arrangement',
  'nav.orders': 'Orders',
  'nav.recurring': 'Subscriptions',
  'nav.floristPanel': 'Florist dashboard',
  'nav.superAdmin': 'SuperAdmin',
  'nav.login': 'Sign in',
  'nav.logout': 'Sign out',
  'nav.account': 'Account',

  'footer.terms': 'Terms',
  'footer.privacy': 'Privacy',
  'footer.cookies': 'Cookies',
  'footer.myData': 'My data',

  'adminShell.managementMenu': 'Management menu',
  'adminShell.storeManagement': 'Store management',
  'adminShell.viewStoreAsCustomer': 'View shop (customer)',
  'adminShell.platform': 'Platform',
  'adminShell.superAdminAria': 'SuperAdmin',

  'admin.nav.dashboard': 'Dashboard',
  'admin.nav.onboarding': 'Getting started',
  'admin.nav.settings': 'Store settings',
  'admin.nav.catalog': 'Catalog',
  'admin.nav.capacity': 'Capacity',
  'admin.nav.portfolio': 'Portfolio',
  'admin.nav.orders': 'Orders',
  'admin.nav.recurring': 'Subscriptions',
  'admin.nav.payments': 'Payments',
  'admin.nav.invoicing': 'Invoicing',
  'admin.nav.customers': 'Customers',
  'admin.nav.coupons': 'Coupons',
  'admin.nav.campaigns': 'Special dates',
  'admin.nav.reviews': 'Reviews',
  'admin.nav.gdpr': 'GDPR',

  'superadmin.nav.tenants': 'Stores',
  'superadmin.nav.billing': 'Commission',
  'superadmin.nav.metrics': 'Metrics',
  'superadmin.nav.gdpr': 'GDPR',

  'auth.login.title': 'Sign in',
  'auth.login.titleWithStore': 'Sign in — {{storeName}}',
  'auth.login.lede': 'Use your account email and password.',
  'auth.login.email': 'Email',
  'auth.login.password': 'Password',
  'auth.login.submit': 'Sign in',
  'auth.login.noAccount': "Don't have an account?",
  'auth.login.registerLink': 'Register',
  'auth.login.errorGeneric': 'Incorrect email or password.',

  'auth.register.title': 'Register',
  'auth.register.lede': 'Customer at this shop or a new florist store.',
  'auth.register.role': 'Profile',
  'auth.register.cliente': 'Customer',
  'auth.register.floristaNewStore': 'Florist (new store)',
  'auth.register.storeName': 'Store name',
  'auth.register.storeSlug': 'Slug (subdomain)',
  'auth.register.linkedToVisitedStore': 'Your account will be linked to the shop you are visiting.',
  'auth.register.fullName': 'Full name',
  'auth.register.legalRequired': 'You must accept the Terms and Privacy Policy.',
  'auth.register.tenantUnknown': 'Could not identify the store. Open the florist site and try again.',
  'auth.register.submit': 'Create account',
  'auth.register.failure': 'Registration failed',
  'auth.register.hasAccount': 'Already have an account?',

  'cookies.bannerAria': 'Cookie consent',
  'cookies.message': 'Essential cookies for your session; optional error monitoring.',
  'cookies.policyLink': 'Cookie Policy',
  'cookies.essentialOnly': 'Essential only',
  'cookies.acceptAll': 'Accept all',

  'preferences.sectionTitle': 'Appearance and language',
  'preferences.sectionDescription': 'Choose the interface theme and preferred language on this device.',
  'preferences.language.label': 'Language',
  'preferences.language.pt': 'Portuguese (Portugal)',
  'preferences.language.en': 'English',
  'preferences.appearance.label': 'Theme',
  'preferences.appearance.light': 'Light',
  'preferences.appearance.dark': 'Dark',
  'preferences.appearance.system': 'System',

  'errors.generic': 'Something went wrong. Please try again.',
  'errors.requestDenied': 'Request denied.',
  'errors.loadFailed': 'Could not load',
  'errors.loadFailedTitle': 'Could not load',

  'session.superAdminPlatform': 'SuperAdmin (platform)',
  'session.storeFallback': 'Store',

  'account.title': 'Profile',
  'account.lede': 'Account details and, when applicable, switch between the platform and your stores.',
  'account.sectionAccount': 'Account',
  'account.email': 'Email',
  'account.contextSection': 'Working context',
  'account.refreshingContexts': 'Refreshing contexts…',
  'account.singleContext': 'You only have one available context',
  'account.activeBadge': 'Active',
  'account.switchTo': 'Switch here',
  'account.contextsLoadError': 'Could not load available contexts.',
  'account.contextSwitchError': 'Could not switch context.',

  'app.embedConfiguratorLoading': 'Preparing configurator…',

  'loading.order': 'Loading order…',
  'loading.arrangement': 'Loading arrangement…',
  'loading.wizard': 'Loading assistant…',
  'loading.campaign': 'Loading campaign…',
  'loading.settings': 'Loading settings…',
  'loading.document': 'Loading document…',
  'loading.invoicing': 'Loading invoicing…',
  'loading.seasonal': 'Loading seasonal flowers…',
  'loading.store': 'Loading store…',
  'loading.metrics': 'Loading metrics…',
  'loading.dashboard': 'Loading dashboard…',

  'store.defaultName': 'Your online florist',
  'store.heroCtaBuild': 'Build my arrangement',
  'store.portfolioHeading': 'Portfolio',
  'store.inspireLede': 'Get inspired and build your arrangement.',
  'store.inspireLedeCheckout': 'Get inspired and customise a new order.',
  'store.emptyPortfolioTitle': 'No portfolio arrangements yet',
  'store.emptyPortfolioDescription': 'Check back soon — the florist is preparing new creations.',
  'store.useInspiration': 'Use as inspiration',
  'store.soonNewTitle': 'New arrangements coming soon',
  'store.soonNewDescription': 'Browse the portfolio or build an arrangement from the store catalog.',
  'store.reviews_one': 'review',
  'store.reviews_other': 'reviews',
  'store.seasonalTitle': 'Seasonal flowers',
  'store.seasonalMeta': 'Seasonal flowers available at this shop.',
  'store.notFound': 'Store not found',
  'store.openFromFlorist': 'Open the page from the florist site.',
  'store.noSeasonalTitle': 'No seasonal flowers this month',
  'store.noSeasonalDescription': 'The full catalog is still available when building an arrangement.',
  'store.campaignUnavailableTitle': 'Campaign unavailable',
  'store.campaignUnavailableDescription': 'This campaign is not active.',
  'store.campaignDefaultMeta': 'Florist campaign.',
  'store.arrangementNotFoundTitle': 'Arrangement not found',
  'store.arrangementNotFoundDescription': 'This URL does not match a published arrangement.',
  'store.arrangementPortfolioMeta': 'Portfolio arrangement.',
  'store.orderFromFloristMeta': 'Order flowers and arrangements from this florist.',
  'store.floristSuffix': 'florist',
  'store.campaignSuffix': 'campaign',
  'store.inspireFromPortfolio': 'Inspire me with this arrangement',

  'order.status.0': 'Awaiting confirmation',
  'order.status.1': 'Confirmed',
  'order.status.2': 'In preparation',
  'order.status.3': 'Ready',
  'order.status.4': 'Delivered',
  'order.status.5': 'Cancelled',
  'order.status.6': 'Declined',

  'order.paymentStatus.0': 'Pending',
  'order.paymentStatus.1': 'Paid',
  'order.paymentStatus.2': 'Failed',
  'order.paymentStatus.3': 'Partially refunded',
  'order.paymentStatus.4': 'Refunded',
  'order.paymentStatus.5': 'Cancelled',

  'order.deliveryType.0': 'Store pickup',
  'order.deliveryType.1': 'Home delivery',
  'order.deliveryType.2': 'Cemetery / other location',

  'invoice.kind.0': 'Invoice-receipt',
  'invoice.kind.1': 'Credit note',
  'invoice.status.0': 'Pending',
  'invoice.status.1': 'Issued',
  'invoice.status.2': 'Failed',

  'configurator.base.0': 'Vase',
  'configurator.base.1': 'Floral foam',
  'configurator.base.2': 'Wrap',
  'configurator.shape.0': 'Round',
  'configurator.shape.1': 'Elongated',
  'configurator.shape.2': 'Short',
  'configurator.format': 'Shape',

  'plural.day_one': 'day',
  'plural.day_other': 'days',
  'plural.order_one': 'order',
  'plural.order_other': 'orders',

  'tenant.createSuccess.inviteSent':
    'Store {{name}} created. An email invite was sent to the florist to set a password (valid for 72 hours).',
  'tenant.createSuccess.existingAccount':
    'Store {{name}} created. The existing account was linked as florist; no email invite was sent.',

  'checkout.submitHint.guest': 'Sign in or create a customer account to submit the order.',
  'checkout.submitHint.wrongRole':
    'To submit an order at this shop, sign in with a customer account (or register as a customer).',
};

// Auto-ingest UI strings from source for keys under pages.*
const srcRoot = path.join(__dirname, '../src');
const uiStringRe = /['"]((?:\\.|[^'"\\\n\r]){2,120})['"]/g;
const skipRe =
  /^(api\/|Bearer |Content-Type|floristhub\.|localhost|\/|className|aria-hidden|#[0-9a-f]{3,8}|--|[A-Z_]{3,}|pointer:|DELETE|POST|PUT|PATCH|Florist|Authorization|M\d)/i;
const badFragment =
  /[<>{}]|\\r|\\n|style=\{|textAlign|Button|Card|form\.|\.tsx|\.ts|padding|margin|role=|type=/;

function slugify(s) {
  return s
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
  .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 48);
}

function walk(dir, files = []) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name);
    if (ent.isDirectory()) walk(p, files);
    else if (/\.(tsx|ts)$/.test(ent.name) && !ent.name.endsWith('.test.ts')) files.push(p);
  }
  return files;
}

for (const file of walk(srcRoot)) {
  const rel = path.relative(srcRoot, file).replace(/\.(tsx|ts)$/, '');
  const section = rel.replace(/\//g, '.');
  const text = fs.readFileSync(file, 'utf8');
  let m;
  while ((m = uiStringRe.exec(text))) {
    const s = m[1];
    if (s.startsWith('./') || s.startsWith('../')) continue;
    if (s.includes('${') || skipRe.test(s) || badFragment.test(s)) continue;
    if (!/[A-Za-zÀ-ú]/.test(s)) continue;
    if (/^[a-z][a-zA-Z0-9_-]{0,14}$/.test(s) && !/[àáâãéêíóôõúç]/.test(s)) continue;
    const key = `pages.${section}.${slugify(s)}`;
    if (ptFlat[key]) continue;
  ptFlat[key] = s;
  enFlat[key] ??= s;
  }
}

const pt = {};
const en = {};
for (const [k, v] of Object.entries(ptFlat)) setNested(pt, k, v);
for (const [k, v] of Object.entries(enFlat)) setNested(en, k, v);

fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'pt-PT.json'), `${JSON.stringify(pt, null, 2)}\n`);
fs.writeFileSync(path.join(outDir, 'en.json'), `${JSON.stringify(en, null, 2)}\n`);

function countKeys(obj) {
  let n = 0;
  for (const v of Object.values(obj)) {
    if (v && typeof v === 'object' && !Array.isArray(v)) n += countKeys(v);
    else n += 1;
  }
  return n;
}

console.log('pt-PT keys:', countKeys(pt));
console.log('en keys:', countKeys(en));
