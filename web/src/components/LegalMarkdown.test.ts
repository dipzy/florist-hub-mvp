import { describe, expect, it } from 'vitest';
import { parseLegalMarkdown } from './LegalMarkdown';

describe('markdown legal', () => {
  it('separa títulos e parágrafos que só têm uma quebra de linha', () => {
    const blocks = parseLegalMarkdown('> **Aviso:** texto\n\n## 1. Âmbito\nA plataforma permite a venda.');
    expect(blocks.map((block) => block.kind)).toEqual(['quote', 'h2', 'p']);
    expect(blocks[2]).toMatchObject({ text: 'A plataforma permite a venda.' });
  });

  it('agrupa listas', () => {
    const blocks = parseLegalMarkdown('- **Loja:** responsável\n- Cliente');
    expect(blocks).toEqual([{ kind: 'ul', items: ['**Loja:** responsável', 'Cliente'] }]);
  });
});
