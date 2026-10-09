import {
	useEffect,
	useLayoutEffect,
	useRef,
	useState,
	type CSSProperties,
	type ReactNode,
} from 'react';
import { cx } from '../../lib/cx';
import type { RepublishMessageIdStrategy } from '../../types/serviceBus';

export interface RepublishStrategyOption {
	description: string;
	value: RepublishMessageIdStrategy;
}

export const republishStrategyOptions: RepublishStrategyOption[] = [
	{ value: 'KeepOriginal', description: 'Keep the original id.' },
	{ value: 'AutoGenerate', description: 'Let Service Bus generate a new id.' },
	{ value: 'Guid', description: 'Use a new random GUID.' },
	{ value: 'GuidV7', description: 'Use a new time-ordered GUID v7.' },
	{ value: 'Manual', description: 'Type an id for this message.' },
];

interface RepublishStrategyMenuProps {
	align: 'left' | 'right';
	ariaBusy?: boolean;
	buttonClassName: string;
	buttonContent: ReactNode;
	disabled?: boolean;
	options: RepublishStrategyOption[];
	onPick: (strategy: RepublishMessageIdStrategy) => void;
}

const EDGE_OFFSET_PX = 4;

export function RepublishStrategyMenu({
	align,
	ariaBusy = false,
	buttonClassName,
	buttonContent,
	disabled = false,
	options,
	onPick,
}: RepublishStrategyMenuProps) {
	const buttonRef = useRef<HTMLButtonElement>(null);
	const menuRef = useRef<HTMLDivElement>(null);
	const [open, setOpen] = useState(false);
	const [position, setPosition] = useState<CSSProperties | null>(null);

	const toggleOpen = () => {
		if (open) {
			setOpen(false);
			return;
		}

		const rect = buttonRef.current?.getBoundingClientRect();
		if (rect === undefined) {
			return;
		}

		setPosition({
			top: rect.bottom + EDGE_OFFSET_PX,
			...(align === 'right' ? { right: window.innerWidth - rect.right } : { left: rect.left }),
		});
		setOpen(true);
	};

	// Flip the menu above the button when it would overflow the viewport bottom.
	useLayoutEffect(() => {
		if (!open || position === null || menuRef.current === null) {
			return;
		}

		const menuRect = menuRef.current.getBoundingClientRect();
		if (menuRect.bottom > window.innerHeight - EDGE_OFFSET_PX) {
			const rect = buttonRef.current?.getBoundingClientRect();
			if (rect !== undefined) {
				setPosition({ ...position, top: rect.top - menuRect.height - EDGE_OFFSET_PX });
			}
		}
	}, [open, position]);

	useEffect(() => {
		if (!open) {
			return;
		}

		const close = () => setOpen(false);
		const handleKeyDown = (event: KeyboardEvent) => {
			if (event.key === 'Escape') {
				event.stopPropagation();
				setOpen(false);
				buttonRef.current?.focus();
			}
		};
		const handlePointerDown = (event: Event) => {
			const target = event.target as Node;
			if (menuRef.current?.contains(target) || buttonRef.current?.contains(target)) {
				return;
			}

			setOpen(false);
		};

		window.addEventListener('scroll', close, true);
		window.addEventListener('resize', close);
		document.addEventListener('keydown', handleKeyDown, true);
		document.addEventListener('pointerdown', handlePointerDown, true);
		return () => {
			window.removeEventListener('scroll', close, true);
			window.removeEventListener('resize', close);
			document.removeEventListener('keydown', handleKeyDown, true);
			document.removeEventListener('pointerdown', handlePointerDown, true);
		};
	}, [open]);

	return (
		<>
			<button
				ref={buttonRef}
				type="button"
				aria-busy={ariaBusy || undefined}
				aria-expanded={open}
				aria-haspopup="menu"
				className={buttonClassName}
				disabled={disabled}
				onClick={toggleOpen}
			>
				{buttonContent}
			</button>

			{open && position !== null ? (
				<div
					ref={menuRef}
					aria-label="Message id strategy"
					role="menu"
					className={cx(
						'fixed z-50 w-72 overflow-hidden rounded-2xl border border-slate-200 bg-white py-1.5 shadow-xl',
						'dark:border-slate-800 dark:bg-slate-900',
					)}
					style={position}
				>
					{options.map((option) => (
						<button
							key={option.value}
							type="button"
							role="menuitem"
							className="block w-full px-4 py-2.5 text-left transition hover:bg-slate-50 focus-visible:bg-slate-50 focus-visible:outline-none dark:hover:bg-slate-800/60 dark:focus-visible:bg-slate-800/60"
							onClick={() => {
								setOpen(false);
								onPick(option.value);
							}}
						>
							<span className="block text-xs font-semibold text-slate-700 dark:text-slate-200">
								{option.value}
							</span>
							<span className="mt-0.5 block text-xs text-slate-500 dark:text-slate-400">
								{option.description}
							</span>
						</button>
					))}
				</div>
			) : null}
		</>
	);
}
