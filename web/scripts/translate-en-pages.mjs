import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const localeDir = path.join(__dirname, '../src/i18n/locales');

function flatten(obj, prefix = '') {
  const out = {};
  for (const [k, v] of Object.entries(obj)) {
    const key = prefix ? `${prefix}.${k}` : k;
    if (typeof v === 'string') out[key] = v;
    else if (v && typeof v === 'object' && !Array.isArray(v)) Object.assign(out, flatten(v, key));
  }
  return out;
}

function unflatten(flat) {
  const root = {};
  for (const [key, value] of Object.entries(flat)) {
    const parts = key.split('.');
    let cur = root;
    for (let i = 0; i < parts.length - 1; i++) {
      cur[parts[i]] ??= {};
      cur = cur[parts[i]];
    }
    cur[parts[parts.length - 1]] = value;
  }
  return root;
}

const EXACT = new Map([
  ['A carregar…', 'Loading…'],
  ['A carregar o dashboard…', 'Loading dashboard…'],
  ['A carregar o estado da encomenda…', 'Loading order status…'],
  ['A carregar a encomenda…', 'Loading order…'],
  ['A preparar o configurador…', 'Preparing configurator…'],
  ['Não foi possível carregar o dashboard.', 'Could not load the dashboard.'],
  ['Marcar em preparação', 'Mark in preparation'],
  ['Marcar pronta', 'Mark ready'],
  ['Marcar entregue', 'Mark delivered'],
  ['Pendentes', 'Pending'],
  ['Confirmadas', 'Confirmed'],
  ['Em preparação', 'In preparation'],
  ['Prontas', 'Ready'],
  ['Entregues', 'Delivered'],
  ['Todas', 'All'],
  ['Confirmada', 'Confirmed'],
  ['Pronta', 'Ready'],
  ['Entregue', 'Delivered'],
  ['Nenhuma', 'None'],
  ['Todos os estados', 'All statuses'],
  ['Todas as entregas', 'All delivery types'],
  ['Cliente (nome ou email)', 'Customer (name or email)'],
  ['Quadro por estado', 'Board by status'],
  ['Sem encomendas neste filtro.', 'No orders match this filter.'],
  ['Pendente', 'Pending'],
  ['Aceite', 'Accepted'],
  ['Recusada', 'Declined'],
  ['Retirada', 'Withdrawn'],
  ['Enviar avaliação', 'Submit review'],
  ['Ainda não há avaliações.', 'No reviews yet.'],
  ['Novidades nas encomendas', 'Order updates'],
  ['Marcar como lidas', 'Mark as read'],
  ['Pagamento recebido', 'Payment received'],
  ['Nova encomenda', 'New order'],
  ['Existências baixas:', 'Low stock:'],
  ['Entregas hoje', 'Deliveries today'],
  ['Entregas amanhã', 'Deliveries tomorrow'],
  ['Preparação da loja', 'Store setup'],
  ['Continuar configuração', 'Continue setup'],
  ['Painel', 'Dashboard'],
  ['Encomendas e receita por dia', 'Orders and revenue by day'],
  ['Receita', 'Revenue'],
  ['Encomendas', 'Orders'],
  ['com vaso', 'with vase'],
  ['esponja, sem vaso', 'foam, no vase'],
  ['embrulho, sem vaso', 'wrap, no vase'],
  ['Recusada pelo cliente', 'Declined by customer'],
  ['Pré-visualização 3D do arranjo', '3D arrangement preview'],
  ['Pré-visualização 2D do arranjo', '2D arrangement preview'],
  ['Pré-visualização do arranjo', 'Arrangement preview'],
  ['Túlipa amarela', 'Yellow tulip'],
  ['Lírio', 'Lily'],
  ['sábado', 'Saturday'],
  ['Sábado', 'Saturday'],
  ['Código', 'Code'],
  ['Ex.: substituir túlipas por rosas', 'E.g. swap tulips for roses'],
  ['As palavras-passe não coincidem.', 'Passwords do not match.'],
  ['Código do cupão', 'Coupon code'],
  [
    'O catálogo completo continua disponível ao montar um arranjo.',
    'The full catalogue is still available when you build an arrangement.',
  ],
  ['Não', 'No'],
  ['não', 'no'],
  ['16% — Açores', '16% — Azores'],
  ['13% — taxa intermédia', '13% — intermediate rate'],
  ['Já guardada. Preencha só para substituir.', 'Already saved. Fill in only to replace.'],
  [
    'A conta está pronta a receber pagamentos.',
    'The account is ready to accept payments.',
  ],
  [
    'A conta está ligada, mas o Stripe ainda não autorizou cobranças.',
    'The account is connected, but Stripe has not enabled charges yet.',
  ],
]);

