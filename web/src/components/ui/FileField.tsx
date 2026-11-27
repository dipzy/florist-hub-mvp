import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';

type Props = {
  label: string;
  accept?: string;
  multiple?: boolean;
  onFile?: (file: File | null) => void;
  onFiles?: (files: File[]) => void;
};

export function FileField({ label, accept = 'image/*', multiple = false, onFile, onFiles }: Props) {
  const { t } = useTranslation();
  const id = useId();
  const [name, setName] = useState<string | null>(null);

  return (
    <div className="fh-file">
      <span className="fh-file__label">{label}</span>
      <div className="fh-file__row">
        <label className="fh-btn fh-btn--secondary fh-btn--md fh-file__button" htmlFor={id}>
          {t('common.chooseFile')}
          <input
            id={id}
            type="file"
            accept={accept}
            multiple={multiple}
            onChange={(event) => {
              const files = Array.from(event.target.files ?? []);
              setName(files.length > 1 ? `${files.length} ficheiros` : files[0]?.name ?? null);
              onFile?.(files[0] ?? null);
              onFiles?.(files);
            }}
          />
        </label>
        {name && <span className="muted fh-file__name">{name}</span>}
      </div>
    </div>
  );
}
