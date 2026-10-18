import { Fragment, type MouseEventHandler } from 'react';
import { getDisplayedMessageCounts } from '../../lib/messageCounts';
import { cx } from '../../lib/cx';
import type { EntityMessageCountsDto } from '../../types/serviceBus';
import { useEntityTooltip } from '../layout/EntityTooltipProvider';

interface EntityMessageCountsBadgeProps {
  counts: EntityMessageCountsDto | null;
  onClick?: MouseEventHandler;
}

export function EntityMessageCountsBadge({ counts, onClick }: EntityMessageCountsBadgeProps) {
  const { bindTooltip } = useEntityTooltip();

  if (!counts) {
    return null;
  }

  const displayedCategories = getDisplayedMessageCounts(counts);

  return (
    <span
      className={cx(
        'shrink-0 self-center rounded bg-slate-200/70 px-1.5 py-0.5 text-[0.5625rem] tabular-nums text-slate-500 dark:bg-zinc-800 dark:text-slate-400',
        onClick && 'cursor-pointer',
      )}
      onClick={onClick}
    >
      {displayedCategories.map((category, index) => {
        const hint = `${category.label}: ${category.value}`;

        return (
          <Fragment key={category.label}>
            {index > 0 ? <span aria-hidden="true"> | </span> : null}
            <span tabIndex={0} aria-label={hint} {...bindTooltip(hint)}>
              {category.value}
            </span>
          </Fragment>
        );
      })}
    </span>
  );
}
