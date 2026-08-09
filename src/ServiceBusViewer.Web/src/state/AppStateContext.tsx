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
  errorMessages: string[];
  isLoading: boolean;
  refreshSessionState: () => Promise<SessionStateDto>;
  setViewerState: (viewer: ViewerState | null) => void;
}

const AppStateContext = createContext<AppStateContextValue | null>(null);

export function AppStateProvider({ children }: { children: ReactNode }) {
  const [sessionState, setSessionState] = useState<SessionStateDto | null>(null);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const hasLoadedRef = useRef(false);

  const refreshSessionState = useCallback(async () => {
    setIsLoading(true);

    try {
      const nextSessionState = await serviceBusApi.getSessionState();
      setSessionState(nextSessionState);
      setErrorMessages([]);
      return nextSessionState;
    } catch (error: unknown) {
      setErrorMessages(getErrorMessages(error));
      setSessionState((current) => current);
      throw error;
    } finally {
      setIsLoading(false);
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

  const setViewerState = useCallback((viewer: ViewerState | null) => {
    setSessionState((current) => {
      if (!current) {
        return current;
      }

      return {
        ...current,
        isConnected: viewer !== null,
        viewer,
      };
    });
  }, []);

  const value = useMemo<AppStateContextValue>(
    () => ({
      sessionState,
      errorMessages,
      isLoading,
      refreshSessionState,
      setViewerState,
    }),
    [sessionState, errorMessages, isLoading, refreshSessionState, setViewerState],
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
