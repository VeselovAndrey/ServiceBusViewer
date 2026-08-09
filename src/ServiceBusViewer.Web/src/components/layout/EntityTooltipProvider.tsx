import {
  createContext,
  useCallback,
  useContext,
  useLayoutEffect,
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

interface PointerPosition {
  x: number;
  y: number;
}

interface TooltipContextValue {
  bindTooltip: (text: string) => HTMLAttributes<HTMLElement>;
}

const TooltipContext = createContext<TooltipContextValue | null>(null);
const tooltipDelayMs = 3000;

export function EntityTooltipProvider({ children }: { children: ReactNode }) {
  const [tooltipState, setTooltipState] = useState<TooltipState | null>(null);
  const [tooltipStyle, setTooltipStyle] = useState<{
    left: number;
    top: number;
  } | null>(null);
  const tooltipRef = useRef<HTMLDivElement | null>(null);
  const pointerRef = useRef<PointerPosition | null>(null);
  const timerRef = useRef<number | null>(null);

  const clearTimer = () => {
    if (timerRef.current !== null) {
      window.clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  };

  const hideTooltip = useCallback(() => {
    clearTimer();
    setTooltipState(null);
    setTooltipStyle(null);
  }, []);

  const showTooltip = useCallback((text: string, target: HTMLElement) => {
    setTooltipState({ target, text });
  }, []);

  const scheduleTooltip = useCallback(
    (text: string, target: HTMLElement) => {
      clearTimer();
      timerRef.current = window.setTimeout(() => {
        showTooltip(text, target);
      }, tooltipDelayMs);
    },
    [showTooltip],
  );

  useLayoutEffect(() => {
    if (!tooltipState || !tooltipRef.current) {
      return;
    }

    const tooltipRect = tooltipRef.current.getBoundingClientRect();
    const targetRect = tooltipState.target.getBoundingClientRect();
    const anchorX = pointerRef.current?.x ?? targetRect.left + targetRect.width / 2;
    const anchorY = pointerRef.current?.y ?? targetRect.top;
    const left = Math.min(
      window.innerWidth - tooltipRect.width - 12,
      Math.max(12, anchorX - tooltipRect.width / 2),
    );
    const preferredTop = anchorY - tooltipRect.height - 16;
    const top =
      preferredTop >= 12
        ? preferredTop
        : Math.min(window.innerHeight - tooltipRect.height - 12, targetRect.bottom + 12);

    setTooltipStyle({ left, top });
  }, [tooltipState]);

  const bindTooltip = useCallback(
    (text: string): HTMLAttributes<HTMLElement> => ({
      onBlur: () => hideTooltip(),
      onFocus: (event) => scheduleTooltip(text, event.currentTarget),
      onMouseEnter: (event) => scheduleTooltip(text, event.currentTarget),
      onMouseLeave: () => hideTooltip(),
      onMouseMove: (event) => {
        pointerRef.current = { x: event.clientX, y: event.clientY };
      },
      onTouchCancel: () => hideTooltip(),
      onTouchEnd: () => hideTooltip(),
      onTouchMove: () => hideTooltip(),
      onTouchStart: (event) => scheduleTooltip(text, event.currentTarget),
    }),
    [hideTooltip, scheduleTooltip],
  );

  const value = useMemo<TooltipContextValue>(() => ({ bindTooltip }), [bindTooltip]);

  return (
    <TooltipContext.Provider value={value}>
      {children}
      <div
        ref={tooltipRef}
        className="entity-name-tooltip"
        hidden={!tooltipState}
        style={tooltipStyle ? { left: `${tooltipStyle.left}px`, top: `${tooltipStyle.top}px` } : undefined}
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
