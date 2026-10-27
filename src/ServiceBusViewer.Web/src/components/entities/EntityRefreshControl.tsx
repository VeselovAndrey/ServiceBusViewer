import { useCallback, useEffect, useRef, useState } from 'react';
import { serviceBusApi } from '../../api/serviceBusApi';
import { useEntityCountAutoRefreshInterval } from '../../hooks/useEntityCountAutoRefreshInterval';
import { cx } from '../../lib/cx';
import {
  ENTITY_COUNT_AUTO_REFRESH_INTERVAL_PRESETS,
  parseEntityCountAutoRefreshInterval,
} from '../../lib/entityCountAutoRefreshInterval';
import { useAppState } from '../../state/AppStateContext';
import type { ViewerState } from '../../types/serviceBus';

const getViewerEntityKey = (viewer: ViewerState) =>
  `${viewer.topicName ?? ''}:${viewer.entityName ?? ''}`;

export function EntityRefreshControl() {
  const {
    sessionState,
    getConnectionGeneration,
    setViewerState,
    entityCountAutoRefreshMode,
    setEntityCountAutoRefreshMode,
  } = useAppState();
  const isConnected = sessionState?.isConnected ?? false;
  // The Service Bus emulator reports no runtime counts: hide the count refresh control there entirely.
  const usesDevelopmentEmulator = sessionState?.viewer?.usesDevelopmentEmulator ?? false;
  const viewerEntityKey = sessionState?.viewer ? getViewerEntityKey(sessionState.viewer) : null;
  // Counts may also be unavailable without the emulator (e.g. direct entity access without management): offer no auto refresh then.
  const countsAvailable = (sessionState?.viewer?.availableEntities ?? []).some(
    (entity) => entity.messageCounts !== null,
  );
  const autoAvailable =
    isConnected &&
    !usesDevelopmentEmulator &&
    countsAvailable &&
    (sessionState?.entityCountAutoRefreshEnabled ?? false);

  const containerRef = useRef<HTMLDivElement>(null);
  const refreshInFlightRef = useRef(false);
  // Auto mode is app-level state (Bug 55): Viewer and Entity Details each remount this control,
  // so a component-local flag would silently fall back to Manual on navigation.
  const autoActive = entityCountAutoRefreshMode !== null;
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const [customEditorOpen, setCustomEditorOpen] = useState(false);
  const [customInput, setCustomInput] = useState('');
  const [intervalSeconds, setIntervalSeconds] = useEntityCountAutoRefreshInterval();

  const effectiveAutoActive = autoActive && autoAvailable;
  const isCustomInterval = !ENTITY_COUNT_AUTO_REFRESH_INTERVAL_PRESETS.includes(
    intervalSeconds as (typeof ENTITY_COUNT_AUTO_REFRESH_INTERVAL_PRESETS)[number],
  );

  const refreshCounts = useCallback(async () => {
    if (refreshInFlightRef.current) {
      return;
    }

    refreshInFlightRef.current = true;
    setIsRefreshing(true);
    const connectionGeneration = getConnectionGeneration();
    const requestEntityKey = viewerEntityKey;

    try {
      const nextViewer = await serviceBusApi.refreshViewer();
      // Keep the transient send result banner, and apply counts only to the entity that requested them.
      setViewerState(
        (current) =>
          current !== null &&
          requestEntityKey !== null &&
          getViewerEntityKey(current) === requestEntityKey &&
          getViewerEntityKey(nextViewer) === requestEntityKey
            ? { ...nextViewer, sendResultMessage: current.sendResultMessage }
            : current,
        connectionGeneration,
      );
    } catch {
      // Best-effort refresh: the existing counts remain displayed.
    } finally {
      refreshInFlightRef.current = false;
      setIsRefreshing(false);
    }
  }, [getConnectionGeneration, setViewerState, viewerEntityKey]);

  // Stop auto refresh when it is no longer available (e.g. counts are no longer reported).
  // A disconnect is handled by the app state, which resets the mode while the session ends.
  useEffect(() => {
    if (autoActive && !autoAvailable) {
      setEntityCountAutoRefreshMode(null);
    }
  }, [autoActive, autoAvailable, setEntityCountAutoRefreshMode]);

  // Poll at the selected interval while auto refresh is active.
  useEffect(() => {
    if (!effectiveAutoActive) {
      return;
    }

    const intervalId = window.setInterval(() => {
      void refreshCounts();
    }, intervalSeconds * 1000);

    return () => window.clearInterval(intervalId);
  }, [effectiveAutoActive, intervalSeconds, refreshCounts]);

  // Close the mode menu on Escape or pointer down outside the control.
  useEffect(() => {
    if (!menuOpen) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.stopPropagation();
        setMenuOpen(false);
        setCustomEditorOpen(false);
      }
    };
    const handlePointerDown = (event: Event) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setMenuOpen(false);
        setCustomEditorOpen(false);
      }
    };

    document.addEventListener('keydown', handleKeyDown, true);
    document.addEventListener('pointerdown', handlePointerDown, true);
    return () => {
      document.removeEventListener('keydown', handleKeyDown, true);
      document.removeEventListener('pointerdown', handlePointerDown, true);
    };
  }, [menuOpen]);

  if (!isConnected || usesDevelopmentEmulator) {
    return null;
  }

  const pickManual = () => {
    setEntityCountAutoRefreshMode(null);
    setMenuOpen(false);
    setCustomEditorOpen(false);
  };

  const pickInterval = (seconds: number) => {
    setIntervalSeconds(seconds);
    setEntityCountAutoRefreshMode(seconds);
    setMenuOpen(false);
    setCustomEditorOpen(false);
  };

  const applyCustomInterval = () => {
    const parsed = parseEntityCountAutoRefreshInterval(customInput);
    if (parsed === null) {
      return;
    }

    pickInterval(parsed);
  };

  const menuItemClass =
    'flex w-full items-center gap-2.5 px-4 py-2 text-left text-xs font-medium text-slate-700 transition hover:bg-slate-50 focus-visible:bg-slate-50 focus-visible:outline-none dark:text-slate-200 dark:hover:bg-zinc-800/60 dark:focus-visible:bg-zinc-800/60';
  const checkIcon = (checked: boolean) => (
    <span
      className={cx(
        'ml-auto material-icons-round text-base leading-none',
        checked ? 'text-brand-500' : 'invisible',
      )}
      aria-hidden="true"
    >
      check
    </span>
  );

  return (
    <div className="relative ml-auto" ref={containerRef}>
      <div className="flex items-center gap-0.5">
        {autoAvailable ? (
          <button
            type="button"
            className={cx(
              'inline-flex items-center gap-0.5 rounded-lg px-1.5 py-1 text-[0.6875rem] font-semibold uppercase tracking-wide transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500',
              effectiveAutoActive
                ? 'text-brand-600 hover:bg-brand-50 dark:text-brand-300 dark:hover:bg-brand-500/10'
                : 'text-slate-500 hover:bg-slate-200 hover:text-slate-700 dark:text-slate-400 dark:hover:bg-zinc-800 dark:hover:text-slate-100',
            )}
            title="Entity count refresh mode"
            aria-label="Entity count refresh mode"
            aria-haspopup="menu"
            aria-expanded={menuOpen}
            onClick={() => setMenuOpen((current) => !current)}
          >
            {effectiveAutoActive ? `Every ${intervalSeconds} sec` : 'Manual'}
            <span className="material-icons-round text-sm" aria-hidden="true">
              expand_more
            </span>
          </button>
        ) : null}

        <button
          type="button"
          className="inline-flex items-center justify-center rounded-lg p-1.5 text-slate-500 transition hover:bg-slate-200 hover:text-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:opacity-50 dark:text-slate-400 dark:hover:bg-zinc-800 dark:hover:text-slate-100"
          title="Refresh entity counts"
          aria-label="Refresh entity counts"
          disabled={isRefreshing}
          onClick={() => {
            void refreshCounts();
          }}
        >
          <span
            className={cx('material-icons-round text-[1.125rem]', isRefreshing && 'animate-spin')}
            aria-hidden="true"
          >
            refresh
          </span>
        </button>
      </div>

      {menuOpen && autoAvailable ? (
        <div
          role="menu"
          aria-label="Entity count refresh mode"
          className="absolute right-0 top-full z-50 mt-1 min-w-44 overflow-hidden rounded-xl border border-slate-200 bg-white py-1.5 shadow-xl dark:border-zinc-800 dark:bg-zinc-900"
        >
          <button
            type="button"
            role="menuitemradio"
            aria-checked={!effectiveAutoActive}
            className={menuItemClass}
            onClick={pickManual}
          >
            <span className="material-icons-round text-base leading-none" aria-hidden="true">
              touch_app
            </span>
            Manual
            {checkIcon(!effectiveAutoActive)}
          </button>

          <div className="my-1 h-px bg-slate-100 dark:bg-zinc-800" role="separator" />

          {ENTITY_COUNT_AUTO_REFRESH_INTERVAL_PRESETS.map((seconds) => (
            <button
              key={seconds}
              type="button"
              role="menuitemradio"
              aria-checked={effectiveAutoActive && intervalSeconds === seconds}
              className={menuItemClass}
              onClick={() => pickInterval(seconds)}
            >
              <span className="material-icons-round text-base leading-none" aria-hidden="true">
                schedule
              </span>
              Every {seconds} sec
              {checkIcon(effectiveAutoActive && intervalSeconds === seconds)}
            </button>
          ))}

          {customEditorOpen ? (
            <div className="flex items-center gap-1.5 px-4 py-2">
              <input
                type="text"
                inputMode="numeric"
                className="block w-20 rounded-md border border-slate-200 bg-white px-2 py-1 text-xs text-slate-700 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-zinc-700 dark:bg-zinc-900 dark:text-slate-100 dark:focus:border-brand-400"
                placeholder="Seconds"
                aria-label="Custom refresh interval in seconds"
                value={customInput}
                autoFocus
                onChange={(event) => setCustomInput(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter') {
                    event.preventDefault();
                    applyCustomInterval();
                  }
                }}
              />
              <button
                type="button"
                className="rounded-md px-2 py-1 text-xs font-semibold text-brand-600 transition hover:bg-brand-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:opacity-50 dark:text-brand-300 dark:hover:bg-brand-500/10"
                disabled={parseEntityCountAutoRefreshInterval(customInput) === null}
                onClick={applyCustomInterval}
              >
                Apply
              </button>
            </div>
          ) : (
            <button
              type="button"
              role="menuitemradio"
              aria-checked={effectiveAutoActive && isCustomInterval}
              className={menuItemClass}
              onClick={() => {
                setCustomInput(isCustomInterval ? String(intervalSeconds) : '');
                setCustomEditorOpen(true);
              }}
            >
              <span className="material-icons-round text-base leading-none" aria-hidden="true">
                tune
              </span>
              {isCustomInterval ? `Custom (${intervalSeconds} sec)` : 'Custom…'}
              {checkIcon(effectiveAutoActive && isCustomInterval)}
            </button>
          )}
        </div>
      ) : null}
    </div>
  );
}
