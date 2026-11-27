/** Plural simples em português de Portugal (1 → singular, resto → plural). */
export function pluralPt(count: number, singular: string, plural: string): string {
  return count === 1 ? singular : plural;
}

export function countLabel(count: number, singular: string, plural: string): string {
  return `${count} ${pluralPt(count, singular, plural)}`;
}
