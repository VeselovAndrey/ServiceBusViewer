import { useCallback, useEffect, useLayoutEffect, useRef, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';

const FOCUSABLE_SELECTOR = [
	'a[href]',
	'button:not([disabled])',
	'input:not([disabled])',
	'select:not([disabled])',
	'textarea:not([disabled])',
	'[tabindex]:not([tabindex="-1"])',
].join(', ');

// React applies autoFocus to a prompt-owned input during commit, before this
// component's mount effects run, so the opener that was focused by the opening
// click cannot always be recovered from document.activeElement. This tracker
// (installed with the module, before any prompt can open) records the last
// element that received focus outside the open prompt; it becomes the focus
// restore fallback when the dialog closed with its own input focused.
const dialogFocusTracker: { overlay: HTMLElement | null; lastOutside: HTMLElement | null } = {
	overlay: null,
	lastOutside: null,
};

if (typeof document !== 'undefined') {
	document.addEventListener('focusin', (event) => {
		const target = event.target;
		if (!(target instanceof HTMLElement)) {
			return;
		}
		// The browser moves focus to a prompt-owned autoFocus input during the
		// commit that inserts the portal, before the overlay ref registers the
		// subtree with this tracker. The dialog card's role attribute is already
		// set at that point, so a focus landing inside any dialog never poisons
		// the restore target.
		if (dialogFocusTracker.overlay?.contains(target) || target.closest('[role="dialog"]')) {
			return;
		}
		dialogFocusTracker.lastOutside = target;
	});
}

interface PromptDialogShellProps {
	confirmClassName?: string;
	confirmDisabled?: boolean;
	confirmLabel?: string;
	description?: string | null;
	onCancel: () => void;
	onConfirm: () => void;
	title: string;
	children: ReactNode;
}

export function PromptDialogShell({
	children,
	confirmClassName = 'rounded-lg bg-brand-600 px-4 py-1.5 text-xs font-semibold text-white transition hover:bg-brand-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:cursor-not-allowed disabled:opacity-60',
	confirmDisabled = false,
	confirmLabel = 'Republish',
	description = null,
	onCancel,
	onConfirm,
	title,
}: PromptDialogShellProps) {
	const dialogRef = useRef<HTMLDivElement>(null);
	const cancelButtonRef = useRef<HTMLButtonElement>(null);
	const onCancelRef = useRef(onCancel);
	const overlayRef = useRef<HTMLDivElement | null>(null);

	useEffect(() => {
		onCancelRef.current = onCancel;
	}, [onCancel]);

	// Registers the open prompt's overlay with the focus tracker so that focus
	// inside the prompt is excluded from restore-target candidates.
	const setOverlayRef = useCallback((node: HTMLDivElement | null) => {
		const previous = overlayRef.current;
		overlayRef.current = node;
		if (node) {
			dialogFocusTracker.overlay = node;
		} else if (dialogFocusTracker.overlay === previous) {
			dialogFocusTracker.overlay = null;
		}
	}, []);

	// Runs once on open and must not depend on callbacks; otherwise any parent
	// re-render (e.g. typing in a dialog-owned input) re-runs the teardown,
	// flickers `inert`, and re-captures the dialog's own focused element as the
	// restore target.
	useLayoutEffect(() => {
		const dialogCard = dialogRef.current;
		const overlay = overlayRef.current;
		const previousActiveElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;
		const appRoot = document.getElementById('root');
		const cancelButton = cancelButtonRef.current;

		appRoot?.setAttribute('inert', '');

		if (dialogCard && !dialogCard.contains(document.activeElement)) {
			cancelButton?.focus();
		}

		const trapFocus = (event: KeyboardEvent) => {
			if (event.key !== 'Tab' || !dialogCard) {
				return;
			}

			const focusable = Array.from(dialogCard.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR));
			if (focusable.length === 0) {
				return;
			}

			const first = focusable[0];
			const last = focusable[focusable.length - 1];
			const active = document.activeElement;

			if (event.shiftKey) {
				if (active === first || !dialogCard.contains(active)) {
					event.preventDefault();
					last.focus();
				}
			} else if (active === last || !dialogCard.contains(active)) {
				event.preventDefault();
				first.focus();
			}
		};

		const handleKeyDown = (event: KeyboardEvent) => {
			if (event.key === 'Escape') {
				onCancelRef.current();
				return;
			}

			trapFocus(event);
		};

		document.addEventListener('keydown', handleKeyDown);
		return () => {
			document.removeEventListener('keydown', handleKeyDown);
			if (dialogFocusTracker.overlay === overlay) {
				dialogFocusTracker.overlay = null;
			}
			appRoot?.removeAttribute('inert');
			let restoreTo: HTMLElement | null = null;
			if (previousActiveElement?.isConnected && !dialogCard?.contains(previousActiveElement)) {
				restoreTo = previousActiveElement;
			}
			// With an autoFocus input the captured element is the dialog's own
			// input; restore to the last element focused outside the prompt.
			const fallback = dialogFocusTracker.lastOutside;
			if (!restoreTo && fallback?.isConnected) {
				restoreTo = fallback;
			}
			if (!restoreTo) {
				return;
			}
			// The restore must not run synchronously. Closing via an Enter
			// keydown executes this cleanup inside that keydown dispatch; a
			// synchronous restore would move focus to the opener before the
			// browser runs Enter's activation default action, which then
			// re-clicks the focused opener and reopens the prompt instantly.
			// Deferring to the next task lets the keydown finish first.
			window.setTimeout(() => {
				if (restoreTo?.isConnected) {
					restoreTo.focus();
				}
			}, 0);
		};
	}, []);

	return createPortal(
		<div
			ref={setOverlayRef}
			className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4"
			role="presentation"
			onClick={onCancel}
		>
			<div
				ref={dialogRef}
				aria-label={title}
				aria-modal="true"
				className="w-full max-w-md overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl dark:border-slate-800 dark:bg-slate-900"
				role="dialog"
				onClick={(event) => event.stopPropagation()}
			>
				<div className="border-b border-slate-200 px-5 py-4 dark:border-slate-800">
					<h2 className="text-sm font-semibold text-slate-900 dark:text-white">{title}</h2>
					{description ? (
						<p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{description}</p>
					) : null}
				</div>

				<div className="p-5">{children}</div>

				<div className="flex items-center justify-end gap-2 border-t border-slate-200 bg-slate-50/80 px-5 py-3 dark:border-slate-800 dark:bg-slate-950/40">
					<button
						ref={cancelButtonRef}
						type="button"
						className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-600 transition hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:border-zinc-800 dark:bg-zinc-900 dark:text-slate-300 dark:hover:bg-zinc-800"
						onClick={onCancel}
					>
						Cancel
					</button>
					<button
						type="button"
						className={confirmClassName}
						disabled={confirmDisabled}
						onClick={onConfirm}
					>
						{confirmLabel}
					</button>
				</div>
			</div>
		</div>,
		document.body,
	);
}

