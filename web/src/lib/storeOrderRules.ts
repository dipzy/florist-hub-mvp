import type { OpeningHour } from '../api/types';

export const WEEKDAYS_PT = [
  'domingo',
  'segunda-feira',
  'terça-feira',
  'quarta-feira',
  'quinta-feira',
  'sexta-feira',
  'sábado',
] as const;

export function activeDeliveryOptions(checkout: {
  pickupEnabled: boolean;
  homeDeliveryEnabled: boolean;
  cemeteryDeliveryEnabled: boolean;
}): { value: number; label: string }[] {
  const opts: { value: number; label: string }[] = [];
  const labels = ['Levantamento na loja', 'Entrega ao domicílio', 'Cemitério / outro local'] as const;
  if (checkout.pickupEnabled) opts.push({ value: 0, label: labels[0] });
  if (checkout.homeDeliveryEnabled) opts.push({ value: 1, label: labels[1] });
  if (checkout.cemeteryDeliveryEnabled) opts.push({ value: 2, label: labels[2] });
  return opts;
}

function parseTime(t: string | undefined): number | null {
  if (!t) return null;
  const [h, m] = t.split(':').map(Number);
  if (Number.isNaN(h) || Number.isNaN(m)) return null;
  return h * 60 + m;
}

export function validateRequestedDeliveryAt(
  requestedAt: Date,
  openingHours: OpeningHour[],
  minimumAdvanceMinutes: number,
): string | null {
  const now = new Date();
  const minWhen = new Date(now.getTime() + minimumAdvanceMinutes * 60_000);
  if (requestedAt < minWhen) {
    return `É necessário antecedência mínima de ${minimumAdvanceMinutes} minutos para a data pretendida.`;
  }

  if (openingHours.length === 0) return null;

  const day = requestedAt.getDay();
  const config = openingHours.find((h) => h.dayOfWeek === day);
  if (!config) return null;

  if (config.isClosed) {
    return `A loja está encerrada à ${WEEKDAYS_PT[day]}.`;
  }

  const open = parseTime(config.openTime);
  const close = parseTime(config.closeTime);
  if (open === null || close === null) return null;

  const minutes = requestedAt.getHours() * 60 + requestedAt.getMinutes();
  if (minutes < open || minutes > close) {
    return `A data pretendida tem de ser dentro do horário de funcionamento (${config.openTime}–${config.closeTime}).`;
  }

  return null;
}
