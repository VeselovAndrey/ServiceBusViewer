import { useEffect, useMemo, useState } from 'react';
import { buildFullMessageJson } from '../../lib/messageJson';
import { cx } from '../../lib/cx';
import { tryFormatJsonBody } from '../../lib/jsonFormatting';
import type { ReceivedMessageDto } from '../../types/serviceBus';

interface JsonMessageBodyProps {
  body: string;
  fullMessage?: ReceivedMessageDto;
  className?: string;
}

const iconButtonClassName =
  'inline-flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-700 transition hover:border-slate-300 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:border-slate-700 dark:hover:bg-slate-800';

export function JsonMessageBody({ body, fullMessage, className }: JsonMessageBodyProps) {
  const formattedJson = useMemo(() => tryFormatJsonBody(body), [body]);
  const [isFormatted, setIsFormatted] = useState(Boolean(formattedJson));
  const [copyStatus, setCopyStatus] = useState<'idle' | 'copied' | 'failed'>('idle');
  const [fullCopyStatus, setFullCopyStatus] = useState<'idle' | 'copied' | 'failed'>('idle');

  useEffect(() => {
    setIsFormatted(Boolean(formattedJson));
  }, [formattedJson]);

  useEffect(() => {
    if (copyStatus === 'idle') {
      return undefined;
    }

    const timeoutId = window.setTimeout(() => {
      setCopyStatus('idle');
    }, 2000);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [copyStatus]);

  useEffect(() => {
    if (fullCopyStatus === 'idle') {
      return undefined;
    }

    const timeoutId = window.setTimeout(() => {
      setFullCopyStatus('idle');
    }, 2000);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [fullCopyStatus]);

  const copyText = async (
    text: string,
    setCopyStatus: (status: 'copied' | 'failed') => void,
  ): Promise<void> => {
    if (!navigator.clipboard?.writeText) {
      setCopyStatus('failed');
      return;
    }

    try {
      await navigator.clipboard.writeText(text);
      setCopyStatus('copied');
    } catch {
      setCopyStatus('failed');
    }
  };

  const handleCopyClick = async () => {
    await copyText(body, setCopyStatus);
  };

  const handleFullCopyClick = async () => {
    if (!fullMessage) {
      return;
    }

    await copyText(buildFullMessageJson(fullMessage), setFullCopyStatus);
  };

  return (
    <div className={cx('js-message-container', className)}>
      <div className="mb-2 flex items-center justify-between gap-3">
        <h3 className="text-[11px] font-bold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
          Message Body
        </h3>
        <div className="flex items-center gap-2">
          <span
            aria-live="polite"
            className={cx(
              'text-[11px] font-medium',
              copyStatus === 'copied' && 'text-emerald-600 dark:text-emerald-400',
              copyStatus === 'failed' && 'text-rose-600 dark:text-rose-400',
            )}
          >
            {copyStatus === 'copied'
              ? 'Copied'
              : copyStatus === 'failed'
                ? 'Copy failed'
                : null}
          </span>
          <button
            type="button"
            className={iconButtonClassName}
            onClick={handleCopyClick}
            aria-label="Copy message body"
            title="Copy message body"
          >
            <span className="material-icons-round text-sm" aria-hidden="true">
              content_copy
            </span>
          </button>
          {fullMessage ? (
            <>
              <button
                type="button"
                className={iconButtonClassName}
                onClick={handleFullCopyClick}
                aria-label="Copy full message as JSON"
                title="Copy full message as JSON"
              >
                <span className="material-icons-round text-sm" aria-hidden="true">
                  data_object
                </span>
              </button>
              <span
                aria-live="polite"
                className={cx(
                  'text-[11px] font-medium',
                  fullCopyStatus === 'copied' && 'text-emerald-600 dark:text-emerald-400',
                  fullCopyStatus === 'failed' && 'text-rose-600 dark:text-rose-400',
                )}
              >
                {fullCopyStatus === 'copied'
                  ? 'Copied JSON'
                  : fullCopyStatus === 'failed'
                    ? 'Copy failed'
                    : null}
              </span>
            </>
          ) : null}
          <button
            type="button"
            className={cx(
              'js-json-toggle inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition hover:border-slate-300 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:border-slate-700 dark:hover:bg-slate-800',
              formattedJson && 'js-json-toggle-visible',
            )}
            aria-pressed={isFormatted}
            onClick={() => {
              if (formattedJson) {
                setIsFormatted((current) => !current);
              }
            }}
          >
            {isFormatted ? 'Raw' : 'Formatted'}
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
