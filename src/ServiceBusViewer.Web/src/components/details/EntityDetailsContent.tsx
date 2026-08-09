import { formatBoolean, formatNullable, formatUnknownValue } from '../../lib/formatters';
import type {
	CorrelationSubscriptionRuleDto,
	EntityDetailsDto,
	QueueEntityPropertiesDto,
	SqlSubscriptionRuleDto,
	SubscriptionEntityPropertiesDto,
	SubscriptionRuleDto,
	TopicEntityPropertiesDto,
	UnknownSubscriptionRuleDto,
} from '../../types/serviceBus';

function hasProperty<T extends string>(
	value: unknown,
	propertyName: T,
): value is Record<T, unknown> {
	return typeof value === 'object' && value !== null && propertyName in value;
}

function getRuleKind(
	rule: SubscriptionRuleDto,
): 'sql' | 'correlation' | 'unknown' | 'unsupported' {
	if (!hasProperty(rule, 'kind')) {
		return 'unsupported';
	}

	switch (rule.kind) {
		case 'Sql':
			return 'sql';
		case 'Correlation':
			return 'correlation';
		case 'Unknown':
			return 'unknown';
		default:
			return 'unsupported';
	}
}

interface EntityDetailsContentProps {
	details: EntityDetailsDto;
	errorMessage: string | null;
}

