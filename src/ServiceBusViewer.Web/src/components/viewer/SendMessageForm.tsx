import {
	useEffect,
	useMemo,
	useRef,
	useState,
	type ChangeEvent,
	type FormEvent,
} from 'react';
import { useStoredBoolean } from '../../hooks/useStoredBoolean';
import { cx } from '../../lib/cx';
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
};

function createProperty(): ApplicationPropertyInputDto {
	return { key: '', type: 'String', value: '' };
}

export function SendMessageForm({
	canSend,
	isSubmitting,
	requiresSession,
	onSubmit,
	onValidationError,
}: SendMessageFormProps) {
	const [isOpen, setIsOpen] = useStoredBoolean('sendFormOpen', true);
	const [messageBody, setMessageBody] = useState('');
	const [messageProperties, setMessageProperties] =
		useState<SendMessagePropertiesDto>(emptyProperties);
	const [applicationProperties, setApplicationProperties] = useState<
		ApplicationPropertyInputDto[]
	>([]);
	const [isContentTypeMenuOpen, setIsContentTypeMenuOpen] = useState(false);
	const [contentTypeFilter, setContentTypeFilter] = useState('');
	const contentTypeContainerRef = useRef<HTMLDivElement | null>(null);

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

		return errors;
	};

	const resetForm = () => {
		setMessageBody('');
		setMessageProperties(emptyProperties);
		setApplicationProperties([]);
		setIsContentTypeMenuOpen(false);
		setContentTypeFilter('');
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
	};

	const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		const errors = validate();
		if (errors.length > 0) {
			onValidationError(errors);
			return;
		}

		await onSubmit({
			sendMessageApplicationProperties: applicationProperties,
			sendMessageBody: messageBody,
			sendMessageProperties: {
				...messageProperties,
				scheduledEnqueueTime: messageProperties.scheduledEnqueueTime || null,
			},
		});
		resetForm();
	};

	return (
		<section className="mb-6 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm shadow-slate-200/40 dark:border-slate-800 dark:bg-slate-900 dark:shadow-black/20">
			<div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
				<h2 className="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">
					Send Message
				</h2>
				<button
					type="button"
					className="inline-flex w-20 items-center justify-center gap-1 rounded-lg bg-slate-100 px-2 py-1 text-[11px] font-medium text-slate-500 transition hover:bg-slate-200 hover:text-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700 dark:hover:text-white"
					onClick={() => setIsOpen((current) => !current)}
				>
					<span className="material-icons-round text-sm">
						{isOpen ? 'keyboard_arrow_up' : 'keyboard_arrow_down'}
					</span>
					<span className="text-center">{isOpen ? 'Hide' : 'Show'}</span>
				</button>
			</div>

			<div className={cx('send-form-transition', !isOpen && 'js-send-form-hidden')}>
				<form className="p-4 lg:p-6" onSubmit={(event) => void handleSubmit(event)}>
					<div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_19rem]">
						<div>
							<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
								Payload
							</label>
							<textarea
								className="block min-h-56 w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 font-mono text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition placeholder:text-slate-400 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:placeholder:text-slate-500 dark:focus:border-brand-400"
								rows={12}
								value={messageBody}
								onChange={(event) => setMessageBody(event.target.value)}
							/>
						</div>

						<div className="space-y-4">
							<div>
								<label className="mb-2 block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
									Message ID
								</label>
								<input
									type="text"
									maxLength={128}
									className="block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm shadow-slate-200/40 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
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

					<div className="mt-6">
						<div className="mb-3 flex items-center justify-between gap-3">
							<div>
								<label className="block text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
									Application Properties
								</label>
								<p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
									Optional typed key-value metadata to attach to the outgoing message.
								</p>
							</div>
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

					<div className="mt-6 flex justify-end">
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
