import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type HTMLAttributes,
  type ReactNode,
} from 'react';

interface TooltipState {
  target: HTMLElement;
  text: string;
}

interface TooltipContextValue {
  bindTooltip: (text: string) => HTMLAttributes<HTMLElement>;
}

const TooltipContext = createContext<TooltipContextValue | null>(null);
const tooltipDelayMs = 2000;
const tooltipHideDelayMs = 150;
const tooltipAnchorName = '--entity-tooltip';

// The popover polyfill cannot match :popover-open, so guard toggle calls instead.
function safeShowPopover(element: HTMLElement) {
  try {
    element.showPopover();
  } catch {
    // Already open.
  }
}

function safeHidePopover(element: HTMLElement) {
  try {
    element.hidePopover();
  } catch {
    // Already closed.
  }
}

export function EntityTooltipProvider({ children }: { children: ReactNode }) {
  const [tooltipState, setTooltipState] = useState<TooltipState | null>(null);
  const tooltipRef = useRef<HTMLDivElement | null>(null);
  const showTimerRef = useRef<number | null>(null);
  const hideTimerRef = useRef<number | null>(null);

  const clearShowTimer = () => {
    if (showTimerRef.current !== null) {
      window.clearTimeout(showTimerRef.current);
      showTimerRef.current = null;
    }
  };

  const clearHideTimer = () => {
    if (hideTimerRef.current !== null) {
      window.clearTimeout(hideTimerRef.current);
      hideTimerRef.current = null;
    }
  };

  const hideTooltip = useCallback(() => {
    clearShowTimer();
    clearHideTimer();
    setTooltipState(null);
  }, []);

  const scheduleHideTooltip = useCallback(() => {
    clearShowTimer();
    clearHideTimer();
    hideTimerRef.current = window.setTimeout(() => {
      hideTooltip();
    }, tooltipHideDelayMs);
  }, [hideTooltip]);

  const scheduleTooltip = useCallback((text: string, target: HTMLElement) => {
    clearShowTimer();
    clearHideTimer();
    showTimerRef.current = window.setTimeout(() => {
      setTooltipState({ target, text });
    }, tooltipDelayMs);
  }, []);

  // Show/hide the popover and bind the CSS anchor name to the current target.
  useEffect(() => {
    const tooltip = tooltipRef.current;
    if (!tooltip) {
      return;
    }

    if (!tooltipState) {
      safeHidePopover(tooltip);
      return;
    }

    const { target } = tooltipState;
    target.style.anchorName = tooltipAnchorName;
    safeShowPopover(tooltip);

    return () => {
      safeHidePopover(tooltip);
      target.style.removeProperty('anchor-name');
    };
  }, [tooltipState]);

  const bindTooltip = useCallback(
    (text: string): HTMLAttributes<HTMLElement> => ({
      onBlur: () => hideTooltip(),
      onFocus: (event) => scheduleTooltip(text, event.currentTarget),
      onMouseEnter: (event) => scheduleTooltip(text, event.currentTarget),
      onMouseLeave: () => scheduleHideTooltip(),
      onTouchCancel: () => hideTooltip(),
      onTouchEnd: () => hideTooltip(),
      onTouchMove: () => hideTooltip(),
      onTouchStart: (event) => scheduleTooltip(text, event.currentTarget),
    }),
    [hideTooltip, scheduleHideTooltip, scheduleTooltip],
  );

  const value = useMemo<TooltipContextValue>(() => ({ bindTooltip }), [bindTooltip]);

  return (
    <TooltipContext.Provider value={value}>
      {children}
      <div
        ref={tooltipRef}
        className="entity-name-tooltip"
        popover=""
        onMouseEnter={clearHideTimer}
        onMouseLeave={hideTooltip}
        onToggle={(event) => {
          // Keep React state in sync when the popover is light-dismissed (e.g. Escape).
          if ((event.nativeEvent as ToggleEvent).newState === 'closed') {
            setTooltipState(null);
          }
        }}
      >
        {tooltipState?.text}
      </div>
    </TooltipContext.Provider>
  );
}

export function useEntityTooltip(): TooltipContextValue {
  const context = useContext(TooltipContext);
  if (!context) {
    throw new Error('useEntityTooltip must be used within EntityTooltipProvider.');
  }

  return context;
}
