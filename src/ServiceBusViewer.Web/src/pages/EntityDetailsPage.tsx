import { useEffect, useMemo, useState } from 'react';
import { Navigate, useNavigate, useSearchParams } from 'react-router-dom';
import { serviceBusApi } from '../api/serviceBusApi';
import { EntityDetailsContent } from '../components/details/EntityDetailsContent';
import { EntitySidebar } from '../components/entities/EntitySidebar';
import { AppLayout } from '../components/layout/AppLayout';
import { getEntityKey } from '../lib/formatters';
import { getErrorMessages } from '../lib/problemDetails';
import { useAppState } from '../state/AppStateContext';
import type { EntityDetailsDto, EntityIdDto } from '../types/serviceBus';

export function EntityDetailsPage() {
  const navigate = useNavigate();
  const { sessionState, setViewerState } = useAppState();
  const [searchParams] = useSearchParams();
  const [details, setDetails] = useState<EntityDetailsDto | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [selectionKey, setSelectionKey] = useState<string | null>(null);
  const type = searchParams.get('type');
  const name = searchParams.get('name');
  const topicName = searchParams.get('topicName');

  useEffect(() => {
    if (!sessionState?.isConnected || !type || !name) {
      return;
    }

    let isDisposed = false;
    setErrorMessage(null);

    void serviceBusApi
      .getEntityDetails(type, name, topicName)
      .then((nextDetails) => {
        if (!isDisposed) {
          setDetails(nextDetails);
        }
      })
      .catch((error: unknown) => {
        if (!isDisposed) {
          setErrorMessage(getErrorMessages(error)[0] ?? 'Unable to load entity details.');
        }
      });

    return () => {
      isDisposed = true;
    };
  }, [sessionState?.isConnected, name, topicName, type]);

  const icon = useMemo(() => {
    if (type === 'Subscription') {
      return 'subtitles';
    }
    if (type === 'Topic') {
      return 'share';
    }
    return 'view_headline';
  }, [type]);

  if (!sessionState) {
    return null;
  }

  if (!sessionState.isConnected) {
    return <Navigate replace to="/connect" />;
  }

  const handleSelectEntity = async (entity: EntityIdDto) => {
    const nextSelectionKey = getEntityKey(entity);
    setSelectionKey(nextSelectionKey);

    try {
      const nextViewer =
        (await serviceBusApi.selectEntity(entity)) ?? (await serviceBusApi.getViewer());
      setViewerState(nextViewer);
      navigate('/viewer', { replace: true });
    } catch (error: unknown) {
      setErrorMessage(getErrorMessages(error)[0] ?? 'Unable to switch entity.');
    } finally {
      setSelectionKey(null);
    }
  };

  const currentDetails = details;
  const availableEntities =
    currentDetails?.availableEntities ?? sessionState.viewer?.availableEntities ?? [];
  const managementAvailable =
    currentDetails?.isManagementApiAvailable ?? sessionState.viewer?.isManagementApiAvailable ?? false;
  const headerHostName =
    currentDetails?.serviceBusHostName ?? sessionState.viewer?.serviceBusHostName ?? '';

  return (
    <AppLayout
      applicationVersion={sessionState.applicationVersion}
      footerStatus="Entity properties view"
      headerAction={{ icon: 'chevron_left', kind: 'link', label: 'Viewer', to: '/viewer' }}
      headerStatus={{ text: 'Connected to', tone: 'connected', value: headerHostName }}
      title="Entity Details"
    >
      <div className="flex flex-1 flex-col bg-slate-100 text-slate-800 dark:bg-slate-950 dark:text-slate-100">
        <div className="flex flex-1 flex-col overflow-hidden lg:flex-row">
          <EntitySidebar
            activeSelectionKey={selectionKey}
            availableEntities={availableEntities}
            displayPropertyLinks={managementAvailable}
            entityName={name}
            onSelectEntity={handleSelectEntity}
            topicName={topicName}
          />

          <main className="flex min-w-0 flex-1 flex-col overflow-hidden">
            <div className="flex flex-col gap-4 border-b border-slate-200 bg-white/80 px-4 py-4 dark:border-slate-800 dark:bg-slate-950/60 lg:flex-row lg:items-center lg:justify-between lg:px-6">
              <div className="min-w-0">
                <div className="flex items-center gap-3">
                  <span className="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-brand-100 text-brand-700 dark:bg-brand-900/60 dark:text-brand-300">
                    <span className="material-icons-round text-[20px]">{icon}</span>
                  </span>

                  <div className="min-w-0">
                    <h1 className="truncate text-lg font-semibold text-slate-900 dark:text-white">
                      {name ?? 'Entity details'}
                    </h1>
                    <p className="text-sm text-slate-500 dark:text-slate-400">
                      {type ?? 'Entity'} entity
                      {topicName ? (
                        <span>
                          {' '}
                          for topic{' '}
                          <span className="font-mono text-slate-700 dark:text-slate-200">
                            {topicName}
                          </span>
                        </span>
                      ) : null}
                    </p>
                  </div>
                </div>
              </div>

              <div className="rounded-full border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
                Cached properties view
              </div>
            </div>

            {type && name ? (
              <EntityDetailsContent
                details={
                  currentDetails ?? {
                    availableEntities,
                    isManagementApiAvailable: managementAvailable,
                    name,
                    properties: null,
                    serviceBusHostName: headerHostName,
                    topicName,
                    type: type as EntityDetailsDto['type'],
                  }
                }
                errorMessage={errorMessage}
              />
            ) : (
              <div className="custom-scrollbar flex-1 overflow-y-auto p-4 lg:p-6">
                <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900/70 dark:bg-rose-950/40 dark:text-rose-200">
                  Entity type and name are required.
                </div>
              </div>
            )}
          </main>
        </div>
      </div>
    </AppLayout>
  );
}
