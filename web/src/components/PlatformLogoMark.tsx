type Props = {
  size?: number;
  className?: string;
};

/** Logo da plataforma embutido (sem pedido HTTP — evita ícone partido se /logo.svg falhar). */
export function PlatformLogoMark({ size = 40, className = '' }: Props) {
  return (
    <svg
      className={className}
      width={size}
      height={size}
      viewBox="0 0 48 48"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden
    >
      <rect width="48" height="48" rx="12" fill="var(--fh-sage-100)" />
      <path
        fill="var(--fh-sage-700)"
        d="M24 10c-1.2 3.4-3.6 5.8-7 7 3.4 1.2 5.8 3.6 7 7 1.2-3.4 3.6-5.8 7-7-3.4-1.2-5.8-3.6-7-7Z"
      />
      <circle cx="24" cy="24" r="4.5" fill="var(--fh-rose-500)" />
      <path fill="var(--fh-sage-600)" d="M20 34h8v2c0 1.5-1.8 3-4 3s-4-1.5-4-3v-2Z" opacity="0.85" />
    </svg>
  );
}
