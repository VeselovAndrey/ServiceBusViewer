import { Navigate, Route, Routes } from 'react-router-dom';
import { Alert } from './components/common/Alert';
import { LoadingScreen } from './components/common/LoadingScreen';
import { ConnectPage } from './pages/ConnectPage';
import { EntityDetailsPage } from './pages/EntityDetailsPage';
import { ViewerPage } from './pages/ViewerPage';
import { useAppState } from './state/AppStateContext';

function BootErrorState() {
  const { errorMessages, refreshSessionState } = useAppState();

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 px-4 py-8 dark:bg-slate-950">
      <div className="w-full max-w-xl rounded-3xl border border-slate-200 bg-white/90 p-6 shadow-xl shadow-slate-200/40 dark:border-slate-800 dark:bg-slate-900/90 dark:shadow-black/20 lg:p-8">
        <div className="border-b border-slate-200 pb-4 dark:border-slate-800">
          <h1 className="text-lg font-semibold text-slate-900 dark:text-white">
            Unable to load Service Bus Viewer
          </h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Check that the API is running and reachable, then try again.
          </p>
        </div>

        <Alert messages={errorMessages} tone="error" className="mt-5" />

        <button
          type="button"
          className="mt-6 inline-flex items-center justify-center gap-2 rounded-lg bg-brand-600 px-4 py-3 text-sm font-semibold text-white shadow-lg shadow-brand-950/20 transition hover:bg-brand-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
          onClick={() => {
            void refreshSessionState();
          }}
        >
          <span className="material-icons-round text-sm">refresh</span>
          Retry
        </button>
      </div>
    </div>
  );
}

function HomeRedirect() {
  const { sessionState } = useAppState();
  return <Navigate replace to={sessionState?.isConnected ? '/viewer' : '/connect'} />;
}

export default function App() {
  const { sessionState, isLoading } = useAppState();

  if (isLoading && !sessionState) {
    return <LoadingScreen message="Loading Service Bus Viewer..." />;
  }

  if (!sessionState) {
    return <BootErrorState />;
  }

  return (
    <Routes>
      <Route path="/" element={<HomeRedirect />} />
      <Route path="/connect" element={<ConnectPage />} />
      <Route path="/viewer" element={<ViewerPage />} />
      <Route path="/entities/details" element={<EntityDetailsPage />} />
      <Route path="*" element={<Navigate replace to="/" />} />
    </Routes>
  );
}
