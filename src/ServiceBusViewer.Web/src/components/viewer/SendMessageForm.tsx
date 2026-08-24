import { useEffect, useMemo, useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import { Alert } from '../common/Alert';
import { serviceBusApi } from '../../api/serviceBusApi';
import { useStoredBoolean } from '../../hooks/useStoredBoolean';
import { cx } from '../../lib/cx';
import { parseSendWindowJson } from '../../lib/sendWindowJsonImport';
import type {
	ApplicationPropertyInputDto,
	ApplicationPropertyType,
	SendMessagePropertiesDto,
	SendMessageRequestDto,
} from '../../types/serviceBus';

const commonContentTypes = [
	'application/json',
	'text/plain',
	'text/html',
	'application/xml',
	'text/xml',
	'application/x-www-form-urlencoded',
	'text/csv',
] as const;

const applicationPropertyTypes = [
	'String',
	'Bool',
	'Byte',
	'SByte',
	'Short',
	'UShort',
	'Int',
	'UInt',
	'Long',
	'ULong',
	'Float',
	'Double',
	'Decimal',
	'Char',
	'Guid',
	'DateTime',
	'DateTimeOffset',
	'TimeSpan',
] as const satisfies readonly ApplicationPropertyType[];

interface SendMessageFormProps {
	canSend: boolean;
	isSubmitting: boolean;
	requiresSession: boolean;
	entityName: string | null;
	topicName: string | null;
	sendResultMessage: string | null;
	sendErrorMessages: string[];
	onSubmit: (request: SendMessageRequestDto) => Promise<void>;
	onValidationError: (messages: string[]) => void;
}

const emptyProperties: SendMessagePropertiesDto = {
	contentType: '',
	correlationId: '',
	messageId: '',
	scheduledEnqueueTime: null,
	sessionId: '',
	timeToLive: '',
	to: '',
	replyTo: '',
	subject: '',
	partitionKey: '',
};

function createProperty(): ApplicationPropertyInputDto {
	return { key: '', type: 'String', value: '' };
}

const messageIdFieldClasses =
	'block w-full rounded-xl border bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20';
const messageIdFieldNormalClasses =
	'border-slate-200 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:focus:border-brand-400';
const messageIdFieldWarningClasses =
	'border-amber-400 bg-amber-50/60 focus:border-amber-500 focus:ring-2 focus:ring-amber-500/20 dark:border-amber-600/70 dark:bg-amber-950/20 dark:focus:border-amber-500';

export function SendMessageForm({
	canSend,
	isSubmitting,
	requiresSession,
	entityName,
	topicName,
	sendResultMessage,
	sendErrorMessages,
	onSubmit,
	onValidationError,
}: SendMessageFormProps) {
	const [isOpen, setIsOpen] = useStoredBoolean('sendFormOpen', true);
	const [showAdvanced, setShowAdvanced] = useStoredBoolean('sendFormAdvanced', false);
	const [messageBody, setMessageBody] = useState('');
	const [messageProperties, setMessageProperties] =
		useState<SendMessagePropertiesDto>(emptyProperties);
	const [applicationProperties, setApplicationProperties] = useState<
		ApplicationPropertyInputDto[]
	>([]);
	const [isContentTypeMenuOpen, setIsContentTypeMenuOpen] = useState(false);
	const [contentTypeFilter, setContentTypeFilter] = useState('');
	const contentTypeContainerRef = useRef<HTMLDivElement | null>(null);
	const [duplicateDetection, setDuplicateDetection] = useState<boolean | null>(null);
	const [loadJsonStatus, setLoadJsonStatus] = useState<{
		kind: 'error';
		message: string;
	} | null>(null);
	const [loadJsonStatusFading, setLoadJsonStatusFading] = useState(false);
	const loadJsonStatusTimerRef = useRef<number | null>(null);
	const loadJsonFadeTimerRef = useRef<number | null>(null);
	const [duplicateDetectionArmed, setDuplicateDetectionArmed] = useState(false);
	const [pasteStatus, setPasteStatus] = useState<'idle' | 'success' | 'invalid'>('idle');
	const pasteStatusTimerRef = useRef<number | null>(null);

	const clearPasteStatusTimers = () => {
		if (pasteStatusTimerRef.current !== null) {
			window.clearTimeout(pasteStatusTimerRef.current);
			pasteStatusTimerRef.current = null;
		}
	};

	const clearPasteStatus = () => {
		clearPasteStatusTimers();
		setPasteStatus('idle');
	};

	const showPasteStatus = (status: 'success' | 'invalid') => {
		clearPasteStatusTimers();
		setPasteStatus(status);
		pasteStatusTimerRef.current = window.setTimeout(() => {
			pasteStatusTimerRef.current = null;
			setPasteStatus('idle');
		}, 3000);
	};

	const clearLoadJsonStatusTimers = () => {
		if (loadJsonStatusTimerRef.current !== null) {
			window.clearTimeout(loadJsonStatusTimerRef.current);
			loadJsonStatusTimerRef.current = null;
		}
		if (loadJsonFadeTimerRef.current !== null) {
			window.clearTimeout(loadJsonFadeTimerRef.current);
			loadJsonFadeTimerRef.current = null;
		}
	};

	const clearLoadJsonStatus = () => {
		clearLoadJsonStatusTimers();
		setLoadJsonStatus(null);
		setLoadJsonStatusFading(false);
	};

	const showLoadJsonError = (message: string) => {
		clearLoadJsonStatusTimers();
		setLoadJsonStatus({ kind: 'error', message });
		setLoadJsonStatusFading(false);
		loadJsonStatusTimerRef.current = window.setTimeout(() => {
			loadJsonStatusTimerRef.current = null;
			setLoadJsonStatusFading(true);
			loadJsonFadeTimerRef.current = window.setTimeout(() => {
				loadJsonFadeTimerRef.current = null;
				setLoadJsonStatus(null);
				setLoadJsonStatusFading(false);
			}, 500);
		}, 5000);
	};

	useEffect(() => {
		return () => {
			clearLoadJsonStatusTimers();
			clearPasteStatusTimers();
		};
	}, []);

	const duplicateDetectionWarning = duplicateDetection === true && duplicateDetectionArmed;

	const messageIdValueRef = useRef(messageProperties.messageId);

	useEffect(() => {
		messageIdValueRef.current = messageProperties.messageId;
	}, [messageProperties.messageId]);

	useEffect(() => {
		if (duplicateDetection === true && messageIdValueRef.current.length > 0) {
			setDuplicateDetectionArmed(true);
		}
	}, [duplicateDetection]);

	useEffect(() => {
		setDuplicateDetectionArmed(false);
		setDuplicateDetection(null);

		const detailsType = topicName ? 'Topic' : 'Queue';
		const detailsName = topicName ?? entityName;
		if (!detailsName) {
			return;
		}

		let isDisposed = false;
		void serviceBusApi
			.getEntityDetails(detailsType, detailsName, null)
			.then((details) => {
				if (isDisposed || details.properties === null) {
					return;
				}
				const requiresDuplicate =
					'requiresDuplicateDetection' in details.properties
						? Boolean(details.properties.requiresDuplicateDetection)
						: false;
				setDuplicateDetection(requiresDuplicate);
			})
			.catch(() => {
				if (!isDisposed) {
					setDuplicateDetection(null);
				}
			});

		return () => {
			isDisposed = true;
		};
	}, [entityName, topicName]);

	useEffect(() => {
		if (!isContentTypeMenuOpen) {
			return;
		}

		const handleClick = (event: MouseEvent) => {
			if (!(event.target instanceof Node)) {
				return;
			}

			if (!contentTypeContainerRef.current?.contains(event.target)) {
				setIsContentTypeMenuOpen(false);
			}
		};

		document.addEventListener('click', handleClick);
		return () => document.removeEventListener('click', handleClick);
	}, [isContentTypeMenuOpen]);

	const filteredContentTypes = useMemo(() => {
		const query = contentTypeFilter.trim().toLowerCase();
		if (!query) {
			return [...commonContentTypes];
		}

		return commonContentTypes.filter((contentType) =>
			contentType.toLowerCase().includes(query),
		);
	}, [contentTypeFilter]);

	const updateProperty = (
		index: number,
		key: keyof ApplicationPropertyInputDto,
		value: string,
	) => {
		setApplicationProperties((current) =>
			current.map((property, propertyIndex) =>
				propertyIndex === index ? { ...property, [key]: value } : property,
			),
		);
	};

	const validate = (): string[] => {
		const errors: string[] = [];
		if (messageBody.trim().length === 0) {
			errors.push('Message body cannot be empty.');
		}

		if (messageProperties.messageId.length > 128) {
			errors.push('The Message ID must be 128 characters or fewer.');
		}

		if (messageProperties.sessionId.length > 128) {
			errors.push('The Session ID must be 128 characters or fewer.');
		}

		if (requiresSession && messageProperties.sessionId.trim().length === 0) {
			errors.push('A Session ID is required when sending to a session-enabled queue or topic.');
		}

		if (messageProperties.correlationId.length > 128) {
			errors.push('The Correlation ID must be 128 characters or fewer.');
		}

		if (messageProperties.partitionKey.length > 128) {
			errors.push('The Partition Key must be 128 characters or fewer.');
		}

		return errors;
	};

	const resetForm = () => {
		setMessageBody('');
		setMessageProperties(emptyProperties);
		setApplicationProperties([]);
		setIsContentTypeMenuOpen(false);
		setContentTypeFilter('');
		clearLoadJsonStatus();
		clearPasteStatus();
		// A successful send is the warning reset: do not re-arm here.
	};

	const handlePropertiesChange = (
		key: keyof SendMessagePropertiesDto,
		event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
	) => {
		const value = event.target.value;
		setMessageProperties((current) => ({
			...current,
			[key]: key === 'scheduledEnqueueTime' ? (value ? value : null) : value,
		}));
		if (key === 'contentType') {
			setContentTypeFilter(value);
			setIsContentTypeMenuOpen(true);
		}
		if (key === 'messageId' && duplicateDetectionWarning) {
			setDuplicateDetectionArmed(false);
		}
	};

	const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		const errors = validate();
		if (errors.length > 0) {
			onValidationError(errors);
			return;
		}

		clearLoadJsonStatus();

		try {
			await onSubmit({
				sendMessageApplicationProperties: applicationProperties,
				sendMessageBody: messageBody,
				sendMessageProperties: {
					...messageProperties,
					scheduledEnqueueTime: messageProperties.scheduledEnqueueTime || null,
				},
			});
		} catch {
			return;
		}

		setDuplicateDetectionArmed(false);
		resetForm();
	};

	const handleLoadJson = async () => {
		clearLoadJsonStatus();
		setPasteStatus('idle');
		let pastedText: string;
		try {
			pastedText = await navigator.clipboard.readText();
		} catch {
			showLoadJsonError(
				'Clipboard access was blocked. Allow clipboard access and try again.',
			);
			showPasteStatus('invalid');
			return;
		}

		if (pastedText.length === 0) {
			showLoadJsonError('The clipboard is empty. Copy a full message as JSON first.');
			showPasteStatus('invalid');
			return;
		}

		const result = parseSendWindowJson(pastedText);
		if (!result.ok) {
			showLoadJsonError(result.error);
			showPasteStatus('invalid');
			return;
		}

		if (result.value.body !== null) {
			setMessageBody(result.value.body);
		}

		const importedProperties = result.value.properties;
		if (importedProperties && Object.keys(importedProperties).length > 0) {
			setMessageProperties((current) => ({ ...current, ...importedProperties }));
		}

		const importedApplicationProperties = result.value.applicationProperties;
		if (importedApplicationProperties) {
			setApplicationProperties(importedApplicationProperties);
		}

		const pastedMessageId = result.value.properties?.messageId;
		if (duplicateDetection === true && pastedMessageId !== undefined && pastedMessageId.length > 0) {
			setDuplicateDetectionArmed(true);
		}

		showPasteStatus('success');
	};

	return (
		<section className="mb-6 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm shadow-slate-200/40 dark:border-slate-800 dark:bg-slate-900 dark:shadow-black/20">
			<div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
				<h2 className="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">
					Send Message
				</h2>
				<div className="flex min-w-0 flex-wrap items-center justify-end gap-2">
					{isOpen && loadJsonStatus ? (
						<span
							role="alert"
							className={cx(
								'max-w-full truncate text-xs font-medium text-rose-700 transition-opacity duration-500 dark:text-rose-300',
								loadJsonStatusFading && 'opacity-0',
							)}
						>
							{loadJsonStatus.message}
						</span>
					) : null}
					<button
						type="button"
						disabled={!isOpen}
						className={cx(
							'inline-flex min-w-[10.5rem] items-center gap-1.5 rounded-lg border px-2.5 py-1 text-[11px] font-medium transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500',
							!isOpen
								? 'cursor-not-allowed border-slate-200 bg-white text-slate-500 opacity-40 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300'
								: pasteStatus === 'success'
									? 'border-emerald-300 bg-emerald-50 text-emerald-700 dark:border-emerald-900/60 dark:bg-emerald-950/30 dark:text-emerald-400'
									: pasteStatus === 'invalid'
										? 'border-rose-300 bg-rose-50 text-rose-700 dark:border-rose-900/60 dark:bg-rose-950/30 dark:text-rose-400'
										: 'border-slate-200 bg-white text-slate-500 hover:border-slate-300 hover:text-slate-700 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300 dark:hover:border-slate-700 dark:hover:text-white',
						)}
						onClick={() => void handleLoadJson()}
						title="Paste the full message JSON from the clipboard into this form"
					>
						<span className="material-icons-round text-sm">content_paste</span>
						{pasteStatus === 'success'
							? 'Successful'
							: pasteStatus === 'invalid'
								? 'Not valid JSON'
								: 'Paste From Clipboard'}
					</button>
					<button
						type="button"
						role="switch"
						aria-checked={showAdvanced}
						aria-label="Toggle advanced fields"
						title="Show or hide advanced fields"
						disabled={!isOpen}
						className={cx(
							'inline-flex h-7 items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-2.5 text-[11px] font-medium transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:border-slate-800 dark:bg-slate-900',
							showAdvanced
								? 'text-slate-700 dark:text-slate-200'
								: 'text-slate-500 dark:text-slate-300',
							!isOpen &&
							'cursor-not-allowed opacity-40 hover:border-slate-200 dark:hover:border-slate-800',
						)}
						onClick={() => setShowAdvanced((current) => !current)}
					>
						<span
							aria-hidden="true"
							className={cx(
								'relative inline-flex h-4 w-7 items-center rounded-full transition-colors',
								showAdvanced ? 'bg-brand-600' : 'bg-slate-300 dark:bg-slate-600',
							)}
						>
							<span
								className={cx(
									'absolute h-3 w-3 rounded-full bg-white shadow transition-transform',
									showAdvanced ? 'translate-x-3.5' : 'translate-x-0.5',
								)}
							/>
						</span>
						Advanced
					</button>
					<button
						type="button"
						className="inline-flex h-7 w-24 items-center justify-center gap-1 rounded-lg bg-slate-100 px-2 text-[11px] font-medium text-slate-500 transition hover:bg-slate-200 hover:text-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700 dark:hover:text-white"
						onClick={() => setIsOpen((current) => !current)}
					>
						<span className="material-icons-round text-sm">
							{isOpen ? 'keyboard_arrow_up' : 'keyboard_arrow_down'}
						</span>
						<span className="w-14 text-center">{isOpen ? 'Collapse' : 'Expand'}</span>
					</button>
				</div>
			</div>

			<div className={cx('send-form-transition', !isOpen && 'js-send-form-hidden')}>
				<form className="p-4 lg:p-6" onSubmit={(event) => void handleSubmit(event)}>
					<div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_19rem]">
						<div className="flex min-h-0 flex-col">
							<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
								Payload
							</label>
							<textarea
								className="block min-h-56 w-full flex-1 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 font-mono text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition placeholder:text-slate-400 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:placeholder:text-slate-500 dark:focus:border-brand-400"
								rows={12}
								value={messageBody}
								onChange={(event) => setMessageBody(event.target.value)}
							/>
						</div>

						<div className="space-y-4">
							<div>
								<label className="mb-2 flex items-center justify-between pr-1.5 text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
									Message ID
									{duplicateDetection === true ? (
										<span
											id="messageIdDuplicateDetectionWarning"
											className={cx(
												'flex items-center gap-1 normal-case tracking-normal text-xs font-medium',
												duplicateDetectionWarning
													? 'text-amber-700 dark:text-amber-400'
													: 'text-slate-500 dark:text-slate-400',
											)}
										>
											<span className="material-icons-round text-sm" aria-hidden="true">
												{duplicateDetectionWarning ? 'warning_amber' : 'info'}
											</span>
											<span>Duplicate detection enabled</span>
										</span>
									) : null}
								</label>
								<input
									type="text"
									maxLength={128}
									aria-invalid={duplicateDetectionWarning || undefined}
									aria-describedby={duplicateDetection === true ? 'messageIdDuplicateDetectionWarning' : undefined}
									className={cx(
										messageIdFieldClasses,
										duplicateDetectionWarning
											? messageIdFieldWarningClasses
											: messageIdFieldNormalClasses,
									)}
									value={messageProperties.messageId}
									onChange={(event) => handlePropertiesChange('messageId', event)}
								/>
							</div>

							<div>
								<label className="mb-2 flex items-center gap-2 text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
									<span>Session ID</span>
								</label>
								<input
									type="text"
									maxLength={128}
									required={requiresSession}
									aria-required={requiresSession}
									placeholder={
										requiresSession ? 'Required for session-enabled entities' : 'Optional'
									}
									className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
									value={messageProperties.sessionId}
									onChange={(event) => handlePropertiesChange('sessionId', event)}
								/>
							</div>

							{showAdvanced ? (
								<div>
									<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
										Partition Key
									</label>
									<input
										type="text"
										maxLength={128}
										placeholder="Optional"
										className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
										value={messageProperties.partitionKey}
										onChange={(event) => handlePropertiesChange('partitionKey', event)}
									/>
								</div>
							) : null}

							<div>
								<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
									Correlation ID
								</label>
								<input
									type="text"
									maxLength={128}
									className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
									value={messageProperties.correlationId}
									onChange={(event) => handlePropertiesChange('correlationId', event)}
								/>
							</div>

							<div>
								<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
									Content Type
								</label>
								<div className="relative" ref={contentTypeContainerRef}>
									<div className="flex items-stretch overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm shadow-slate-200/40 transition focus-within:border-brand-500 focus-within:ring-2 focus-within:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:shadow-black/20 dark:focus-within:border-brand-400">
										<input
											type="text"
											autoComplete="off"
											className="min-w-0 flex-1 border-0 bg-transparent px-3 py-2 text-sm text-slate-700 focus:ring-0 dark:text-slate-100"
											value={messageProperties.contentType}
											onChange={(event) => handlePropertiesChange('contentType', event)}
											onFocus={() => setIsContentTypeMenuOpen(true)}
											onKeyDown={(event) => {
												if (event.key === 'Escape') {
													setIsContentTypeMenuOpen(false);
												}
											}}
										/>
										<button
											type="button"
											className="inline-flex w-11 items-center justify-center border-l border-slate-200 bg-slate-50 text-slate-600 transition hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-none dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-white"
											aria-expanded={isContentTypeMenuOpen}
											aria-controls="contentTypePickerMenu"
											aria-label="Toggle common content types"
											title="Toggle common content types"
											onClick={() => setIsContentTypeMenuOpen((current) => !current)}
										>
											<svg
												className="h-4 w-4"
												viewBox="0 0 24 24"
												fill="none"
												stroke="currentColor"
												strokeWidth="1.8"
												aria-hidden="true"
											>
												<path strokeLinecap="round" strokeLinejoin="round" d="m6 9 6 6 6-6" />
											</svg>
										</button>
									</div>

									<div
										id="contentTypePickerMenu"
										className={cx(
											'absolute left-0 right-0 top-full z-20 mt-2 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl shadow-slate-200/60 dark:border-slate-800 dark:bg-slate-900 dark:shadow-black/30',
											!isContentTypeMenuOpen && 'hidden',
										)}
									>
										<div className="border-b border-slate-200 px-3 py-2 text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:border-slate-800 dark:text-slate-400">
											Common content types
										</div>
										<div className="custom-scrollbar h-48 overflow-y-scroll p-2 pr-2" style={{ scrollbarGutter: 'stable' }}>
											<div className="grid gap-1.5">
												{filteredContentTypes.map((contentType) => (
													<button
														key={contentType}
														type="button"
														className="flex w-full items-center rounded-lg px-3 py-2 text-left text-sm font-medium text-slate-700 transition hover:bg-brand-50 hover:text-brand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:text-slate-200 dark:hover:bg-brand-950/30 dark:hover:text-brand-300"
														onClick={() => {
															setMessageProperties((current) => ({ ...current, contentType }));
															setContentTypeFilter(contentType);
															setIsContentTypeMenuOpen(false);
														}}
													>
														<span>{contentType}</span>
													</button>
												))}
											</div>
											{filteredContentTypes.length === 0 ? (
												<div className="px-3 py-4 text-sm text-slate-500 dark:text-slate-400">
													No matching content types.
												</div>
											) : null}
										</div>
									</div>
								</div>
							</div>
						</div>
					</div>

					<div className="mt-6 grid gap-4 lg:grid-cols-2">
						<div>
							<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
								Scheduled Enqueue Time
							</label>
							<input
								type="datetime-local"
								className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
								value={messageProperties.scheduledEnqueueTime ?? ''}
								onChange={(event) => handlePropertiesChange('scheduledEnqueueTime', event)}
							/>
						</div>

						<div>
							<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
								Time To Live
							</label>
							<input
								type="text"
								placeholder="hh:mm:ss"
								className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
								value={messageProperties.timeToLive}
								onChange={(event) => handlePropertiesChange('timeToLive', event)}
							/>
						</div>
					</div>

					{showAdvanced ? (
						<div className="mt-6 grid gap-4 sm:grid-cols-3">
							<div>
								<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
									To
								</label>
								<input
									type="text"
									placeholder="Optional"
									className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
									value={messageProperties.to}
									onChange={(event) => handlePropertiesChange('to', event)}
								/>
							</div>

							<div>
								<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
									Reply To
								</label>
								<input
									type="text"
									placeholder="Optional"
									className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
									value={messageProperties.replyTo}
									onChange={(event) => handlePropertiesChange('replyTo', event)}
								/>
							</div>

							<div>
								<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
									Subject
								</label>
								<input
									type="text"
									placeholder="Optional"
									className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
									value={messageProperties.subject}
									onChange={(event) => handlePropertiesChange('subject', event)}
								/>
							</div>
						</div>
					) : null}

					<div className="mt-6">
						<div className="mb-3 flex flex-wrap items-center justify-between gap-3">
							<div>
								<label className="block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
									Custom Properties
								</label>
								<p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
									Optional typed key-value metadata to attach to the outgoing message.
								</p>
							</div>
							<div className="flex flex-wrap items-center gap-2">
								<button
									type="button"
									className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs font-medium text-slate-700 transition hover:border-slate-300 hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:border-slate-700 dark:hover:bg-slate-800"
									onClick={() => setApplicationProperties((current) => [...current, createProperty()])}
								>
									<svg
										className="h-4 w-4"
										viewBox="0 0 24 24"
										fill="none"
										stroke="currentColor"
										strokeWidth="1.8"
										aria-hidden="true"
									>
										<path strokeLinecap="round" strokeLinejoin="round" d="M12 5v14M5 12h14" />
									</svg>
									Add Property
								</button>
							</div>
						</div>

						<div className="space-y-3">
							{applicationProperties.map((property, index) => (
								<div
									key={`property-${index}`}
									className="grid gap-3 rounded-2xl border border-slate-200 bg-slate-50/80 p-3 dark:border-slate-800 dark:bg-slate-950/60 xl:grid-cols-[minmax(0,1fr)_11rem_minmax(0,1fr)_auto]"
								>
									<input
										type="text"
										placeholder="Key"
										className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
										value={property.key}
										onChange={(event) => updateProperty(index, 'key', event.target.value)}
									/>
									<select
										className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
										value={property.type}
										onChange={(event) => updateProperty(index, 'type', event.target.value)}
									>
										{applicationPropertyTypes.map((type) => (
											<option key={type} value={type}>
												{type}
											</option>
										))}
									</select>
									<input
										type="text"
										placeholder="Value"
										className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
										value={property.value}
										onChange={(event) => updateProperty(index, 'value', event.target.value)}
									/>
									<button
										type="button"
										className="inline-flex items-center justify-center rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm font-medium text-rose-700 transition hover:bg-rose-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-500 dark:border-rose-900/70 dark:bg-rose-950/40 dark:text-rose-300 dark:hover:bg-rose-950/70"
										onClick={() =>
											setApplicationProperties((current) =>
												current.filter((_, propertyIndex) => propertyIndex !== index),
											)
										}
									>
										Remove
									</button>
								</div>
							))}
						</div>
					</div>

					<div className="mt-6 flex flex-wrap items-center justify-end gap-3">
						{sendErrorMessages.length > 0 ? (
							<Alert
								className="min-w-0 flex-1 basis-full lg:basis-auto"
								messages={sendErrorMessages}
								tone="error"
							/>
						) : (
							<Alert
								className="min-w-0 flex-1 basis-full lg:basis-auto"
								messages={sendResultMessage ? [sendResultMessage] : []}
								tone="success"
							/>
						)}
						<button
							type="submit"
							className="inline-flex items-center gap-2 rounded-lg bg-brand-600 px-4 py-3 text-sm font-semibold text-white shadow-lg shadow-brand-950/20 transition hover:bg-brand-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:cursor-not-allowed disabled:opacity-60"
							disabled={!canSend || isSubmitting}
						>
							<svg
								className="h-4 w-4"
								viewBox="0 0 24 24"
								fill="none"
								stroke="currentColor"
								strokeWidth="1.8"
								aria-hidden="true"
							>
								<path strokeLinecap="round" strokeLinejoin="round" d="M5 12h11M12 5l7 7-7 7" />
							</svg>
							{isSubmitting ? 'Sending...' : 'Send Message'}
						</button>
					</div>
				</form>
			</div>
		</section>
	);
}
