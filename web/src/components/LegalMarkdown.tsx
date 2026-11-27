import type { ReactNode } from 'react';

type Props = {
  title: string;
  body: string;
  responsible?: string | null;
};

type Block =
  | { kind: 'h1' | 'h2' | 'quote' | 'p'; text: string }
  | { kind: 'ul'; items: string[] };

/** Renderização simples do markdown legal (títulos, listas, negrito e parágrafos). */
export function LegalMarkdown({ title, body, responsible }: Props) {
  const blocks = parseLegalMarkdown(`${body}\n\n${responsible ?? ''}`);

  return (
    <article className="legal-doc">
      <h1>{title}</h1>
      {blocks.map((block, index) => renderBlock(block, index, title))}
    </article>
  );
}

function renderBlock(block: Block, index: number, pageTitle: string): ReactNode {
  if (block.kind === 'h1') {
    if (block.text.trim() === pageTitle.trim()) return null;
    return <h2 key={index}>{inline(block.text)}</h2>;
  }
  if (block.kind === 'h2') return <h2 key={index}>{inline(block.text)}</h2>;
  if (block.kind === 'quote') return <p key={index} className="legal-disclaimer">{inline(block.text)}</p>;
  if (block.kind === 'ul') {
    return (
      <ul key={index} className="list compact">
        {block.items.map((item) => (
          <li key={item}>{inline(item)}</li>
        ))}
      </ul>
    );
  }
  return <p key={index}>{inline(block.text)}</p>;
}

function inline(text: string): ReactNode[] {
  return text.split(/(\*\*[^*]+\*\*)/g).map((part, index) =>
    part.startsWith('**') && part.endsWith('**') ? <strong key={index}>{part.slice(2, -2)}</strong> : part,
  );
}

export function parseLegalMarkdown(source: string): Block[] {
  const blocks: Block[] = [];
  let paragraph: string[] = [];
  let list: string[] | null = null;

  function flushParagraph() {
    if (paragraph.length === 0) return;
    blocks.push({ kind: 'p', text: paragraph.join(' ') });
    paragraph = [];
  }

  function flushList() {
    if (!list) return;
    blocks.push({ kind: 'ul', items: list });
    list = null;
  }

  for (const raw of source.replace(/\r\n/g, '\n').split('\n')) {
    const line = raw.trim();
    if (!line) {
      flushParagraph();
      flushList();
      continue;
    }
    if (line.startsWith('# ')) {
      flushParagraph();
      flushList();
      blocks.push({ kind: 'h1', text: line.slice(2).trim() });
      continue;
    }
    if (line.startsWith('## ')) {
      flushParagraph();
      flushList();
      blocks.push({ kind: 'h2', text: line.slice(3).trim() });
      continue;
    }
    if (line.startsWith('> ')) {
      flushParagraph();
      flushList();
      blocks.push({ kind: 'quote', text: line.slice(2).trim() });
      continue;
    }
    if (line.startsWith('- ')) {
      flushParagraph();
      list ??= [];
      list.push(line.slice(2).trim());
      continue;
    }
    flushList();
    paragraph.push(line);
  }

  flushParagraph();
  flushList();
  return blocks;
}
