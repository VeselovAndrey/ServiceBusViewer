import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { serviceBusApi } from '../api/serviceBusApi';
import { getErrorMessages } from '../lib/problemDetails';
import type { SessionStateDto, ViewerState } from '../types/serviceBus';

interface AppStateContextValue {
  sessionState: SessionStateDto | null;
  appliedEntityFilterText: string;
  entityCountAutoRefreshMode: number | null;
  entityFilterText: string;
  errorMessages: string[];
  isLoading: boolean;
  getConnectionGeneration: () => number;
  invalidateConnectionGeneration: () => void;
  refreshSessionState: () => Promise<SessionStateDto>;
  setAppliedEntityFilterText: (filterText: string) => void;
  setEntityCountAutoRefreshMode: (mode: number | null) => void;
  setEntityFilterText: (filterText: string) => void;
  setViewerState: (viewer: SetViewerStateValue, connectionGeneration?: number) => void;
}

type SetViewerStateValue = ViewerState | null | ((current: ViewerState | null) => ViewerState | null);

const AppStateContext = createContext<AppStateContextValue | null>(null);

export function AppStateProvider({ children }: { children: ReactNode }) {
  const [sessionState, setSessionState] = useState<SessionStateDto | null>(null);
  const [appliedEntityFilterText, setAppliedEntityFilterText] = useState('');
  const [entityFilterText, setEntityFilterText] = useState('');
  // Entity-count auto-refresh mode (interval seconds, or null for Manual) lives at app level:
  // Viewer and Entity Details each mount their own refresh control, so component state resets
  // on navigation (Bug 55).
  const [entityCountAutoRefreshMode, setEntityCountAutoRefreshMode] = useState<number | null>(null);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const hasLoadedRef = useRef(false);
  const connectionGenerationRef = useRef(0);
  const sessionStateRequestRef = useRef(0);

  const getConnectionGeneration = useCallback(() => connectionGenerationRef.current, []);
  const invalidateConnectionGeneration = useCallback(() => {
    connectionGenerationRef.current += 1;
  }, []);

  const refreshSessionState = useCallback(async () => {
    const requestId = ++sessionStateRequestRef.current;
    setIsLoading(true);

    try {
      const nextSessionState = await serviceBusApi.getSessionState();
      if (requestId !== sessionStateRequestRef.current) {
        return nextSessionState;
      }

      setSessionState((current) => {
        if (current?.isConnected !== nextSessionState.isConnected) {
          connectionGenerationRef.current += 1;
        }
        return nextSessionState;
      });
      if (!nextSessionState.isConnected) {
        setAppliedEntityFilterText('');
        setEntityFilterText('');
        // Leaving the session ends auto refresh; the user re-enables it on the next connection.
        setEntityCountAutoRefreshMode(null);
      }
      setErrorMessages([]);
      return nextSessionState;
    } catch (error: unknown) {
      if (requestId === sessionStateRequestRef.current) {
        setErrorMessages(getErrorMessages(error));
        setSessionState((current) => current);
      }
      throw error;
    } finally {
      if (requestId === sessionStateRequestRef.current) {
        setIsLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    if (hasLoadedRef.current) {
      return;
    }

    hasLoadedRef.current = true;
    void refreshSessionState().catch(() => {
      // The error state is rendered by the app shell.
    });
  }, [refreshSessionState]);

  const setViewerState = useCallback(
    (viewer: SetViewerStateValue, connectionGeneration = connectionGenerationRef.current) => {
      setSessionState((current) => {
        if (!current || connectionGeneration !== connectionGenerationRef.current) {
          return current;
        }

        const nextViewer = typeof viewer === 'function' ? viewer(current.viewer) : viewer;
        if (nextViewer !== null && !current.isConnected) {
          return current;
        }

        return {
          ...current,
          viewer: nextViewer,
        };
      });
    },
    [],
  );

  const value = useMemo<AppStateContextValue>(
    () => ({
      sessionState,
      appliedEntityFilterText,
      entityFilterText,
      entityCountAutoRefreshMode,
      errorMessages,
      isLoading,
      getConnectionGeneration,
      invalidateConnectionGeneration,
      refreshSessionState,
      setAppliedEntityFilterText,
      setEntityCountAutoRefreshMode,
      setEntityFilterText,
      setViewerState,
    }),
    [
      sessionState,
      appliedEntityFilterText,
      entityFilterText,
      entityCountAutoRefreshMode,
      errorMessages,
      isLoading,
      getConnectionGeneration,
      invalidateConnectionGeneration,
      refreshSessionState,
      setEntityCountAutoRefreshMode,
      setViewerState,
    ],
  );

  return (
    <AppStateContext.Provider value={value}>
      {children}
    </AppStateContext.Provider>
  );
}

export function useAppState(): AppStateContextValue {
  const context = useContext(AppStateContext);
  if (!context) {
    throw new Error('useAppState must be used within AppStateProvider.');
  }

  return context;
}
