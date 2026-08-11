import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { cx } from '../../lib/cx';
import { getEntityKey } from '../../lib/formatters';
import { useAppState } from '../../state/AppStateContext';
import type { EntityIdDto } from '../../types/serviceBus';
import { useEntityTooltip } from '../layout/EntityTooltipProvider';

interface EntitySidebarProps {
  activeSelectionKey?: string | null;
  availableEntities: EntityIdDto[];
  displayPropertyLinks: boolean;
  entityName: string | null;
  onSelectEntity?: (entity: EntityIdDto) => Promise<void> | void;
  topicName: string | null;
}

const entityFilterDelayMilliseconds = 500;

function buildDetailsPath(entity: EntityIdDto): string {
  const searchParams = new URLSearchParams({ name: entity.name, type: entity.type });
  if (entity.topicName) {
    searchParams.set('topicName', entity.topicName);
  }

  return `/entities/details?${searchParams.toString()}`;
}

export function EntitySidebar({
  activeSelectionKey,
  availableEntities,
  displayPropertyLinks,
  entityName,
  onSelectEntity,
  topicName,
}: EntitySidebarProps) {
  const { bindTooltip } = useEntityTooltip();
  const {
    appliedEntityFilterText,
    entityFilterText,
    setAppliedEntityFilterText,
    setEntityFilterText,
  } = useAppState();

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setAppliedEntityFilterText(entityFilterText.trim().toLocaleLowerCase());
    }, entityFilterDelayMilliseconds);

    return () => window.clearTimeout(timeoutId);
  }, [entityFilterText, setAppliedEntityFilterText]);

  const unfilteredQueues = availableEntities
    .filter((entity) => entity.type === 'Queue')
    .sort((left, right) => left.name.localeCompare(right.name));
  const unfilteredTopics = availableEntities
    .filter((entity) => entity.type === 'Topic')
    .sort((left, right) => left.name.localeCompare(right.name));
  const unfilteredSubscriptions = availableEntities
    .filter((entity) => entity.type === 'Subscription')
    .sort(
      (left, right) =>
        (left.topicName ?? '').localeCompare(right.topicName ?? '') ||
        left.name.localeCompare(right.name),
    );
  const isFiltering = appliedEntityFilterText.length > 0;
  const matchesFilter = (entity: EntityIdDto) =>
    entity.name.toLocaleLowerCase().includes(appliedEntityFilterText);
  const queues = isFiltering ? unfilteredQueues.filter(matchesFilter) : unfilteredQueues;
  const subscriptions = isFiltering
    ? unfilteredSubscriptions.filter(matchesFilter)
    : unfilteredSubscriptions;
  const matchingSubscriptionTopics = new Set(
    subscriptions.map((subscription) => subscription.topicName),
  );
  const topics = isFiltering
    ? unfilteredTopics.filter(
        (topic) => matchesFilter(topic) || matchingSubscriptionTopics.has(topic.name),
      )
    : unfilteredTopics;

  return (
    <aside className="flex w-full shrink-0 flex-col border-b border-slate-200 bg-slate-50 dark:border-zinc-800 dark:bg-zinc-950 lg:w-64 lg:border-r lg:border-b-0">
      <div className="relative flex h-16 items-center px-3">
        <span className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">
          Entities
        </span>
        <div className="pointer-events-none absolute right-3 bottom-0 left-3 h-px bg-slate-200 dark:bg-zinc-800" />
      </div>

      <div className="px-3 pt-3">
        <label className="relative block">
          <span className="sr-only">Filter entities</span>
          <span
            className="material-icons-round pointer-events-none absolute top-1/2 left-2.5 -translate-y-1/2 text-[18px] text-slate-400 dark:text-slate-500"
            aria-hidden="true"
          >
            search
          </span>
          <input
            type="search"
            className="block w-full rounded-lg border border-slate-200 bg-white py-2 pr-3 pl-9 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition placeholder:text-slate-400 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-zinc-800 dark:bg-zinc-900 dark:text-slate-100 dark:shadow-black/20 dark:placeholder:text-slate-500 dark:focus:border-brand-400"
            placeholder="Filter entities"
            value={entityFilterText}
            onChange={(event) => setEntityFilterText(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                setAppliedEntityFilterText(
                  event.currentTarget.value.trim().toLocaleLowerCase(),
                );
              }
            }}
          />
        </label>
      </div>

      <nav className="custom-scrollbar flex-1 space-y-1 overflow-y-auto p-2">
        {queues.length === 0 && topics.length === 0 ? (
          <div className="rounded border border-dashed border-slate-300 bg-white/80 px-4 py-6 text-center text-sm text-slate-500 dark:border-zinc-700 dark:bg-zinc-900/60 dark:text-slate-400">
            {isFiltering ? 'No entities match your filter' : 'No entities available'}
          </div>
        ) : null}

        {queues.length > 0 ? (
          <section className="space-y-1">
            <div className="px-2 py-1.5 text-[11px] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
              Queues
            </div>
            <div className="space-y-1">
              {queues.map((queue) => {
                const isActive = entityName === queue.name && !topicName;
                const entityKey = getEntityKey(queue);

                return (
                  <div key={entityKey} className="flex items-stretch justify-between gap-1 text-sm">
                    <button
                      type="button"
                      className={cx(
                        'flex min-w-0 flex-1 items-center gap-2 rounded px-2 py-1.5 text-left transition',
                        isActive
                          ? 'bg-brand-50 font-medium text-brand-600 dark:bg-brand-500/20 dark:text-brand-100'
                          : 'text-slate-600 hover:bg-slate-200 dark:text-slate-300 dark:hover:bg-zinc-800',
                      )}
                      disabled={!onSelectEntity || activeSelectionKey === entityKey}
                      onClick={() => {
                        if (onSelectEntity) {
                          void onSelectEntity(queue);
                        }
                      }}
                      {...bindTooltip(queue.name)}
                    >
                      <span className="material-icons-round text-sm">view_headline</span>
                      <span className="min-w-0 truncate">{queue.name}</span>
                    </button>

                    {displayPropertyLinks ? (
                      <Link
                        className="inline-flex h-6 w-6 shrink-0 items-center justify-center self-center rounded text-slate-400 transition hover:bg-slate-200 hover:text-slate-700 dark:hover:bg-zinc-700 dark:hover:text-slate-100"
                        title="View queue properties"
                        aria-label="View queue properties"
                        to={buildDetailsPath(queue)}
                      >
                        <span className="material-icons-round text-[18px]">info</span>
                      </Link>
                    ) : null}
                  </div>
                );
              })}
            </div>
          </section>
        ) : null}

        {topics.length > 0 ? (
          <section className="pt-2">
            <div className="px-2 py-1.5 text-[11px] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
              Topics
            </div>
            <div className="space-y-1">
              {topics.map((topic) => {
                const isActiveTopic = entityName === topic.name && !topicName;
                const topicSubscriptions = subscriptions.filter(
                  (subscription) => subscription.topicName === topic.name,
                );

                return (
                  <div key={getEntityKey(topic)} className="rounded">
                    <div className="flex items-stretch justify-between gap-1 text-sm">
                      <div
                        className={cx(
                          'flex min-w-0 flex-1 items-center gap-2 rounded px-2 py-1.5',
                          isActiveTopic
                            ? 'bg-brand-50 font-medium text-brand-600 dark:bg-brand-500/20 dark:text-brand-100'
                            : 'text-slate-600 dark:text-slate-300',
                        )}
                        tabIndex={0}
                        {...bindTooltip(topic.name)}
                      >
                        <span className="material-icons-round text-sm">share</span>
                        <span className="min-w-0 truncate">{topic.name}</span>
                      </div>

                      {displayPropertyLinks ? (
                        <Link
                          className="inline-flex h-6 w-6 shrink-0 items-center justify-center self-center rounded text-slate-400 transition hover:bg-slate-200 hover:text-slate-700 dark:hover:bg-zinc-700 dark:hover:text-slate-100"
                          title="View topic properties"
                          aria-label="View topic properties"
                          to={buildDetailsPath(topic)}
                        >
                          <span className="material-icons-round text-[18px]">info</span>
                        </Link>
                      ) : null}
                    </div>

                    {topicSubscriptions.length > 0 ? (
                      <div className="ml-4 mt-0.5 space-y-0.5 border-l-2 border-slate-200 pl-3 dark:border-zinc-800">
                        {topicSubscriptions.map((subscription) => {
                          const isActiveSubscription =
                            entityName === subscription.name &&
                            topicName === subscription.topicName;
                          const entityKey = getEntityKey(subscription);

                          return (
                            <div
                              key={entityKey}
                              className="flex items-stretch justify-between gap-1 text-sm"
                            >
                              <button
                                type="button"
                                className={cx(
                                  'flex min-w-0 flex-1 items-center gap-2 rounded px-2 py-1.5 text-left transition',
                                  isActiveSubscription
                                    ? 'bg-brand-50 font-medium text-brand-600 dark:bg-brand-500/20 dark:text-brand-100'
                                    : 'text-slate-600 hover:bg-slate-200 dark:text-slate-300 dark:hover:bg-zinc-800',
                                )}
                                disabled={!onSelectEntity || activeSelectionKey === entityKey}
                                onClick={() => {
                                  if (onSelectEntity) {
                                    void onSelectEntity(subscription);
                                  }
                                }}
                                {...bindTooltip(subscription.name)}
                              >
                                <span className="material-icons-round text-sm">subtitles</span>
                                <span className="min-w-0 truncate">{subscription.name}</span>
                              </button>

                              {displayPropertyLinks ? (
                                <Link
                                  className="inline-flex h-6 w-6 shrink-0 items-center justify-center self-center rounded text-slate-400 transition hover:bg-slate-200 hover:text-slate-700 dark:hover:bg-zinc-700 dark:hover:text-slate-100"
                                  title="View subscription properties"
                                  aria-label="View subscription properties"
                                  to={buildDetailsPath(subscription)}
                                >
                                  <span className="material-icons-round text-[18px]">info</span>
                                </Link>
                              ) : null}
                            </div>
                          );
                        })}
                      </div>
                    ) : null}
                  </div>
                );
              })}
            </div>
          </section>
        ) : null}
      </nav>
    </aside>
  );
}