export function EntityDetailsContent({
	details,
	errorMessage,
}: EntityDetailsContentProps) {
	const properties = details.properties;

	const renderRows = () => {
		if (!properties) {
			return null;
		}

		switch (details.type) {
			case 'Queue': {
				const queue = properties as QueueEntityPropertiesDto;
				return [
					['Name', queue.name],
					['Type', 'Queue'],
					['Lock Duration', queue.lockDuration],
					['Requires Session', formatBoolean(queue.requiresSession)],
					['Enable Partitioning', formatBoolean(queue.enablePartitioning)],
					['Enable Batched Operations', formatBoolean(queue.enableBatchedOperations)],
					['Max Delivery Count', String(queue.maxDeliveryCount)],
					['Default TTL', queue.defaultMessageTimeToLive],
					[
						'Requires Duplicate Detection',
						formatBoolean(queue.requiresDuplicateDetection),
					],
					['Duplicate Detection Window', queue.duplicateDetectionHistoryTimeWindow],
					[
						'Dead-letter on Expiration',
						formatBoolean(queue.deadLetteringOnMessageExpiration),
					],
					['Auto Delete on Idle', queue.autoDeleteOnIdle],
				];
			}
			case 'Topic': {
				const topic = properties as TopicEntityPropertiesDto;
				return [
					['Name', topic.name],
					['Type', 'Topic'],
					['Enable Partitioning', formatBoolean(topic.enablePartitioning)],
					['Enable Batched Operations', formatBoolean(topic.enableBatchedOperations)],
					['Default TTL', topic.defaultMessageTimeToLive],
					[
						'Requires Duplicate Detection',
						formatBoolean(topic.requiresDuplicateDetection),
					],
					['Duplicate Detection Window', topic.duplicateDetectionHistoryTimeWindow],
					['Auto Delete on Idle', topic.autoDeleteOnIdle],
				];
			}
			case 'Subscription': {
				const subscription = properties as SubscriptionEntityPropertiesDto;
				return [
					['Name', subscription.name],
					['Type', 'Subscription'],
					['Topic', subscription.topicName],
					['Lock Duration', subscription.lockDuration],
					['Requires Session', formatBoolean(subscription.requiresSession)],
					[
						'Enable Batched Operations',
						formatBoolean(subscription.enableBatchedOperations),
					],
					['Max Delivery Count', String(subscription.maxDeliveryCount)],
					['Default TTL', subscription.defaultMessageTimeToLive],
					[
						'Dead-letter on Expiration',
						formatBoolean(subscription.deadLetteringOnMessageExpiration),
					],
					['Auto Delete on Idle', subscription.autoDeleteOnIdle],
				];
			}
		}
	};

	const rows = renderRows();
	const subscriptionRules =
		details.type === 'Subscription' && properties
			? (properties as SubscriptionEntityPropertiesDto).rules
			: [];

	return (
		<div className="custom-scrollbar flex-1 overflow-y-auto p-4 lg:p-6">
			{errorMessage ? (
				<div className="mb-6 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900/70 dark:bg-rose-950/40 dark:text-rose-200">
					<div className="flex items-start gap-3">
						<svg
							className="mt-0.5 h-5 w-5 shrink-0"
							viewBox="0 0 24 24"
							fill="none"
							stroke="currentColor"
							strokeWidth="1.8"
							aria-hidden="true"
						>
							<circle cx="12" cy="12" r="9" />
							<path strokeLinecap="round" strokeLinejoin="round" d="M12 8v5m0 3h.01" />
						</svg>
						<div>{errorMessage}</div>
					</div>
				</div>
			) : null}

			<section className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm shadow-slate-200/40 dark:border-slate-800 dark:bg-slate-900 dark:shadow-black/20">
				<div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
					<h2 className="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">
						Entity properties
					</h2>
					<span className="rounded-full bg-slate-100 px-2 py-1 text-[11px] text-slate-500 dark:bg-slate-800 dark:text-slate-400">
						Read-only
					</span>
				</div>

				<div className="p-4 lg:p-6">
					{rows ? (
						<div className="overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-800">
							<table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
								<tbody className="divide-y divide-slate-100 dark:divide-slate-800">
									{rows.map(([label, value], index) => (
										<tr
											key={label}
											className={index % 2 === 0 ? 'bg-slate-50/70 dark:bg-slate-950/40' : undefined}
										>
											<th className="w-1/3 px-4 py-3 text-left font-medium text-slate-500 dark:text-slate-400">
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
					) : (
						<div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-950/50 dark:text-slate-400">
							Property list is empty.
						</div>
					)}

					{subscriptionRules.length > 0 ? (
						<div className="mt-6 space-y-3">
							<div className="flex items-center justify-between">
								<h3 className="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">
									Subscription rules
								</h3>
								<span className="rounded-full bg-slate-100 px-2 py-1 text-[11px] text-slate-500 dark:bg-slate-800 dark:text-slate-400">
									{subscriptionRules.length} total
								</span>
							</div>

							{subscriptionRules.map((rule, index) => {
								const ruleKind = getRuleKind(rule);

								return (
									<article
										key={`${formatNullable((rule as { name?: string }).name, 'rule')}-${index}`}
										className="rounded-2xl border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-800 dark:bg-slate-950/60"
									>
										<div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
											<div>
												<h4 className="text-sm font-semibold text-slate-900 dark:text-white">
													{formatNullable((rule as { name?: string }).name, 'Unnamed rule')}
												</h4>
												<p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
													{ruleKind === 'sql'
														? 'SQL filter'
														: ruleKind === 'correlation'
															? 'Correlation filter'
															: ruleKind === 'unknown'
																? formatNullable((rule as UnknownSubscriptionRuleDto).filterTypeName, 'Unknown filter')
																: 'Unsupported rule'}
												</p>
											</div>
										</div>

										<div className="mt-4 space-y-3 text-sm">
											{ruleKind === 'sql'
												? (() => {
													const sqlRule = rule as SqlSubscriptionRuleDto;
													return (
														<>
															<div>
																<div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
																	Expression
																</div>
																<pre className="mt-2 overflow-x-auto rounded-2xl border border-slate-200 bg-white p-3 font-mono text-xs text-slate-700 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200">
																	{sqlRule.sqlExpression}
																</pre>
															</div>
															{sqlRule.actionExpression ? (
																<div>
																	<div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
																		Action
																	</div>
																	<pre className="mt-2 overflow-x-auto rounded-2xl border border-slate-200 bg-white p-3 font-mono text-xs text-slate-700 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200">
																		{sqlRule.actionExpression}
																	</pre>
																</div>
															) : null}
														</>
													);
												})()
												: null}

											{ruleKind === 'correlation'
												? (() => {
													const correlationRule = rule as CorrelationSubscriptionRuleDto;
													const cards = [
														['Correlation ID', correlationRule.correlationId],
														['Message ID', correlationRule.messageId],
														['To', correlationRule.to],
														['Reply To', correlationRule.replyTo],
														['Subject', correlationRule.subject],
														['Session ID', correlationRule.sessionId],
														['Reply To Session ID', correlationRule.replyToSessionId],
														['Content Type', correlationRule.contentType],
													].filter((entry): entry is [string, string] => Boolean(entry[1]));

													return (
														<>
															{cards.length > 0 ? (
																<div className="grid gap-3 sm:grid-cols-2">
																	{cards.map(([label, value]) => (
																		<div
																			key={label}
																			className="rounded-2xl border border-slate-200 bg-white px-3 py-3 dark:border-slate-800 dark:bg-slate-900"
																		>
																			<div className="text-[11px] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
																				{label}
																			</div>
																			<div className="mt-1 text-xs text-slate-700 dark:text-slate-200">
																				{value}
																			</div>
																		</div>
																	))}
																</div>
															) : null}

															{Object.keys(correlationRule.applicationProperties ?? {}).length > 0 ? (
																<div className="overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-800">
																	<table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
																		<thead className="bg-slate-50 text-slate-500 dark:bg-slate-950/50 dark:text-slate-400">
																			<tr>
																				<th className="px-4 py-2 text-left font-medium">
																					Application Property
																				</th>
																				<th className="px-4 py-2 text-left font-medium">
																					Value
																				</th>
																			</tr>
																		</thead>
																		<tbody className="divide-y divide-slate-100 dark:divide-slate-800">
																			{Object.entries(correlationRule.applicationProperties).map(([key, value]) => (
																				<tr key={key}>
																					<td className="px-4 py-2 font-mono text-xs text-slate-700 dark:text-slate-200">
																						{key}
																					</td>
																					<td className="px-4 py-2 text-xs text-slate-600 dark:text-slate-300">
																						{formatUnknownValue(value)}
																					</td>
																				</tr>
																			))}
																		</tbody>
																	</table>
																</div>
															) : null}

															{correlationRule.actionExpression ? (
																<div>
																	<div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
																		Action
																	</div>
																	<pre className="mt-2 overflow-x-auto rounded-2xl border border-slate-200 bg-white p-3 font-mono text-xs text-slate-700 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200">
																		{correlationRule.actionExpression}
																	</pre>
																</div>
															) : null}
														</>
													);
												})()
												: null}

											{ruleKind === 'unknown'
												? (() => {
													const unknownRule = rule as UnknownSubscriptionRuleDto;
													return (
														<div className="grid gap-3">
															<div className="rounded-2xl border border-slate-200 bg-white px-3 py-3 dark:border-slate-800 dark:bg-slate-900">
																<div className="text-[11px] font-bold uppercase tracking-[0.18em] text-slate-400 dark:text-slate-500">
																	Filter Type
																</div>
																<div className="mt-1 text-xs text-slate-700 dark:text-slate-200">
																	{unknownRule.filterTypeName}
																</div>
															</div>
															<div>
																<div className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
																	Details
																</div>
																<pre className="mt-2 overflow-x-auto rounded-2xl border border-slate-200 bg-white p-3 font-mono text-xs text-slate-700 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200">
																	{unknownRule.filterExpression}
																</pre>
															</div>
														</div>
													);
												})()
												: null}

											{ruleKind === 'unsupported' ? (
												<div className="text-sm text-slate-500 dark:text-slate-400">
													Unsupported rule type.
												</div>
											) : null}
										</div>
									</article>
								);
							})}
						</div>
					) : null}
				</div>
			</section>
		</div>
	);
}
