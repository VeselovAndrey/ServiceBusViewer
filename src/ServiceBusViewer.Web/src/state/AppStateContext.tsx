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
import type { BootstrapDto, ViewerState } from '../types/serviceBus';

interface AppStateContextValue {
  bootstrap: BootstrapDto | null;
  errorMessages: string[];
  isLoading: boolean;
  refreshBootstrap: () => Promise<BootstrapDto>;
  setViewerState: (viewer: ViewerState | null) => void;
}

const AppStateContext = createContext<AppStateContextValue | null>(null);

export function AppStateProvider({ children }: { children: ReactNode }) {
  const [bootstrap, setBootstrap] = useState<BootstrapDto | null>(null);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const hasLoadedRef = useRef(false);

  const refreshBootstrap = useCallback(async () => {
    setIsLoading(true);

    try {
      const nextBootstrap = await serviceBusApi.getBootstrap();
      setBootstrap(nextBootstrap);
      setErrorMessages([]);
      return nextBootstrap;
    } catch (error: unknown) {
      setErrorMessages(getErrorMessages(error));
      setBootstrap((current) => current);
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
    void refreshBootstrap().catch(() => {
      // The error state is rendered by the app shell.
    });
  }, [refreshBootstrap]);

  const setViewerState = useCallback((viewer: ViewerState | null) => {
    setBootstrap((current) => {
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
      bootstrap,
      errorMessages,
      isLoading,
      refreshBootstrap,
      setViewerState,
    }),
    [bootstrap, errorMessages, isLoading, refreshBootstrap, setViewerState],
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