const WORD = [
  [/Não foi possível/g, 'Could not'],
  [/não foi possível/g, 'could not'],
  [/A carregar/g, 'Loading'],
  [/a carregar/g, 'loading'],
  [/Guardar/g, 'Save'],
  [/Cancelar/g, 'Cancel'],
  [/Encomenda/g, 'Order'],
  [/encomenda/g, 'order'],
  [/Encomendas/g, 'Orders'],
  [/encomendas/g, 'orders'],
  [/Entrega/g, 'Delivery'],
  [/entrega/g, 'delivery'],
  [/Florista/g, 'Florist'],
  [/florista/g, 'florist'],
  [/Cliente/g, 'Customer'],
  [/cliente/g, 'customer'],
  [/Morada/g, 'Address'],
  [/morada/g, 'address'],
  [/Palavra-passe/g, 'Password'],
  [/palavra-passe/g, 'password'],
  [/avaliação/g, 'review'],
  [/avaliações/g, 'reviews'],
  [/Descrição/g, 'Description'],
  [/descrição/g, 'description'],
  [/Nome/g, 'Name'],
  [/nome/g, 'name'],
  [/opcional/gi, 'optional'],
  [/obrigatório/gi, 'required'],
];

function translatePageString(ptText) {
  if (EXACT.has(ptText)) return EXACT.get(ptText);
  let out = ptText;
  for (const [re, rep] of WORD) out = out.replace(re, rep);
  return out;
}

function isJunkPath(s) {
  return s.startsWith('./') || s.startsWith('../');
}

const ptPath = path.join(localeDir, 'pt-PT.json');
const enPath = path.join(localeDir, 'en.json');
const fpt = flatten(JSON.parse(fs.readFileSync(ptPath, 'utf8')));
const fen = flatten(JSON.parse(fs.readFileSync(enPath, 'utf8')));

let updated = 0;
let removed = 0;

for (const key of Object.keys(fpt)) {
  if (key.startsWith('pages.') && isJunkPath(fpt[key])) {
    delete fpt[key];
    delete fen[key];
    removed++;
  }
}

for (const key of Object.keys(fen)) {
  if (!key.startsWith('pages.')) continue;
  const ptVal = fpt[key];
  const enVal = fen[key];
  if (!ptVal || enVal !== ptVal) continue;
  fen[key] = translatePageString(ptVal);
  updated++;
}

const PT_MARK = /[àáâãéêíóôõúçÀÁÂÃÉÊÍÓÔÕÚÇ]/;
for (let pass = 0; pass < 6; pass++) {
  let passUpdates = 0;
  for (const key of Object.keys(fen)) {
    if (!key.startsWith('pages.')) continue;
    const val = fen[key];
    if (typeof val !== 'string') continue;
    if (val === fpt[key] || PT_MARK.test(val)) {
      const next = translatePageString(fpt[key] ?? val);
      if (next !== val) {
        fen[key] = next;
        passUpdates++;
      }
    }
  }
  if (passUpdates === 0) break;
}

fs.writeFileSync(ptPath, `${JSON.stringify(unflatten(fpt), null, 2)}\n`);
fs.writeFileSync(enPath, `${JSON.stringify(unflatten(fen), null, 2)}\n`);
console.log('updated', updated, 'removed', removed);
