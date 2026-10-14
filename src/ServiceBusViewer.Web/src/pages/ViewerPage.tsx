import { Fragment, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { serviceBusApi } from '../api/serviceBusApi';
import { Alert } from '../components/common/Alert';
import { CopyFullMessageButton } from '../components/common/CopyFullMessageButton';
import { JsonMessageBody } from '../components/common/JsonMessageBody';
import { EntitySidebar } from '../components/entities/EntitySidebar';
import { AppLayout } from '../components/layout/AppLayout';
import { DropAllConfirmPrompt, DropSingleConfirmPrompt } from '../components/viewer/DropPrompts';
import { RepublishAllConfirmPrompt, RepublishManualIdPrompt } from '../components/viewer/RepublishPrompts';
import {
	RepublishStrategyMenu,
	republishStrategyOptions,
} from '../components/viewer/RepublishStrategyMenu';
import { SendMessageForm } from '../components/viewer/SendMessageForm';
import { PeekMessageCountSelector } from '../components/viewer/PeekMessageCountSelector';
import { usePeekMessageCount } from '../hooks/usePeekMessageCount';
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
import { matchesPeekedMessageSearch } from '../lib/messageSearch';
import {
	comparePeekedMessages,
	type PeekedMessageSort,
	type PeekedMessageSortColumn,
} from '../lib/messageSorting';
import { getErrorMessages } from '../lib/problemDetails';
import { useAppState } from '../state/AppStateContext';
import type {
	DropDeadLettersResponseDto,
	EntityIdDto,
	ReceivedMessageApplicationPropertyDto,
	RepublishDeadLetterRequestDto,
	RepublishDeadLetterResponseDto,
	RepublishMessageIdStrategy,
	SendMessageRequestDto,
	ViewerState,
} from '../types/serviceBus';

type ViewerAction = 'disconnect' | 'drop' | 'receive' | 'refresh' | 'republish' | 'send' | 'peek-count';

// Number of dead-letter messages each bounded bulk step processes per request (decided in Task 25-01).
const BULK_STEP_SIZE = 100;

const MESSAGE_ROW_BASE_CLASS = 'align-top transition hover:bg-slate-50 dark:hover:bg-slate-950/40';

interface BulkOperationProgress {
	operation: 'drop' | 'republish';
	processed: number;
	failed: number;
}

function buildSendResultMessage(request: SendMessageRequestDto) {
	const { messageId } = request.sendMessageProperties;

	return messageId.trim()
		? `Message '${messageId}' sent successfully.`
		: 'Message sent successfully.';
}

function buildRepublishOutcomeMessages(outcome: RepublishDeadLetterResponseDto, cancelled: boolean) {
	const messages = [
		`${cancelled ? 'Republish cancelled.' : outcome.succeeded ? 'Republish succeeded.' : 'Republish finished with failures.'} ${outcome.affectedCount} republished, ${outcome.failedCount} failed.`,
	];
	if (outcome.detail) {
		messages.push(outcome.detail);
	}
	return messages;
}

function buildDropOutcomeMessages(outcome: DropDeadLettersResponseDto, cancelled: boolean) {
	const messages = [
		`${cancelled ? 'Drop cancelled.' : outcome.succeeded ? 'Drop succeeded.' : 'Drop finished with failures.'} ${outcome.affectedCount} dropped, ${outcome.failedCount} failed.`,
	];
	if (outcome.detail) {
		messages.push(outcome.detail);
	}
	return messages;
}

export function ViewerPage() {
	const navigate = useNavigate();
	const { sessionState, refreshSessionState, setViewerState } = useAppState();
	const viewer = sessionState?.viewer;
	const currentViewer = viewer;
	const [pendingAction, setPendingAction] = useState<ViewerAction | null>(null);
	const [errorMessages, setErrorMessages] = useState<string[]>([]);
	const [sendErrorMessages, setSendErrorMessages] = useState<string[]>([]);
	const [selectionKey, setSelectionKey] = useState<string | null>(null);
	const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
	const [receiveSessionId, setReceiveSessionId] = useState(viewer?.receiveSessionId ?? '');
	const [deadLetterMode, setDeadLetterMode] = useState(false);
	const [deadLetterViewer, setDeadLetterViewer] = useState<ViewerState | null>(null);
	const [republishOutcome, setRepublishOutcome] = useState<RepublishDeadLetterResponseDto | null>(
		null,
	);
	const [republishCancelled, setRepublishCancelled] = useState(false);
	const [republishErrorMessages, setRepublishErrorMessages] = useState<string[]>([]);
	const [republishPrompt, setRepublishPrompt] = useState<{
		kind: 'manual';
		messageId: string;
	} | null>(null);
	const [republishTargetRequiresDuplicateDetection, setRepublishTargetRequiresDuplicateDetection] =
		useState<boolean | null>(null);
	const [dropOutcome, setDropOutcome] = useState<DropDeadLettersResponseDto | null>(null);
	const [dropCancelled, setDropCancelled] = useState(false);
	const [dropErrorMessages, setDropErrorMessages] = useState<string[]>([]);
	const [dropConfirmOpen, setDropConfirmOpen] = useState(false);
	const [republishAllConfirmStrategy, setRepublishAllConfirmStrategy] =
		useState<RepublishMessageIdStrategy | null>(null);
	const [dropSingleTarget, setDropSingleTarget] = useState<string | null>(null);
	const [bulkProgress, setBulkProgress] = useState<BulkOperationProgress | null>(null);
	const bulkCancelRequestedRef = useRef(false);
	const [removingMessageIds, setRemovingMessageIds] = useState<Map<string, 'drop' | 'republish'>>(new Map());
	const [bulkSuccessMessage, setBulkSuccessMessage] = useState<string | null>(null);
	const [peekMessageCount, setPeekMessageCount] = usePeekMessageCount();
	const [messageSort, setMessageSort] = useState<PeekedMessageSort | null>(null);
	// The search text intentionally survives new message-set loads (unlike the active sort) and is
	// re-applied to each newly loaded set; only the clear control or the user empties it.
	const [messageSearchText, setMessageSearchText] = useState('');

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

	const sendResultMessage = viewer?.sendResultMessage ?? null;

	useEffect(() => {
		if (!sendResultMessage || sendErrorMessages.length > 0) {
			return;
		}

		const timerId = window.setTimeout(() => {
			setViewerState((currentViewer) => {
				if (!currentViewer) {
					return currentViewer;
				}

				return {
					...currentViewer,
					sendResultMessage: null,
				};
			});
		}, 5000);

		return () => window.clearTimeout(timerId);
	}, [sendResultMessage, sendErrorMessages.length, setViewerState]);

	useEffect(() => {
		setRepublishTargetRequiresDuplicateDetection(null);

		if (!deadLetterMode) {
			return;
		}

		const detailsType = currentViewer?.topicName ? 'Topic' : 'Queue';
		const detailsName = currentViewer?.topicName ?? currentViewer?.entityName;
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

				const requiresDuplicateDetection =
					'requiresDuplicateDetection' in details.properties
						? Boolean(details.properties.requiresDuplicateDetection)
						: false;
				setRepublishTargetRequiresDuplicateDetection(requiresDuplicateDetection);
			})
			.catch(() => {
				if (!isDisposed) {
					setRepublishTargetRequiresDuplicateDetection(null);
				}
			});

		return () => {
			isDisposed = true;
		};
	}, [deadLetterMode, currentViewer?.topicName, currentViewer?.entityName]);

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
			setSendErrorMessages([]);

			try {
				await work();
				return true;
			} catch (error: unknown) {
				const messages = getErrorMessages(error);
				if (action === 'send') {
					setSendErrorMessages(messages);
				} else {
					setErrorMessages(messages);
				}
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
			['Content Type', formatNullable(properties.contentType, 'not set')],
			['Time To Live', formatNullable(properties.timeToLive)],
			['Scheduled Enqueue', formatDateTime(properties.scheduledEnqueueTime)],
			['Correlation ID', formatNullable(properties.correlationId)],
			['Subject', formatNullable(properties.subject)],
			['To', formatNullable(properties.to)],
			['Reply To', formatNullable(properties.replyTo)]
		];
	}, [currentViewer?.displayedMessage]);

	// The displayed rows derive from the already-loaded messages only: sorting reorders the loaded
	// set and searching narrows it, both display-only and never triggering message retrieval.
	// Each row keeps its original load-order index so row keys and expansion state stay stable.
	const displayedMessageRows = useMemo(() => {
		const source = deadLetterMode ? deadLetterViewer : viewer;
		let rows = (source?.messages ?? []).map((message, loadIndex) => ({ loadIndex, message }));
		if (messageSort !== null) {
			rows.sort((left, right) => comparePeekedMessages(left.message, right.message, messageSort));
		}

		rows = rows.filter(({ message }) => matchesPeekedMessageSearch(message, messageSearchText));

		return rows;
	}, [deadLetterMode, deadLetterViewer, viewer, messageSort, messageSearchText]);

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
	const activeMessageSource = deadLetterMode ? deadLetterViewer : currentViewer;
	const activeMessages = activeMessageSource?.messages ?? [];
	// Drop/republish buttons stay disabled while any operation runs, including the row-removal
	// animation window after a single republish succeeds (pendingAction already cleared by then).
	const deadLetterOperationInProgress = pendingAction !== null || removingMessageIds.size > 0;
	// With management access, the total count comes from runtime properties, so no full read is needed; it covers the live list and the dead-letter list alike.
	const totalMessageCount = activeMessageSource?.totalMessageCount ?? null;
	const peekedMessageSummary =
		totalMessageCount !== null && (activeMessageSource?.hasMoreMessages ?? false)
			? `Showing first ${activeMessages.length} of ${totalMessageCount}${deadLetterMode ? ' dead-lettered' : ''}`
			: buildPeekedMessageSummary(
				activeMessages,
				activeMessageSource?.hasMoreMessages ?? false,
			);
	const showScheduledEnqueue = activeMessages.some((message) =>
		Boolean(message.properties.scheduledEnqueueTime),
	);
	const showPartitionKey = activeMessages.some((message) =>
		Boolean(message.properties.partitionKey),
	);
	// Content Type shows only for peeked messages and Dead Letter Reason only in dead-letter mode, so they cancel out.
	const peekedMessageColumnCount =
		6 + (showScheduledEnqueue ? 1 : 0) + (showPartitionKey ? 1 : 0);

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
		setSendErrorMessages([]);

		try {
			const nextViewer = await syncViewer(await serviceBusApi.selectEntity(entity));
			setExpandedRows(new Set());
			setMessageSort(null);
			setReceiveSessionId(nextViewer.receiveSessionId ?? '');
			setDeadLetterMode(false);
			setDeadLetterViewer(null);
			setRepublishPrompt(null);
			setRepublishAllConfirmStrategy(null);
			setRepublishOutcome(null);
			setRepublishCancelled(false);
			setRepublishErrorMessages([]);
			setDropConfirmOpen(false);
			setDropSingleTarget(null);
			setDropOutcome(null);
			setDropCancelled(false);
			setDropErrorMessages([]);
			setBulkSuccessMessage(null);
			navigate('/viewer', { replace: true });
		} catch (error: unknown) {
			setErrorMessages(getErrorMessages(error));
		} finally {
			setSelectionKey(null);
		}
	};

	const handleRefresh = async () => {
		setBulkSuccessMessage(null);
		await runAction('refresh', async () => {
			if (deadLetterMode) {
				setDeadLetterViewer(await serviceBusApi.refreshDeadLetterList());
				setExpandedRows(new Set());
				setMessageSort(null);
				return;
			}

			const nextViewer = await syncViewer(await serviceBusApi.refreshViewer());
			setReceiveSessionId(nextViewer.receiveSessionId ?? '');
			setExpandedRows(new Set());
			setMessageSort(null);
		});
	};

	const handleToggleDeadLetter = async () => {
		setBulkSuccessMessage(null);
		if (!deadLetterMode) {
			await runAction('refresh', async () => {
				setDeadLetterViewer(await serviceBusApi.refreshDeadLetterList());
				setDeadLetterMode(true);
				setExpandedRows(new Set());
				setMessageSort(null);
			});
			return;
		}

		setDeadLetterMode(false);
		setDeadLetterViewer(null);
		setExpandedRows(new Set());
		setMessageSort(null);
	};

	const handlePeekMessageCountApply = async (count: number) => {
		await runAction('peek-count', async () => {
			const nextViewer = await serviceBusApi.updatePeekMessageCount({ peekMessageCount: count });
			setPeekMessageCount(count);

			if (deadLetterMode) {
				setDeadLetterViewer(await serviceBusApi.refreshDeadLetterList());
				setExpandedRows(new Set());
				setMessageSort(null);
			}

			await syncViewer(nextViewer);
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
			setMessageSort(null);
		});
	};

	const handleSend = async (request: SendMessageRequestDto) => {
		if (currentViewer) {
			setViewerState({
				...currentViewer,
				sendResultMessage: null,
			});
		}

		const succeeded = await runAction('send', async () => {
			await serviceBusApi.send(request);
			const sendResultMessage = buildSendResultMessage(request);

			try {
				const nextViewer = await syncViewer(await serviceBusApi.refreshViewer());
				setReceiveSessionId(nextViewer.receiveSessionId ?? '');
				setExpandedRows(new Set());
				setMessageSort(null);
				setViewerState({
					...nextViewer,
					sendResultMessage,
				});
			} catch {
				setSendErrorMessages([
					'The message was sent successfully, but the message list could not be refreshed. Use Refresh to try again.',
				]);
			}
		});
		if (!succeeded) {
			throw new Error('The message was not sent.');
		}
	};

	const runRepublish = async (request: RepublishDeadLetterRequestDto) => {
		const isBulk = request.messageId === null;
		setPendingAction('republish');
		setRepublishOutcome(null);
		setRepublishCancelled(false);
		setRepublishErrorMessages([]);
		bulkCancelRequestedRef.current = false;

		if (isBulk) {
			setBulkProgress({ operation: 'republish', processed: 0, failed: 0 });
		}

		let totalAffected = 0;
		let totalFailed = 0;
		let detail: string | null = null;
		let allStepsSucceeded = true;
		let cancelled = false;

		try {
			// A bulk republish runs as a sequence of bounded requests; the loop stops when a step processes
			// zero messages or the user cancels. Single-message republish stays one unbounded request.
			while (true) {
				const step = await serviceBusApi.republishDeadLetter(
					isBulk ? { ...request, maxCount: BULK_STEP_SIZE } : request,
				);
				totalAffected += step.affectedCount;
				totalFailed += step.failedCount;
				allStepsSucceeded = allStepsSucceeded && step.succeeded;
				detail = step.detail ?? detail;

				if (!isBulk) {
					break;
				}

				setBulkProgress({ operation: 'republish', processed: totalAffected + totalFailed, failed: totalFailed });

				if (bulkCancelRequestedRef.current) {
					cancelled = true;
					break;
				}

				if (step.affectedCount + step.failedCount === 0) {
					break;
				}
			}

			const republishSucceeded = !cancelled && allStepsSucceeded;

			if (!isBulk && republishSucceeded) {
				animateAndRemoveMessage(request.messageId!, 'republish');
				return;
			}

			if (isBulk && republishSucceeded) {
				setBulkSuccessMessage('All dead-lettered messages republished.');
			} else {
				setRepublishCancelled(cancelled);
				setRepublishOutcome({
					succeeded: republishSucceeded,
					affectedCount: totalAffected,
					failedCount: totalFailed,
					detail,
				});
			}

			try {
				setDeadLetterViewer(await serviceBusApi.refreshDeadLetterList());
				setExpandedRows(new Set());
				setMessageSort(null);
			} catch {
				setRepublishErrorMessages([
					'The republish completed, but the dead-letter list could not be refreshed. Use Refresh to try again.',
				]);
			}
		} catch (error: unknown) {
			if (isBulk && totalAffected + totalFailed > 0) {
				setRepublishOutcome({
					succeeded: false,
					affectedCount: totalAffected,
					failedCount: totalFailed,
					detail,
				});
			}
			setRepublishErrorMessages(getErrorMessages(error));
		} finally {
			setBulkProgress(null);
			setPendingAction(null);
		}
	};

	const startSingleRepublish = (
		messageId: string,
		strategy: RepublishMessageIdStrategy,
		manualMessageId: string | null = null,
	) => {
		void runRepublish({ messageId, strategy, manualMessageId });
	};

	const singleRepublishStrategyOptions =
		republishTargetRequiresDuplicateDetection === false
			? republishStrategyOptions
			: republishStrategyOptions.filter((option) => option.value !== 'KeepOriginal');

	const bulkRepublishStrategyOptions = singleRepublishStrategyOptions.filter(
		(option) => option.value !== 'Manual',
	);

	const handleSingleStrategyPick = (messageId: string, strategy: RepublishMessageIdStrategy) => {
		if (strategy === 'Manual') {
			setRepublishPrompt({ kind: 'manual', messageId });
			return;
		}

		startSingleRepublish(messageId, strategy);
	};

	const handleBulkStrategyPick = (strategy: RepublishMessageIdStrategy) => {
		setRepublishAllConfirmStrategy(strategy);
	};

	const handleRepublishAllConfirm = () => {
		const strategy = republishAllConfirmStrategy;
		setRepublishAllConfirmStrategy(null);

		if (strategy !== null) {
			void runRepublish({ messageId: null, strategy, manualMessageId: null });
		}
	};

	const handleRepublishAllCancel = () => {
		setRepublishAllConfirmStrategy(null);
	};

	const handleRepublishPromptCancel = () => {
		setRepublishPrompt(null);
	};

	const handleManualPromptConfirm = (manualMessageId: string) => {
		const prompt = republishPrompt;
		if (prompt === null) {
			return;
		}

		setRepublishPrompt(null);
		startSingleRepublish(prompt.messageId, 'Manual', manualMessageId);
	};

	const runDrop = async (messageId: string | null) => {
		const isBulk = messageId === null;
		setPendingAction('drop');
		setDropOutcome(null);
		setDropCancelled(false);
		setDropErrorMessages([]);
		bulkCancelRequestedRef.current = false;

		if (isBulk) {
			setBulkProgress({ operation: 'drop', processed: 0, failed: 0 });
		}

		let totalAffected = 0;
		let totalFailed = 0;
		let detail: string | null = null;
		let allStepsSucceeded = true;
		let cancelled = false;

		try {
			// A bulk drop runs as a sequence of bounded requests; the loop stops when a step processes
			// zero messages or the user cancels. Single-message drop stays one unbounded request.
			while (true) {
				const step = await serviceBusApi.dropDeadLetters(
					isBulk ? { messageId, maxCount: BULK_STEP_SIZE } : { messageId },
				);
				totalAffected += step.affectedCount;
				totalFailed += step.failedCount;
				allStepsSucceeded = allStepsSucceeded && step.succeeded;
				detail = step.detail ?? detail;

				if (!isBulk) {
					break;
				}

				setBulkProgress({ operation: 'drop', processed: totalAffected + totalFailed, failed: totalFailed });

				if (bulkCancelRequestedRef.current) {
					cancelled = true;
					break;
				}

				if (step.affectedCount + step.failedCount === 0) {
					break;
				}
			}

			const dropSucceeded = !cancelled && allStepsSucceeded;

			if (!isBulk && dropSucceeded) {
				animateAndRemoveMessage(messageId!, 'drop');
				return;
			}

			if (isBulk && dropSucceeded) {
				setBulkSuccessMessage('All dead-lettered messages dropped.');
			} else {
				setDropCancelled(cancelled);
				setDropOutcome({
					succeeded: dropSucceeded,
					affectedCount: totalAffected,
					failedCount: totalFailed,
					detail,
				});
			}

			try {
				setDeadLetterViewer(await serviceBusApi.refreshDeadLetterList());
				setExpandedRows(new Set());
				setMessageSort(null);
			} catch {
				setDropErrorMessages([
					'The drop completed, but the dead-letter list could not be refreshed. Use Refresh to try again.',
				]);
			}
		} catch (error: unknown) {
			if (isBulk && totalAffected + totalFailed > 0) {
				setDropOutcome({
					succeeded: false,
					affectedCount: totalAffected,
					failedCount: totalFailed,
					detail,
				});
			}
			setDropErrorMessages(getErrorMessages(error));
		} finally {
			setBulkProgress(null);
			setPendingAction(null);
		}
	};

	const handleBulkCancel = () => {
		bulkCancelRequestedRef.current = true;
	};

	const removeMessageFromList = (messageId: string) => {
		setDeadLetterViewer((current) => {
			if (!current) {
				return current;
			}

			return {
				...current,
				messages: current.messages.filter(
					(message) => message.properties.messageId !== messageId,
				),
				totalMessageCount:
					current.totalMessageCount !== null ? current.totalMessageCount - 1 : null,
			};
		});
	};

	// A successfully dropped or republished single message animates out of the list instead of a full
	// refresh: drop turns the row gray and soft before shrinking it to zero; republish shrinks directly.
	const animateAndRemoveMessage = (messageId: string, variant: 'drop' | 'republish') => {
		setRemovingMessageIds((current) => new Map(current).set(messageId, variant));
		window.setTimeout(() => {
			removeMessageFromList(messageId);
			setRemovingMessageIds((current) => {
				const next = new Map(current);
				next.delete(messageId);
				return next;
			});
		}, 300);
	};

	const dismissRepublishFeedback = () => {
		setRepublishOutcome(null);
		setRepublishCancelled(false);
		setRepublishErrorMessages([]);
	};

	const dismissDropFeedback = () => {
		setDropOutcome(null);
		setDropCancelled(false);
		setDropErrorMessages([]);
	};

	const handleDropSingle = (messageId: string) => {
		setDropSingleTarget(messageId);
	};

	const handleDropSingleConfirm = () => {
		const messageId = dropSingleTarget;
		setDropSingleTarget(null);

		if (messageId !== null) {
			void runDrop(messageId);
		}
	};

	const handleDropSingleCancel = () => {
		setDropSingleTarget(null);
	};

	const handleDropAllConfirm = () => {
		setDropConfirmOpen(false);
		void runDrop(null);
	};

	const handleDropAllCancel = () => {
		setDropConfirmOpen(false);
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
								<th className="px-4 py-2 text-left font-medium">Custom Property</th>
								<th className="px-4 py-2 text-left font-medium">Value</th>
								<th className="px-4 py-2 text-left font-medium">Type</th>
							</tr>
						</thead>
						<tbody className="divide-y divide-slate-100 dark:divide-slate-800">
							{Object.entries(applicationProperties)
								.sort(([a], [b]) => (a < b ? -1 : a > b ? 1 : 0))
								.map(([key, property]) => (
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

		return <div className={emptyClassName}>No custom properties on this message.</div>;
	};

	const handleSortHeaderClick = (column: PeekedMessageSortColumn) => {
		setMessageSort((current) => {
			if (current?.column !== column) {
				return { column, direction: 'ascending' };
			}

			return current.direction === 'ascending' ? { column, direction: 'descending' } : null;
		});
	};

	const renderSortableHeader = (column: PeekedMessageSortColumn, label: string) => {
		const isActive = messageSort?.column === column;
		const direction = isActive && messageSort !== null ? messageSort.direction : null;
		const indicator =
			direction === 'ascending'
				? 'arrow_upward'
				: direction === 'descending'
					? 'arrow_downward'
					: 'unfold_more';
		const stateLabel =
			direction === 'ascending'
				? 'sorted ascending'
				: direction === 'descending'
					? 'sorted descending'
					: 'not sorted';

		return (
			<button
				type="button"
				className={`inline-flex items-center gap-1 rounded font-medium transition hover:text-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:hover:text-slate-200${
					isActive ? ' text-brand-600 dark:text-brand-400' : ''
				}`}
				aria-label={`Sort by ${label} (${stateLabel})`}
				title={`Sort by ${label} (${stateLabel})`}
				onClick={() => handleSortHeaderClick(column)}
			>
				{label}
				<span aria-hidden="true" className="material-icons-round text-sm">
					{indicator}
				</span>
			</button>
		);
	};

	// Block-header controls are shared between the single-row layout and the dead-letter
	// small-screen two-line layout, so they are built once and placed per breakpoint.
	const refreshButton = (
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
	);

	const switchModeButton = (
		<button
			type="button"
			className={`inline-flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-xs shadow-sm transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 ${
				deadLetterMode
					? 'border-brand-500 bg-brand-50 text-brand-700 dark:border-brand-500 dark:bg-brand-950/30 dark:text-brand-300'
					: 'border-slate-200 bg-white hover:bg-slate-100 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800'
			}`}
			aria-pressed={deadLetterMode}
			aria-busy={deadLetterMode && pendingAction === 'refresh'}
			disabled={Boolean(currentViewer) && pendingAction !== null}
			onClick={() => {
				void handleToggleDeadLetter();
			}}
		>
			<span className="material-icons-round text-sm">{deadLetterMode ? 'inbox' : 'archive'}</span>
			<span className="inline-grid">
				<span className="col-start-1 row-start-1 invisible">To Dead Letter</span>
				<span className="col-start-1 row-start-1 text-left">{deadLetterMode ? 'To Active' : 'To Dead Letter'}</span>
			</span>
		</button>
	);

	const deadLetterBulkActions = (
		<>
			<RepublishStrategyMenu
				align="right"
				ariaBusy={pendingAction === 'republish'}
				buttonClassName="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium shadow-sm transition hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:cursor-not-allowed disabled:opacity-60 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800"
				buttonContent={
					<>
						<span className="material-icons-round text-sm">upload</span>
						{pendingAction === 'republish' ? 'Republishing...' : 'Republish All'}
						<span className="material-icons-round text-sm">arrow_drop_down</span>
					</>
				}
				disabled={deadLetterOperationInProgress || activeMessages.length === 0}
				menuTitle="Republish"
				options={bulkRepublishStrategyOptions}
				onPick={handleBulkStrategyPick}
			/>
			<button
				type="button"
				className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-600 shadow-sm transition hover:border-rose-300 hover:bg-rose-50 hover:text-rose-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-500 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:border-rose-900 dark:hover:bg-rose-950/30 dark:hover:text-rose-300"
				aria-busy={pendingAction === 'drop'}
				disabled={
					deadLetterOperationInProgress ||
					activeMessages.length === 0
				}
				onClick={() => {
					setDropConfirmOpen(true);
				}}
			>
				<span className="material-icons-round inline-block rotate-180 text-sm">
					upload
				</span>
				{pendingAction === 'drop' ? 'Dropping...' : 'Drop All'}
			</button>
		</>
	);

	const peekCountSelector = (
		<PeekMessageCountSelector
			value={peekMessageCount}
			disabled={pendingAction !== null}
			onApply={(count) => {
				void handlePeekMessageCountApply(count);
			}}
		/>
	);

	const searchControl = (
		<div className="inline-flex items-stretch overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
			<span className="inline-flex items-center border-0 pl-3 pr-1 text-slate-400 dark:text-slate-500">
				<span aria-hidden="true" className="material-icons-round text-sm">
					search
				</span>
			</span>			<input
				type="text"
				className="w-44 border-0 bg-transparent py-1.5 pl-1 pr-3 text-xs text-slate-700 transition placeholder:text-slate-400 focus:ring-0 dark:text-slate-100 dark:placeholder:text-slate-500"
				placeholder={deadLetterMode ? 'Search dead-lettered messages' : 'Search peeked messages'}
				aria-label={deadLetterMode ? 'Search dead-lettered messages' : 'Search peeked messages'}
				value={messageSearchText}
				onChange={(event) => setMessageSearchText(event.target.value)}
			/>
			<button
				type="button"
				className="inline-flex items-center border-l border-slate-200 px-2 py-1.5 text-xs transition hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:cursor-not-allowed disabled:opacity-40 dark:border-zinc-800 dark:hover:bg-zinc-800"
				aria-label="Clear search"
				title="Clear search"
				disabled={messageSearchText.length === 0}
				onClick={() => setMessageSearchText('')}
			>
				<span aria-hidden="true" className="material-icons-round text-sm">
					close
				</span>
			</button>
		</div>
	);

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
						</div>

						<div className="custom-scrollbar flex-1 overflow-y-auto p-4 lg:p-6">
							<Alert className="mb-6" messages={errorMessages} tone="error" />

							<SendMessageForm
								canSend={Boolean(currentViewer?.entityName)}
								entityName={currentViewer?.entityName ?? null}
								isSubmitting={pendingAction === 'send'}
								requiresSession={Boolean(currentViewer?.requiresSession)}
								sendResultMessage={currentViewer?.sendResultMessage ?? null}
								sendErrorMessages={sendErrorMessages}
								topicName={currentViewer?.topicName ?? null}
								onSubmit={handleSend}
								onValidationError={setSendErrorMessages}
							/>

							<section className="mb-6 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm shadow-slate-200/40 dark:border-slate-800 dark:bg-slate-900 dark:shadow-black/20">
								<div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-200 bg-slate-50/80 px-4 py-3 dark:border-slate-800 dark:bg-slate-950/40">
									<h2 className="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">
										Last Received Message
									</h2>
									<div className="flex items-center gap-2">
										<CopyFullMessageButton
											message={currentViewer?.displayedMessage ?? null}
											disabled={!currentViewer?.displayedMessage}
										/>
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

								<div className="p-4 lg:p-6">
									{currentViewer?.displayedMessage ? (
										<>
											<div className="grid gap-4 border-b border-dashed border-slate-200 pb-4 dark:border-slate-800 lg:grid-cols-2 xl:grid-cols-5">
												<div>
													<div className="text-[0.6875rem] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
														Enqueued Time
													</div>
													<div className="mt-1 font-mono text-xs text-slate-700 dark:text-slate-200">
														{formatDateTime(currentViewer.displayedMessage.properties.enqueuedTimeUtc)}
													</div>
												</div>
												<div>
													<div className="text-[0.6875rem] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
														Message ID
													</div>
													<div className="mt-1 font-mono text-xs text-slate-700 dark:text-slate-200">
														{currentViewer.displayedMessage.properties.messageId}
													</div>
												</div>
												<div>
													<div className="text-[0.6875rem] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
														Partition Key
													</div>
													<div className="mt-1 font-mono text-xs text-slate-700 dark:text-slate-200">
														{formatNullable(currentViewer.displayedMessage.properties.partitionKey)}
													</div>
												</div>
												<div>
													<div className="text-[0.6875rem] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
														Session ID
													</div>
													<div className="mt-1 font-mono text-xs text-slate-700 dark:text-slate-200">
														{formatNullable(currentViewer.displayedMessage.properties.sessionId)}
													</div>
												</div>
												<div>
													<div className="text-[0.6875rem] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
														Delivery Count
													</div>
													<div className="mt-1 font-mono text-xs text-slate-700 dark:text-slate-200">
														{currentViewer.displayedMessage.properties.deliveryCount}
													</div>
												</div>
											</div>

											<div className="mt-4 grid gap-6 xl:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)]">
												<div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-800">
													<table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
														<thead className="bg-slate-50 text-slate-500 dark:bg-slate-950/40 dark:text-slate-400">
															<tr>
																<th className="px-4 py-2 text-left font-medium">Message Property</th>
																<th className="px-4 py-2 text-left font-medium">Value</th>
															</tr>
														</thead>
														<tbody className="divide-y divide-slate-100 dark:divide-slate-800">
															{activeMessagePropertyRows.map(([label, value]) => (
																<tr key={label}>
																	<th className="w-44 whitespace-nowrap bg-white px-4 py-2 text-xs font-medium text-slate-700 dark:bg-slate-900 dark:text-slate-200">
																		{label}
																	</th>
																	<td className="px-4 py-2 text-xs text-slate-600 dark:text-slate-300">
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
								<div className="flex flex-wrap items-center gap-x-4 gap-y-3 border-b border-slate-200 px-4 py-3 dark:border-slate-800">
									{/* The two blocks share one row while they fit; when space runs out the actions
									   block wraps to a second line and ml-auto keeps it right-aligned. */}
									<div className="flex items-center gap-2">
										<h2 className="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">
											{deadLetterMode ? 'Dead-Lettered Messages' : 'Peeked Messages'}
										</h2>
										<span className="rounded-xl bg-slate-100 px-2 py-1 text-[0.6875rem] text-slate-500 dark:bg-slate-800 dark:text-slate-400">
											{peekedMessageSummary}
										</span>
										{peekCountSelector}
									</div>

									<div className="ml-auto flex flex-wrap items-center justify-end gap-2">
										{switchModeButton}
										{searchControl}
										{refreshButton}
									</div>

									{deadLetterMode ? (
										<div className="flex w-full flex-wrap items-center justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
											{deadLetterBulkActions}
										</div>
									) : null}
								</div>

								{deadLetterMode &&
								((republishOutcome !== null && !republishOutcome.succeeded) ||
									republishErrorMessages.length > 0 ||
									(dropOutcome !== null && !dropOutcome.succeeded) ||
									dropErrorMessages.length > 0) ? (
									<div className="flex flex-col gap-2 border-b border-slate-200 bg-slate-50/60 px-4 py-3 dark:border-slate-800 dark:bg-slate-950/30">
										{republishOutcome && !republishOutcome.succeeded ? (
											<Alert
												messages={buildRepublishOutcomeMessages(republishOutcome, republishCancelled)}
												tone="error"
												onDismiss={dismissRepublishFeedback}
											/>
										) : null}
										{republishErrorMessages.length > 0 ? (
											<Alert messages={republishErrorMessages} tone="error" onDismiss={dismissRepublishFeedback} />
										) : null}
										{dropOutcome && !dropOutcome.succeeded ? (
											<Alert
												messages={buildDropOutcomeMessages(dropOutcome, dropCancelled)}
												tone="error"
												onDismiss={dismissDropFeedback}
											/>
										) : null}
										{dropErrorMessages.length > 0 ? (
											<Alert messages={dropErrorMessages} tone="error" onDismiss={dismissDropFeedback} />
										) : null}
									</div>
								) : null}

								{deadLetterMode && bulkProgress ? (
								<div
									aria-live="polite"
									className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-200 bg-brand-50/60 px-4 py-3 dark:border-slate-800 dark:bg-brand-950/20"
								>
									<div className="flex items-center gap-2 text-xs text-slate-600 dark:text-slate-300">
										<span className="material-icons-round animate-spin text-sm text-brand-500">
											refresh
										</span>
										{bulkProgress.operation === 'republish' ? 'Republishing' : 'Dropping'} dead-lettered
										messages: {bulkProgress.processed} processed, {bulkProgress.failed} failed
									</div>
									<button
										type="button"
										className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-600 shadow-sm transition hover:border-rose-300 hover:bg-rose-50 hover:text-rose-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:border-rose-900 dark:hover:bg-rose-950/30 dark:hover:text-rose-300"
										onClick={handleBulkCancel}
									>
										<span className="material-icons-round text-sm">close</span>
										Cancel
									</button>
								</div>
							) : null}

							{activeMessages.length ? (
									<div className={`message-table-container overflow-x-auto${deadLetterMode ? ' dead-letter' : ''}`}>
										<table className="min-w-full text-left text-sm">
											<thead className="border-b border-slate-200 bg-slate-50 text-slate-500 dark:border-slate-800 dark:bg-slate-950/40 dark:text-slate-400">
												<tr>
													<th className="px-4 py-3 font-medium">{renderSortableHeader('messageId', 'Message ID')}</th>
													{showPartitionKey && <th className="col-partition-key px-4 py-3 font-medium">Partition Key</th>}
													<th className="col-session px-4 py-3 font-medium">{renderSortableHeader('session', 'Session')}</th>
													<th className="col-delivery-count px-4 py-3 font-medium">{renderSortableHeader('deliveryCount', 'Delivery Count')}</th>
													<th className="col-enqueued-time px-4 py-3 font-medium">{renderSortableHeader('enqueuedTime', 'Enqueued Time / TTL')}</th>
													{showScheduledEnqueue && <th className="col-scheduled-enqueue px-4 py-3 font-medium">Scheduled Enqueue</th>}
													{!deadLetterMode && <th className="col-content-type px-4 py-3 font-medium">{renderSortableHeader('contentType', 'Content Type')}</th>}
													{deadLetterMode && <th className="px-4 py-3 font-medium">{renderSortableHeader('deadLetterReason', 'Dead Letter Reason')}</th>}
													<th className="px-4 py-3 text-center font-medium">Actions</th>
												</tr>
											</thead>
											<tbody className="divide-y divide-slate-100 dark:divide-slate-800">
												{displayedMessageRows.map(({ loadIndex, message }) => {
													const rowKey = createMessageRowKey(message, loadIndex);
													const isExpanded = expandedRows.has(rowKey);
													const deadLetterPropertyRows: Array<[string, string]> = deadLetterMode
														? [
															['Dead Letter Reason', formatNullable(message.properties.deadLetterReason)],
															['Dead Letter Source', formatNullable(message.properties.deadLetterSource)],
															[
																	'Dead Letter Error Description',
																	formatNullable(message.properties.deadLetterErrorDescription),
																],
														]
														: [];
													const propertyRows: Array<[string, string, string?]> = [
														...deadLetterPropertyRows,
														['Scheduled Enqueue', formatDateTime(message.properties.scheduledEnqueueTime)],
														['Correlation ID', formatNullable(message.properties.correlationId)],
														[
															'Content Type',
															formatNullable(message.properties.contentType, 'not set'),
															deadLetterMode ? undefined : 'prop-content-type',
														],
														['Subject', formatNullable(message.properties.subject)],
														['To', formatNullable(message.properties.to)],
														['Reply To', formatNullable(message.properties.replyTo)],
														...(showPartitionKey
															? ([[
																'Partition Key',
																formatNullable(message.properties.partitionKey),
																'prop-partition-key',
															]] as Array<[string, string, string?]>)
															: []),
														['Session', formatNullable(message.properties.sessionId, 'No session'), 'prop-session'],
														['Delivery Count', String(message.properties.deliveryCount), 'prop-delivery-count'],
														[
															'Enqueued Time / TTL',
															`${formatDateTime(message.properties.enqueuedTimeUtc)} (TTL: ${formatNullable(message.properties.timeToLive)})`,
															'prop-enqueued-time',
														],
													];

													return (
														<Fragment key={rowKey}>
															<tr className={`${MESSAGE_ROW_BASE_CLASS}${removingMessageIds.get(message.properties.messageId) === 'drop' ? ' message-row-dropping' : removingMessageIds.has(message.properties.messageId) ? ' message-row-removing' : ''}`}>
																<td className="px-4 py-3">
																	<div className="font-mono text-xs text-slate-700 dark:text-slate-200">
																		{message.properties.messageId}
																	</div>
																</td>
																{showPartitionKey && (
																	<td className="col-partition-key px-4 py-3 font-mono text-xs text-slate-700 dark:text-slate-200">
																		{formatNullable(message.properties.partitionKey)}
																	</td>
																)}
																<td className="col-session px-4 py-3 text-xs text-slate-600 dark:text-slate-300">
																	{formatNullable(message.properties.sessionId, 'No session')}
																</td>
																<td className="col-delivery-count px-4 py-3 text-xs text-slate-600 dark:text-slate-300">
																	{message.properties.deliveryCount}
																</td>
																<td className="col-enqueued-time px-4 py-3 text-xs text-slate-600 dark:text-slate-300">
																	{formatDateTimeShort(message.properties.enqueuedTimeUtc)}
																	<div className="mt-1 text-[0.6875rem] text-slate-400 dark:text-slate-500">
																		TTL: {formatNullable(message.properties.timeToLive)}
																	</div>
																</td>
																{showScheduledEnqueue && (
																	<td className="col-scheduled-enqueue px-4 py-3 text-xs text-slate-600 dark:text-slate-300">
																		{message.properties.scheduledEnqueueTime
																			? formatDateTimeShort(message.properties.scheduledEnqueueTime)
																			: 'n/a'}
																	</td>
																)}
																{!deadLetterMode && (
																	<td className="col-content-type px-4 py-3 text-xs text-slate-600 dark:text-slate-300">
																		{formatNullable(message.properties.contentType, 'not set')}
																	</td>
																)}
																{deadLetterMode && (
																	<td className="px-4 py-3 text-xs text-slate-600 dark:text-slate-300">
																		{formatNullable(message.properties.deadLetterReason)}
																	</td>
																)}
																<td className="px-4 py-3">
																	<div className="flex items-center justify-end gap-2">
																		<CopyFullMessageButton message={message} />
																		{deadLetterMode ? (
																			<RepublishStrategyMenu
																				align="right"
																				ariaBusy={pendingAction === 'republish'}
																				buttonClassName="inline-flex h-7 items-center gap-1 rounded-lg bg-slate-100 px-2 text-[0.6875rem] font-medium text-slate-500 transition hover:bg-slate-200 hover:text-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700 dark:hover:text-white"
																				buttonContent={
																					<>
																						<span className="material-icons-round text-sm">
																									upload
																						</span>
																						<span className="material-icons-round text-sm">
																							arrow_drop_down
																						</span>
																					</>
																				}
																				buttonTitle="Republish"
																				disabled={deadLetterOperationInProgress}
																				menuTitle="Republish"
																				options={singleRepublishStrategyOptions}
																				onPick={(strategy) =>
																					handleSingleStrategyPick(
																						message.properties.messageId,
																						strategy,
																					)
																				}
																			/>
																		) : null}
																		{deadLetterMode ? (
																			<button
																				type="button"
																				aria-label="Drop"
																				className="inline-flex h-7 items-center rounded-lg bg-slate-100 px-2 text-[0.6875rem] font-medium text-slate-500 transition hover:bg-rose-100 hover:text-rose-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-500 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-slate-800 dark:text-slate-400 dark:hover:bg-rose-950/40 dark:hover:text-rose-300"
																				disabled={deadLetterOperationInProgress}
																				title="Drop"
																				onClick={() =>
																					handleDropSingle(
																						message.properties.messageId,
																						)
																				}
																			>
																				<span className="material-icons-round inline-block rotate-180 text-sm">
																					upload
																				</span>
																			</button>
																		) : null}
																		<button
																			type="button"
																			aria-label={isExpanded ? 'Collapse message details' : 'Expand message details'}
																			title={isExpanded ? 'Collapse' : 'Expand'}
																			className="inline-flex h-7 items-center rounded-lg bg-slate-100 px-2 text-[0.6875rem] font-medium text-slate-500 transition hover:bg-slate-200 hover:text-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700 dark:hover:text-white"
																			onClick={() => toggleExpandedRow(rowKey)}
																		>
																			<span className="material-icons-round text-sm">
																				{isExpanded ? 'keyboard_arrow_up' : 'keyboard_arrow_down'}
																			</span>
																		</button>
																	</div>
																</td>
															</tr>
															{isExpanded ? (
																<tr className="bg-slate-50/70 dark:bg-slate-950/50">
																	<td colSpan={peekedMessageColumnCount} className="max-w-0 px-4 py-4">
																		<div className="grid gap-6 xl:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)]">
																			<div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-800">
																				<table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
																					<thead className="bg-slate-50 text-slate-500 dark:bg-slate-950/40 dark:text-slate-400">
																						<tr>
																							<th className="px-4 py-2 text-left font-medium">Message Property</th>
																							<th className="px-4 py-2 text-left font-medium">Value</th>
																						</tr>
																					</thead>
																					<tbody className="divide-y divide-slate-100 dark:divide-slate-800">
																						{[...propertyRows]
																					.sort(([a], [b]) => (a < b ? -1 : a > b ? 1 : 0))
																					.map(([label, value, rowClassName]) => (
																							<tr key={`${rowKey}-${label}`} className={rowClassName}>
																								<th className="w-44 whitespace-nowrap bg-white px-4 py-2 text-xs font-medium text-slate-700 dark:bg-slate-900 dark:text-slate-200">
																									{label}
																								</th>
																								<td className="px-4 py-2 text-xs text-slate-600 dark:text-slate-300">
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

																		<JsonMessageBody body={message.body} className="mt-4" />
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
											{bulkSuccessMessage ?? (deadLetterMode
												? 'No dead-lettered messages found.'
												: 'No messages found.')}
										</div>
									</div>
								)}
							</section>
						</div>
					</main>
				</div>

				{republishPrompt ? (
					<RepublishManualIdPrompt
						messageId={republishPrompt.messageId}
						onCancel={handleRepublishPromptCancel}
						onConfirm={handleManualPromptConfirm}
					/>
				) : null}

				{republishAllConfirmStrategy !== null ? (
				<RepublishAllConfirmPrompt
					onCancel={handleRepublishAllCancel}
					onConfirm={handleRepublishAllConfirm}
					visibleCount={deadLetterViewer?.messages.length ?? 0}
				/>
			) : null}

			{dropConfirmOpen ? (
					<DropAllConfirmPrompt
						onCancel={handleDropAllCancel}
						onConfirm={handleDropAllConfirm}
						visibleCount={deadLetterViewer?.messages.length ?? 0}
					/>
				) : null}

				{dropSingleTarget !== null ? (
					<DropSingleConfirmPrompt
						messageId={dropSingleTarget}
						onCancel={handleDropSingleCancel}
						onConfirm={handleDropSingleConfirm}
					/>
				) : null}
			</div>
		</AppLayout>
	);
}
