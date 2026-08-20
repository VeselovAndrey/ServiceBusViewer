import { Fragment, useCallback, useEffect, useMemo, useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { serviceBusApi } from '../api/serviceBusApi';
import { Alert } from '../components/common/Alert';
import { JsonMessageBody } from '../components/common/JsonMessageBody';
import { EntitySidebar } from '../components/entities/EntitySidebar';
import { AppLayout } from '../components/layout/AppLayout';
import { SendMessageForm } from '../components/viewer/SendMessageForm';
import {
	buildPeekedMessageSummary,
	createMessageRowKey,
	formatDateTime,
	formatDateTimeShort,
	formatNullable,
	formatUnknownValue,
	getEntityKey,
	getViewerEntityMeta,
} from '../lib/formatters';
import { getErrorMessages } from '../lib/problemDetails';
import { useAppState } from '../state/AppStateContext';
import type {
	EntityIdDto,
	ReceivedMessageApplicationPropertyDto,
	SendMessageRequestDto,
	ViewerState,
} from '../types/serviceBus';

type ViewerAction = 'disconnect' | 'receive' | 'refresh' | 'send';

function buildSendResultMessage(request: SendMessageRequestDto) {
	const { contentType, messageId } = request.sendMessageProperties;
	const contentTypeDescription = contentType.trim()
		? `with content type '${contentType}'`
		: 'without a content type';

	return messageId.trim()
		? `Message '${messageId}' sent successfully ${contentTypeDescription}.`
		: `Message sent successfully ${contentTypeDescription}.`;
}

export function ViewerPage() {
	const navigate = useNavigate();
	const { sessionState, refreshSessionState, setViewerState } = useAppState();
	const viewer = sessionState?.viewer;
	const currentViewer = viewer;
	const [pendingAction, setPendingAction] = useState<ViewerAction | null>(null);
	const [errorMessages, setErrorMessages] = useState<string[]>([]);
	const [selectionKey, setSelectionKey] = useState<string | null>(null);
	const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
	const [receiveSessionId, setReceiveSessionId] = useState(viewer?.receiveSessionId ?? '');

	useEffect(() => {
		if (!sessionState?.isConnected || viewer) {
			return;
		}

		let isDisposed = false;
		void serviceBusApi
			.getViewer()
			.then((nextViewer) => {
				if (!isDisposed) {
					setViewerState(nextViewer);
				}
			})
			.catch((error: unknown) => {
				if (!isDisposed) {
					setErrorMessages(getErrorMessages(error));
				}
			});

		return () => {
			isDisposed = true;
		};
	}, [sessionState?.isConnected, setViewerState, viewer]);

	useEffect(() => {
		setReceiveSessionId(viewer?.receiveSessionId ?? '');
	}, [viewer?.receiveSessionId]);

	const syncViewer = useCallback(
		async (nextViewer: ViewerState) => {
			setViewerState(nextViewer);
			return nextViewer;
		},
		[setViewerState],
	);

	const runAction = useCallback(
		async (action: ViewerAction, work: () => Promise<void>) => {
			setPendingAction(action);
			setErrorMessages([]);

			try {
				await work();
				return true;
			} catch (error: unknown) {
				setErrorMessages(getErrorMessages(error));
				return false;
			} finally {
				setPendingAction(null);
			}
		},
		[],
	);

	const activeMessagePropertyRows = useMemo(() => {
		if (!currentViewer?.displayedMessage) {
			return [];
		}

		const properties = currentViewer.displayedMessage.properties;
		return [
			['Message ID', properties.messageId],
			['Content Type', formatNullable(properties.contentType, 'not set')],
			['Partition Key', formatNullable(properties.partitionKey)],
			['Scheduled Enqueue', formatDateTime(properties.scheduledEnqueueTime)],
			['Time To Live', formatNullable(properties.timeToLive)],
		];
	}, [currentViewer?.displayedMessage]);

	if (!sessionState) {
		return null;
	}

	if (!sessionState.isConnected) {
		return <Navigate replace to="/connect" />;
	}
	const entityMeta = currentViewer
		? getViewerEntityMeta(currentViewer)
		: {
			icon: 'view_headline',
			subtitle: 'Loading viewer state...',
			typeLabel: 'Queue' as const,
		};
	const peekedMessageSummary = currentViewer
		? buildPeekedMessageSummary(currentViewer.messages, currentViewer.hasMoreMessages)
		: '0 total';

	const handleDisconnect = async () => {
		await runAction('disconnect', async () => {
			await serviceBusApi.disconnect();
			await refreshSessionState();
			navigate('/connect', { replace: true });
		});
	};

	const handleSelectEntity = async (entity: EntityIdDto) => {
		const nextSelectionKey = getEntityKey(entity);
		setSelectionKey(nextSelectionKey);
		setErrorMessages([]);

		try {
			const nextViewer = await syncViewer(await serviceBusApi.selectEntity(entity));
			setExpandedRows(new Set());
			setReceiveSessionId(nextViewer.receiveSessionId ?? '');
			navigate('/viewer', { replace: true });
		} catch (error: unknown) {
			setErrorMessages(getErrorMessages(error));
		} finally {
			setSelectionKey(null);
		}
	};

	const handleRefresh = async () => {
		await runAction('refresh', async () => {
			const nextViewer = await syncViewer(await serviceBusApi.refreshViewer());
			setReceiveSessionId(nextViewer.receiveSessionId ?? '');
			setExpandedRows(new Set());
		});
	};

	const handleReceive = async () => {
		await runAction('receive', async () => {
			const nextViewer = await syncViewer(
				await serviceBusApi.receive({
					receiveSessionId: currentViewer?.requiresSession ? receiveSessionId : null,
				}),
			);
			setReceiveSessionId(nextViewer.receiveSessionId ?? '');
			setExpandedRows(new Set());
		});
	};

	const handleSend = async (request: SendMessageRequestDto) => {
		const succeeded = await runAction('send', async () => {
			await serviceBusApi.send(request);

			if (currentViewer) {
				setViewerState({
					...currentViewer,
					sendResultMessage: buildSendResultMessage(request),
				});
			}

			try {
				const nextViewer = await syncViewer(await serviceBusApi.refreshViewer());
				setReceiveSessionId(nextViewer.receiveSessionId ?? '');
				setExpandedRows(new Set());
			} catch {
				setErrorMessages([
					'The message was sent successfully, but the message list could not be refreshed. Use Refresh to try again.',
				]);
			}
		});
		if (!succeeded) {
			throw new Error('The message was not sent.');
		}
	};

	const renderApplicationPropertiesTable = (
		applicationProperties: Record<string, ReceivedMessageApplicationPropertyDto> | null,
		emptyClassName: string,
	) => {
		if (applicationProperties && Object.keys(applicationProperties).length > 0) {
			return (
				<div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-800">
					<table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
						<thead className="bg-slate-50 text-slate-500 dark:bg-slate-950/40 dark:text-slate-400">
							<tr>
								<th className="px-4 py-2 text-left font-medium">Application Property</th>
								<th className="px-4 py-2 text-left font-medium">Value</th>
								<th className="px-4 py-2 text-left font-medium">Type</th>
							</tr>
						</thead>
						<tbody className="divide-y divide-slate-100 dark:divide-slate-800">
							{Object.entries(applicationProperties).map(([key, property]) => (
								<tr key={key}>
									<td className="px-4 py-2 font-mono text-xs text-slate-700 dark:text-slate-200">
										{key}
									</td>
									<td className="px-4 py-2 text-xs text-slate-600 dark:text-slate-300">
										{formatUnknownValue(property.value)}
									</td>
									<td className="px-4 py-2 text-xs text-slate-600 dark:text-slate-300">
										{property.type}
									</td>
								</tr>
							))}
						</tbody>
					</table>
				</div>
			);
		}

		return <div className={emptyClassName}>No application properties on this message.</div>;
	};

	const toggleExpandedRow = (rowKey: string) => {
		setExpandedRows((current) => {
			const next = new Set(current);
			if (next.has(rowKey)) {
				next.delete(rowKey);
			} else {
				next.add(rowKey);
			}
			return next;
		});
	};

	return (
		<AppLayout
			applicationVersion={sessionState.applicationVersion}
			footerStatus={
				currentViewer?.requiresSession
					? 'Session-aware receive enabled'
					: 'Standard receive mode'
			}
			headerAction={{
				busy: pendingAction === 'disconnect',
				icon: 'logout',
				kind: 'button',
				label: 'Disconnect',
				onClick: handleDisconnect,
			}}
			headerStatus={{
				text: 'Connected to',
				tone: 'connected',
				value: currentViewer?.serviceBusHostName ?? '',
			}}
			title={currentViewer?.entityName ?? 'Viewer'}
		>
			<div className="flex flex-1 flex-col bg-slate-100 text-slate-800 dark:bg-slate-950 dark:text-slate-100">
				<div className="flex flex-1 flex-col overflow-hidden lg:flex-row">
					<EntitySidebar
						activeSelectionKey={selectionKey}
						availableEntities={currentViewer?.availableEntities ?? []}
						displayPropertyLinks={Boolean(currentViewer?.isManagementApiAvailable)}
						entityName={currentViewer?.entityName ?? null}
						onSelectEntity={handleSelectEntity}
						topicName={currentViewer?.topicName ?? null}
					/>

					<main className="flex min-w-0 flex-1 flex-col overflow-hidden">
						<div className="flex flex-col gap-4 border-b border-slate-200 bg-white px-4 py-3 dark:border-zinc-800 dark:bg-zinc-900 lg:flex-row lg:items-center lg:justify-between lg:px-4">
							<div className="min-w-0">
								<div className="flex items-center gap-3">
									<span className="material-icons-round text-brand-500">{entityMeta.icon}</span>

									<div className="min-w-0">
										<h1 className="truncate text-sm font-semibold text-slate-900 dark:text-white">
											{currentViewer?.entityName ?? 'No entity selected'}
										</h1>
										<p className="text-sm text-slate-500 dark:text-slate-400">
											{entityMeta.typeLabel} - {entityMeta.subtitle}
										</p>
									</div>
								</div>
							</div>

							<div className="flex flex-wrap items-center gap-5">
								<button
									type="button"
									className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs shadow-sm transition hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800"
									aria-busy={pendingAction === 'refresh'}
									disabled={pendingAction === 'refresh'}
									onClick={() => {
										void handleRefresh();
									}}
								>
									<span
										className={`material-icons-round text-sm${pendingAction === 'refresh' ? ' animate-spin' : ''}`}
									>
										refresh
									</span>
									Refresh
								</button>

								<div className="inline-flex items-stretch overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
									<input
										type="text"
										className="w-44 border-0 bg-transparent px-3 py-1.5 text-xs text-slate-700 transition placeholder:text-slate-400 focus:ring-0 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-400 dark:text-slate-100 dark:placeholder:text-slate-500 dark:disabled:bg-zinc-950"
										placeholder="Session ID"
										value={receiveSessionId}
										disabled={!currentViewer?.requiresSession}
										onChange={(event) => setReceiveSessionId(event.target.value)}
									/>
									<button
										type="button"
										className="inline-flex items-center gap-1.5 border-l border-slate-200 px-3 py-1.5 text-xs transition hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:border-zinc-800 dark:hover:bg-zinc-800"
										disabled={pendingAction === 'receive'}
										onClick={() => {
											void handleReceive();
										}}
									>
										<span className="material-icons-round text-sm">download</span>
										{pendingAction === 'receive' ? 'Receiving...' : 'Receive'}
									</button>
								</div>
							</div>
						</div>

						<div className="custom-scrollbar flex-1 overflow-y-auto p-4 lg:p-6">
							<Alert className="mb-6" messages={errorMessages} tone="error" />
							<Alert
								className="mb-6"
								messages={currentViewer?.sendResultMessage ? [currentViewer.sendResultMessage] : []}
								tone="success"
							/>

						<SendMessageForm
							canSend={Boolean(currentViewer?.entityName)}
							entityName={currentViewer?.entityName ?? null}
							isSubmitting={pendingAction === 'send'}
							requiresSession={Boolean(currentViewer?.requiresSession)}
							topicName={currentViewer?.topicName ?? null}
							onSubmit={handleSend}
							onValidationError={setErrorMessages}
						/>

							<section className="mb-6 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm shadow-slate-200/40 dark:border-slate-800 dark:bg-slate-900 dark:shadow-black/20">
								<div className="flex items-center justify-between border-b border-slate-200 bg-slate-50/80 px-4 py-3 dark:border-slate-800 dark:bg-slate-950/40">
									<h2 className="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">
										Last Received Message
									</h2>
									{currentViewer?.displayedMessage ? (
										<span className="rounded-xl bg-slate-200 px-2 py-1 font-mono text-[11px] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
											ID: {currentViewer.displayedMessage.properties.messageId}
										</span>
									) : null}
								</div>

								<div className="p-4 lg:p-6">
									{currentViewer?.displayedMessage ? (
										<>
											<div className="grid gap-4 border-b border-dashed border-slate-200 pb-4 dark:border-slate-800 lg:grid-cols-2 xl:grid-cols-4">
												<div>
													<div className="text-[11px] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
														Enqueued Time
													</div>
													<div className="mt-1 font-mono text-xs text-slate-700 dark:text-slate-200">
														{formatDateTime(currentViewer.displayedMessage.properties.enqueuedTimeUtc)}
													</div>
												</div>
												<div>
													<div className="text-[11px] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
														Session ID
													</div>
													<div className="mt-1 font-mono text-xs text-slate-700 dark:text-slate-200">
														{formatNullable(currentViewer.displayedMessage.properties.sessionId)}
													</div>
												</div>
												<div>
													<div className="text-[11px] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
														Correlation ID
													</div>
													<div className="mt-1 font-mono text-xs text-slate-700 dark:text-slate-200">
														{formatNullable(currentViewer.displayedMessage.properties.correlationId)}
													</div>
												</div>
												<div>
													<div className="text-[11px] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
														Content Type
													</div>
													<div className="mt-1 text-xs text-slate-700 dark:text-slate-200">
														{formatNullable(
															currentViewer.displayedMessage.properties.contentType,
															'not set',
														)}
													</div>
												</div>
											</div>

											<div className="mt-4 grid gap-6 xl:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)]">
												<div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-800">
													<table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
														<tbody className="divide-y divide-slate-100 dark:divide-slate-800">
															{activeMessagePropertyRows.map(([label, value]) => (
																<tr key={label}>
																	<th className="w-40 bg-slate-50 px-4 py-3 text-left font-medium text-slate-500 dark:bg-slate-950/40 dark:text-slate-400">
																		{label}
																	</th>
																	<td className="px-4 py-3 text-slate-700 dark:text-slate-200">
																		{value}
																	</td>
																</tr>
															))}
														</tbody>
													</table>
												</div>

												<div>
													{renderApplicationPropertiesTable(
														currentViewer.displayedMessage.applicationProperties,
														'rounded-xl border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-950/50 dark:text-slate-400',
													)}
												</div>
											</div>

											<JsonMessageBody
												body={currentViewer.displayedMessage.body}
												fullMessage={currentViewer.displayedMessage}
												className="mt-6"
											/>
										</>
									) : (
										<div className="rounded-xl border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-950/50 dark:text-slate-400">
											Use{' '}
											<span className="font-medium text-slate-700 dark:text-slate-200">
												Receive
											</span>{' '}
											to lock and inspect the next available message.
										</div>
									)}
								</div>
							</section>

							<section className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm shadow-slate-200/40 dark:border-slate-800 dark:bg-slate-900 dark:shadow-black/20">
								<div className="flex flex-col gap-3 border-b border-slate-200 px-4 py-3 dark:border-slate-800 md:flex-row md:items-center md:justify-between">
									<div className="flex items-center gap-2">
										<h2 className="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">
											Peeked Messages
										</h2>
										<span className="rounded-xl bg-slate-100 px-2 py-1 text-[11px] text-slate-500 dark:bg-slate-800 dark:text-slate-400">
											{peekedMessageSummary}
										</span>
									</div>
								</div>

								{currentViewer?.messages.length ? (
									<div className="overflow-x-auto">
										<table className="min-w-full text-left text-sm">
											<thead className="border-b border-slate-200 bg-slate-50 text-slate-500 dark:border-slate-800 dark:bg-slate-950/40 dark:text-slate-400">
												<tr>
													<th className="px-4 py-3 font-medium">Message ID</th>
													<th className="px-4 py-3 font-medium">Enqueued Time</th>
													<th className="px-4 py-3 font-medium">Content Type</th>
													<th className="px-4 py-3 font-medium">Session / TTL</th>
													<th className="px-4 py-3 text-right font-medium">Actions</th>
												</tr>
											</thead>
											<tbody className="divide-y divide-slate-100 dark:divide-slate-800">
												{currentViewer.messages.map((message, index) => {
													const rowKey = createMessageRowKey(message, index);
													const isExpanded = expandedRows.has(rowKey);
													const propertyRows: Array<[string, string]> = [
														['Message ID', message.properties.messageId],
														['Enqueued Time', formatDateTime(message.properties.enqueuedTimeUtc)],
														['Content Type', formatNullable(message.properties.contentType, 'not set')],
														['Partition Key', formatNullable(message.properties.partitionKey)],
														['Correlation ID', formatNullable(message.properties.correlationId)],
														['Scheduled Enqueue', formatDateTime(message.properties.scheduledEnqueueTime)],
														['Time To Live', formatNullable(message.properties.timeToLive)],
													];

													return (
														<Fragment key={rowKey}>
															<tr className="align-top transition hover:bg-slate-50 dark:hover:bg-slate-950/40">
																<td className="px-4 py-3">
																	<div className="font-mono text-xs text-slate-700 dark:text-slate-200">
																		{message.properties.messageId}
																	</div>
																	{currentViewer.requiresSession && message.properties.sessionId ? (
																		<div className="mt-1 text-[11px] text-slate-500 dark:text-slate-400">
																			Session: {message.properties.sessionId}
																		</div>
																	) : null}
																</td>
																<td className="px-4 py-3 text-xs text-slate-600 dark:text-slate-300">
																	{formatDateTimeShort(message.properties.enqueuedTimeUtc)}
																</td>
																<td className="px-4 py-3 text-xs text-slate-600 dark:text-slate-300">
																	{formatNullable(message.properties.contentType, 'not set')}
																</td>
																<td className="px-4 py-3 text-xs text-slate-600 dark:text-slate-300">
																	{formatNullable(message.properties.sessionId, 'No session')}
																	<div className="mt-1 text-[11px] text-slate-400 dark:text-slate-500">
																		TTL: {formatNullable(message.properties.timeToLive)}
																	</div>
																</td>
																<td className="px-4 py-3 text-right">
																	<button
																		type="button"
																		className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-brand-700 transition hover:border-brand-200 hover:bg-brand-50 dark:border-slate-800 dark:bg-slate-900 dark:text-brand-300 dark:hover:border-brand-900/60 dark:hover:bg-brand-950/30"
																		onClick={() => toggleExpandedRow(rowKey)}
																	>
																		<span>{isExpanded ? 'Collapse' : 'Expand'}</span>
																	</button>
																</td>
															</tr>
															{isExpanded ? (
																<tr className="bg-slate-50/70 dark:bg-slate-950/50">
																	<td colSpan={5} className="max-w-0 px-4 py-4">
																		<div className="grid gap-6 xl:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)]">
																			<div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-800">
																				<table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
																					<tbody className="divide-y divide-slate-100 dark:divide-slate-800">
																						{propertyRows.map(([label, value]) => (
																							<tr key={`${rowKey}-${label}`}>
																								<th className="w-40 bg-white px-4 py-3 text-left font-medium text-slate-500 dark:bg-slate-900 dark:text-slate-400">
																									{label}
																								</th>
																								<td className="px-4 py-3 text-slate-700 dark:text-slate-200">
																									{value}
																								</td>
																							</tr>
																						))}
																					</tbody>
																				</table>
																			</div>

																			<div>
																				{renderApplicationPropertiesTable(
																					message.applicationProperties,
																					'rounded-xl border border-dashed border-slate-300 bg-white px-4 py-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400',
																				)}
																			</div>
																		</div>

																		<JsonMessageBody
																			body={message.body}
																			fullMessage={message}
																			className="mt-4"
																		/>
																	</td>
																</tr>
															) : null}
														</Fragment>
													);
												})}
											</tbody>
										</table>
									</div>
								) : (
									<div className="p-6">
										<div className="rounded-xl border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-950/50 dark:text-slate-400">
											No messages found.
										</div>
									</div>
								)}
							</section>
						</div>
					</main>
				</div>
			</div>
		</AppLayout>
	);
}
