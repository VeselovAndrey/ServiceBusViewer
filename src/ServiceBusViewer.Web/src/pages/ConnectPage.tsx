import {
	useEffect,
	useRef,
	useState,
	type ChangeEvent,
	type FormEvent,
} from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { serviceBusApi } from '../api/serviceBusApi';
import { Alert } from '../components/common/Alert';
import { AppLayout } from '../components/layout/AppLayout';
import { getErrorMessages } from '../lib/problemDetails';
import { useAppState } from '../state/AppStateContext';
import type { ConnectRequestDto } from '../types/serviceBus';

const emptyConnectRequest: ConnectRequestDto = {
	connectionString: '',
	emulatorManagementConnectionString: null,
	queueOrTopicName: null,
	subscriptionName: null,
};

export function ConnectPage() {
	const navigate = useNavigate();
	const { sessionState, refreshSessionState } = useAppState();
	const [formState, setFormState] = useState<ConnectRequestDto>(emptyConnectRequest);
	const [isSubmitting, setIsSubmitting] = useState(false);
	const [errorMessages, setErrorMessages] = useState<string[]>([]);
	const isInitializedRef = useRef(false);

	useEffect(() => {
		if (!sessionState || isInitializedRef.current) {
			return;
		}

		isInitializedRef.current = true;
		setFormState({
			connectionString: sessionState.connection.connectionString,
			emulatorManagementConnectionString: sessionState.connection.emulatorManagementConnectionString,
			queueOrTopicName: sessionState.connection.queueOrTopicName,
			subscriptionName: sessionState.connection.subscriptionName,
		});
	}, [sessionState]);

	if (!sessionState) {
		return null;
	}

	if (sessionState.isConnected) {
		return <Navigate replace to="/viewer" />;
	}

	const handleChange = (key: keyof ConnectRequestDto, event: ChangeEvent<HTMLInputElement>) => {
		const value = event.target.value;
		setFormState((current) => ({
			...current,
			[key]: value.trim().length === 0 && key !== 'connectionString' ? null : value,
		}));
	};

	const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
		event.preventDefault();
		const validationErrors: string[] = [];

		if (formState.connectionString.trim().length === 0) {
			validationErrors.push('Connection string is required.');
		}

		if (validationErrors.length > 0) {
			setErrorMessages(validationErrors);
			return;
		}

		setIsSubmitting(true);
		setErrorMessages([]);

		try {
			await serviceBusApi.connect(formState);
			await refreshSessionState();
			navigate('/viewer', { replace: true });
		} catch (error: unknown) {
			setErrorMessages(getErrorMessages(error));
		} finally {
			setIsSubmitting(false);
		}
	};

	return (
		<AppLayout
			applicationVersion={sessionState.applicationVersion}
			headerStatus={{ text: 'Ready for a connection', tone: 'ready' }}
			title="Connect"
		>
			<div className="flex flex-1 flex-col bg-slate-100 text-slate-800 dark:bg-slate-950 dark:text-slate-100">
				<main className="flex flex-1 items-center px-4 pt-0 pb-4 lg:px-8 lg:pt-0 lg:pb-4">
					<div className="mx-auto flex w-full max-w-[2124px] items-stretch gap-6">
						<section className="hidden w-[350px] shrink-0 flex-col justify-center rounded-3xl border border-slate-200 bg-white/85 p-6 shadow-xl shadow-slate-200/40 backdrop-blur dark:border-slate-800 dark:bg-slate-900/85 dark:shadow-black/20 min-[760px]:flex min-[1151px]:w-[700px] lg:p-8">
							<div className="mt-5 flex justify-center">
								<img
									src="/images/app-logo-576x576.png"
									alt="Service Bus Viewer logo"
									className="h-auto w-auto max-w-[18rem] object-contain"
								/>
							</div>

							<div className="mt-8 grid gap-4 sm:grid-cols-2">
								{[
									{
										body: 'Azure management permission is detected automatically to browse every queue, topic, and subscription.',
										iconBackground:
											'bg-emerald-100 text-emerald-700 dark:bg-emerald-950/70 dark:text-emerald-300',
										title: 'Namespace browsing',
										svg: (
											<>
												<path
													strokeLinecap="round"
													strokeLinejoin="round"
													d="M4 7.75A2.75 2.75 0 0 1 6.75 5h10.5A2.75 2.75 0 0 1 20 7.75v8.5A2.75 2.75 0 0 1 17.25 19H6.75A2.75 2.75 0 0 1 4 16.25Z"
												/>
												<path strokeLinecap="round" strokeLinejoin="round" d="M8 9.5h8M8 13h5" />
											</>
										),
									},
									{
										body: 'Peek, receive, inspect, and send messages with system and app properties.',
										iconBackground:
											'bg-violet-100 text-violet-700 dark:bg-violet-950/70 dark:text-violet-300',
										title: 'Message operations',
										svg: (
											<>
												<path
													strokeLinecap="round"
													strokeLinejoin="round"
													d="M6 19.25h12M7.75 16.5h8.5A1.75 1.75 0 0 0 18 14.75v-8.5A1.75 1.75 0 0 0 16.25 4.5h-8.5A1.75 1.75 0 0 0 6 6.25v8.5A1.75 1.75 0 0 0 7.75 16.5Z"
												/>
												<path strokeLinecap="round" strokeLinejoin="round" d="M9 8.5h6m-6 3h6" />
											</>
										),
									},
									{
										body: 'Add the optional emulator management endpoint to browse entities, or provide a queue or subscription for direct access.',
										iconBackground:
											'bg-amber-100 text-amber-700 dark:bg-amber-950/70 dark:text-amber-300',
										title: 'Emulator-friendly',
										svg: (
											<>
												<path strokeLinecap="round" strokeLinejoin="round" d="M12 3.5 20 7v10l-8 3.5L4 17V7l8-3.5Z" />
												<path strokeLinecap="round" strokeLinejoin="round" d="M4 7l8 3.5L20 7M12 10.5V20.5" />
											</>
										),
									},
									{
										body: 'Use one Azure connection string, or provide a queue or subscription when management access is unavailable.',
										iconBackground:
											'bg-brand-100 text-brand-700 dark:bg-brand-900/60 dark:text-brand-300',
										title: 'Connection modes',
										svg: (
											<>
												<path
													strokeLinecap="round"
													strokeLinejoin="round"
													d="M6 7.75A2.75 2.75 0 0 1 8.75 5h6.5A2.75 2.75 0 0 1 18 7.75v8.5A2.75 2.75 0 0 1 15.25 19h-6.5A2.75 2.75 0 0 1 6 16.25Z"
												/>
												<path strokeLinecap="round" strokeLinejoin="round" d="M9 9.5h6M9 13h6" />
											</>
										),
									},
								].map((card) => (
									<div
										key={card.title}
										className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-800 dark:bg-slate-950/60"
									>
										<span
											className={`inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ${card.iconBackground}`}
										>
											<svg
												className="h-5 w-5"
												viewBox="0 0 24 24"
												fill="none"
												stroke="currentColor"
												strokeWidth="1.8"
												aria-hidden="true"
											>
												{card.svg}
											</svg>
										</span>
										<div>
											<div className="text-sm font-semibold text-slate-900 dark:text-white">
												{card.title}
											</div>
											<div className="text-xs text-slate-500 dark:text-slate-400">
												{card.body}
											</div>
										</div>
									</div>
								))}
							</div>
						</section>

						<section className="min-w-0 max-w-[1400px] flex-1 rounded-3xl border border-slate-200 bg-white/90 p-6 shadow-xl shadow-slate-200/40 backdrop-blur dark:border-slate-800 dark:bg-slate-900/90 dark:shadow-black/20 lg:p-8">
							<div className="flex items-center justify-between gap-4 border-b border-slate-200 pb-4 dark:border-slate-800">
								<div>
									<h2 className="text-lg font-semibold text-slate-900 dark:text-white">
										Connect to Service Bus
									</h2>
									<p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
										Azure management access is detected automatically. Direct entity access remains available when needed.
									</p>
								</div>
							</div>

							<Alert className="mt-5" messages={errorMessages} tone="error" />

							<form className="mt-6 space-y-5" onSubmit={(event) => void handleSubmit(event)}>
								<div>
									<label className="mb-2 block text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
										Connection String
									</label>
									<input
										className="block w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-800 shadow-sm shadow-slate-200/50 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
										placeholder="Endpoint=sb://localhost;SharedAccessKeyName=..."
										value={formState.connectionString}
										onChange={(event) => handleChange('connectionString', event)}
									/>
								</div>

								<div>
									<label className="mb-2 block text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
										Emulator Management Connection String
									</label>
									<input
										className="block w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-800 shadow-sm shadow-slate-200/50 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
										placeholder="Optional, emulator administration endpoint"
										value={formState.emulatorManagementConnectionString ?? ''}
										onChange={(event) =>
											handleChange('emulatorManagementConnectionString', event)
										}
									/>
									<p className="mt-2 text-xs leading-5 text-slate-500 dark:text-slate-400">
										Optional and emulator-only. Azure uses the primary connection string for both discovery and messages.
									</p>
								</div>

								<div className="grid gap-5 sm:grid-cols-2">
									<div>
										<label className="mb-2 block text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
											Queue or Topic
										</label>
										<input
											className="block w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-800 shadow-sm shadow-slate-200/50 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
											placeholder="Required when management is unavailable"
											value={formState.queueOrTopicName ?? ''}
											onChange={(event) => handleChange('queueOrTopicName', event)}
										/>
										<p className="mt-2 text-xs leading-5 text-slate-500 dark:text-slate-400">
											Without management access, enter a queue name or pair a topic name with a subscription.
										</p>
									</div>

									<div>
										<label className="mb-2 block text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">
											Subscription
										</label>
										<input
											className="block w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-800 shadow-sm shadow-slate-200/50 transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100 dark:shadow-black/20 dark:focus:border-brand-400"
											placeholder="Optional for topic subscriptions"
											value={formState.subscriptionName ?? ''}
											onChange={(event) => handleChange('subscriptionName', event)}
										/>
									</div>
								</div>

								{sessionState.isRunningInContainer && (
									<div className="flex items-center gap-2 rounded-2xl border border-slate-200 bg-slate-50/80 px-4 py-3 text-xs leading-5 text-slate-500 dark:border-slate-800 dark:bg-slate-950/60 dark:text-slate-400">
										<span className="material-icons-round text-base" aria-hidden="true">
											info
										</span>
										<span>
											For Docker or Podman, use{' '}
											<span className="font-mono text-slate-700 dark:text-slate-200">
												host.docker.internal
											</span>{' '}
											instead of{' '}
											<span className="font-mono text-slate-700 dark:text-slate-200">
												localhost
											</span>{' '}
											when targeting the emulator on the host machine.
										</span>
									</div>
								)}

								<button
									type="submit"
									className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-brand-600 px-4 py-3 text-sm font-semibold text-white shadow-lg shadow-brand-950/20 transition hover:bg-brand-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 focus-visible:ring-offset-white dark:focus-visible:ring-offset-slate-900"
									disabled={isSubmitting}
								>
									<svg
										className="h-4 w-4"
										viewBox="0 0 24 24"
										fill="none"
										stroke="currentColor"
										strokeWidth="1.8"
										aria-hidden="true"
									>
										<path strokeLinecap="round" strokeLinejoin="round" d="M5 12h14M13 6l6 6-6 6" />
									</svg>
									{isSubmitting ? 'Connecting...' : 'Connect'}
								</button>
							</form>
						</section>
					</div>
				</main>
			</div>
		</AppLayout>
	);
}