interface RepublishManualIdPromptProps {
	messageId: string | null;
	onCancel: () => void;
	onConfirm: (manualMessageId: string) => void;
}

export function RepublishManualIdPrompt({
	messageId,
	onCancel,
	onConfirm,
}: RepublishManualIdPromptProps) {
	const [value, setValue] = useState('');
	const trimmedValue = value.trim();

	return (
		<PromptDialogShell
			confirmDisabled={trimmedValue.length === 0}
			description="The Manual strategy sends the republished copy under the message id you enter here."
			onCancel={onCancel}
			onConfirm={() => {
				if (trimmedValue.length > 0) {
					onConfirm(trimmedValue);
				}
			}}
			title="Manual message id"
		>
			<p className="text-xs text-slate-500 dark:text-slate-400">
				Current message id:
				<span className="mt-1 block break-all font-mono text-slate-700 dark:text-slate-200">{messageId ?? 'n/a'}</span>
			</p>
			<label
				htmlFor="republish-manual-message-id"
				className="mt-3 block text-[0.6875rem] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500"
			>
				New message id
			</label>
			<input
				id="republish-manual-message-id"
				type="text"
				autoFocus
				maxLength={128}
				className="mt-1 w-full rounded-lg border border-slate-200 bg-white px-3 py-2 font-mono text-xs text-slate-700 transition placeholder:text-slate-400 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-100 dark:placeholder:text-slate-500"
				placeholder="e.g. 3f2b1a4d-9c8e-4f6a-b5d7-2e1c0a9b8d7f"
				value={value}
				onChange={(event) => setValue(event.target.value)}
				onKeyDown={(event) => {
					if (event.key === 'Enter' && trimmedValue.length > 0) {
						onConfirm(trimmedValue);
					}
				}}
			/>
		</PromptDialogShell>
	);
}

interface RepublishAllConfirmPromptProps {
	onCancel: () => void;
	onConfirm: () => void;
	visibleCount: number;
}

export function RepublishAllConfirmPrompt({
	onCancel,
	onConfirm,
	visibleCount,
}: RepublishAllConfirmPromptProps) {
	return (
		<PromptDialogShell
			confirmLabel="Republish All"
			description="This republishes every dead-lettered message in the selected entity's dead-letter queue back to the original entity."
			onCancel={onCancel}
			onConfirm={onConfirm}
			title="Republish all dead-lettered messages?"
		>
			<p className="text-xs text-slate-500 dark:text-slate-400">
				{visibleCount} message{visibleCount === 1 ? '' : 's'}{' '}
				{visibleCount === 1 ? 'is' : 'are'} currently visible. The republish applies
				to the entire dead-letter queue, including messages beyond the visible
				page.
			</p>
		</PromptDialogShell>
	);
}
