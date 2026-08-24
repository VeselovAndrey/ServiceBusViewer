import { useEffect, useMemo, useState } from 'react';
import { cx } from '../../lib/cx';
import { tryFormatJsonBody } from '../../lib/jsonFormatting';
import { CopyButton } from './CopyButton';

interface JsonMessageBodyProps {
	body: string;
	className?: string;
}


export function JsonMessageBody({ body, className }: JsonMessageBodyProps) {
  const formattedJson = useMemo(() => tryFormatJsonBody(body), [body]);
  const [isFormatted, setIsFormatted] = useState(Boolean(formattedJson));

  useEffect(() => {
    setIsFormatted(Boolean(formattedJson));
  }, [formattedJson]);

  return (
    <div className={cx('js-message-container', className)}>
      <div className="mb-2 flex items-center justify-between gap-3">
        <h3 className="text-[11px] font-bold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
          Message Body
        </h3>
        <div className="flex items-center gap-2">
          <CopyButton
            text={body}
            label="Copy body"
            icon="content_copy"
            className="min-w-[104px] px-3"
          />
          <button
            type="button"
            disabled={!formattedJson}
            className={cx(
              'inline-flex min-w-[88px] items-center justify-center gap-2 whitespace-nowrap rounded-lg border px-3 py-1.5 text-xs font-medium transition',
              formattedJson
                ? 'border-slate-200 bg-white text-slate-700 hover:border-slate-300 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:border-slate-700 dark:hover:bg-slate-800'
                : 'cursor-not-allowed border-slate-200 bg-slate-50 text-slate-400 dark:border-slate-800 dark:bg-slate-950/40 dark:text-slate-600',
            )}
            aria-pressed={Boolean(formattedJson) && isFormatted}
            title={formattedJson ? undefined : 'Body is not valid JSON'}
            onClick={() => {
              if (formattedJson) {
                setIsFormatted((current) => !current);
              }
            }}
          >
            <span className="material-icons-round text-sm" aria-hidden="true">
              {isFormatted ? 'receipt_long' : 'code'}
            </span>
            {isFormatted ? 'Source' : 'Format'}
          </button>
        </div>
      </div>

      {formattedJson && isFormatted ? (
        <pre
          className="js-message-body custom-scrollbar overflow-x-auto rounded-xl border border-slate-200 bg-slate-50 p-4 font-mono text-xs leading-6 text-slate-700 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-200"
          dangerouslySetInnerHTML={{ __html: formattedJson.html }}
        />
      ) : (
        <pre className="js-message-body custom-scrollbar overflow-x-auto rounded-xl border border-slate-200 bg-slate-50 p-4 font-mono text-xs leading-6 text-slate-700 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-200">
          {body}
        </pre>
      )}
    </div>
  );
}
