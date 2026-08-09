function escapeHtml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');
}

function syntaxHighlightJson(jsonText: string): string {
  return jsonText.replace(
    /("(\\u[a-fA-F0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+-]?\d+)?)/g,
    (match) => {
      let cssClass = 'json-number';

      if (match.startsWith('"')) {
        cssClass = match.endsWith(':') ? 'json-key' : 'json-string';
      } else if (match === 'true' || match === 'false') {
        cssClass = 'json-boolean';
      } else if (match === 'null') {
        cssClass = 'json-null';
      }

      return `<span class="${cssClass}">${match}</span>`;
    },
  );
}

export interface FormattedJsonBody {
  formattedText: string;
  html: string;
  rawText: string;
}

export function tryFormatJsonBody(rawText: string): FormattedJsonBody | null {
  const trimmed = rawText.trim();
  if (!trimmed) {
    return null;
  }

  try {
    const parsed = JSON.parse(trimmed) as unknown;
    const formattedText = JSON.stringify(parsed, null, 2);
    const html = syntaxHighlightJson(escapeHtml(formattedText));
    return { formattedText, html, rawText };
  } catch {
    return null;
  }
}
