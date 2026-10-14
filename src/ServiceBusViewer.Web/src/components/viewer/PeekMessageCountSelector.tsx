import { useState, type ChangeEvent } from 'react';
import {
	PEEK_MESSAGE_COUNT_MAX,
	PEEK_MESSAGE_COUNT_MIN,
	PEEK_MESSAGE_COUNT_PRESETS,
	parsePeekMessageCount,
} from '../../lib/peekMessageCount';
import { PromptDialogShell } from './RepublishPrompts';

const CUSTOM_OPTION = 'custom';

interface PeekMessageCountSelectorProps {
	value: number;
	disabled?: boolean;
	onApply: (peekMessageCount: number) => void;
}

/** Peek-message-count selector with preset options and a validated custom-value prompt. */
export function PeekMessageCountSelector({ value, disabled, onApply }: PeekMessageCountSelectorProps) {
	const [customValues, setCustomValues] = useState<number[]>(() =>
		(PEEK_MESSAGE_COUNT_PRESETS as readonly number[]).includes(value) ? [] : [value],
	);
	const [isPromptOpen, setIsPromptOpen] = useState(false);
	const [customInput, setCustomInput] = useState('');
	const [validationMessage, setValidationMessage] = useState<string | null>(null);

	const handleSelectChange = (event: ChangeEvent<HTMLSelectElement>) => {
		const selected = event.target.value;

		if (selected === CUSTOM_OPTION) {
			setCustomInput('');
			setValidationMessage(null);
			setIsPromptOpen(true);
			return;
		}

		const selectedCount = Number(selected);
		if (selectedCount !== value) {
			onApply(selectedCount);
		}
	};

	const handlePromptConfirm = () => {
		const parsed = parsePeekMessageCount(customInput);
		if (parsed === null) {
			setValidationMessage(
				`Enter a whole number from ${PEEK_MESSAGE_COUNT_MIN} through ${PEEK_MESSAGE_COUNT_MAX}.`,
			);
			return;
		}

		setIsPromptOpen(false);

		if (!(PEEK_MESSAGE_COUNT_PRESETS as readonly number[]).includes(parsed)) {
			setCustomValues([parsed]);
		}

		if (parsed !== value) {
			onApply(parsed);
		}
	};

	return (
		<div className="flex items-center gap-2">
			<span aria-hidden="true" className="h-4 w-px bg-slate-200 dark:bg-zinc-700" />
			<label
				htmlFor="peek-message-count"
				className="text-[0.6875rem] font-medium uppercase tracking-[0.14em] text-slate-400 dark:text-slate-500"
			>
				Peek top
			</label>
			<select
				id="peek-message-count"
				className="w-30 rounded-lg border border-slate-200 bg-white px-2 py-1.5 text-xs text-slate-700 shadow-sm transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 disabled:cursor-not-allowed disabled:opacity-60 dark:border-zinc-800 dark:bg-zinc-900 dark:text-slate-200"
				value={String(value)}
				disabled={disabled}
				onChange={handleSelectChange}
			>
				{PEEK_MESSAGE_COUNT_PRESETS.map((preset) => (
					<option key={preset} value={preset}>
						{preset}
					</option>
				))}
				{customValues.map((custom) => (
					<option key={custom} value={custom}>
						{custom} (custom)
					</option>
				))}
				<option value={CUSTOM_OPTION}>Custom…</option>
			</select>

			{isPromptOpen ? (
				<PromptDialogShell
					confirmDisabled={customInput.trim().length === 0}
					confirmLabel="Apply"
					description={`Choose how many messages the viewer peeks: a whole number from ${PEEK_MESSAGE_COUNT_MIN} through ${PEEK_MESSAGE_COUNT_MAX}.`}
					onCancel={() => setIsPromptOpen(false)}
					onConfirm={handlePromptConfirm}
					title="Custom peek count"
				>
					<label
						htmlFor="custom-peek-message-count"
						className="block text-[0.6875rem] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500"
					>
						Peek message count
					</label>
					<input
						id="custom-peek-message-count"
						type="text"
						inputMode="numeric"
						autoFocus
						aria-invalid={validationMessage !== null}
						className="mt-1 w-full rounded-lg border border-slate-200 bg-white px-3 py-2 font-mono text-xs text-slate-700 transition placeholder:text-slate-400 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-100 dark:placeholder:text-slate-500"
						placeholder={`${PEEK_MESSAGE_COUNT_MIN}-${PEEK_MESSAGE_COUNT_MAX}`}
						value={customInput}
						onChange={(event) => {
							setCustomInput(event.target.value);
							setValidationMessage(null);
						}}
						onKeyDown={(event) => {
							if (event.key === 'Enter' && customInput.trim().length > 0) {
								handlePromptConfirm();
							}
						}}
					/>
					{validationMessage ? (
						<p className="mt-2 text-[0.6875rem] text-rose-600 dark:text-rose-400" role="alert">
							{validationMessage}
						</p>
					) : null}
				</PromptDialogShell>
			) : null}
		</div>
	);
}
