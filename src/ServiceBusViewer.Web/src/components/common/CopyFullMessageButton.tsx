import { buildFullMessageJson } from '../../lib/messageJson';
import { cx } from '../../lib/cx';
import { CopyButton } from './CopyButton';
import type { ReceivedMessageDto } from '../../types/serviceBus';

interface CopyFullMessageButtonProps {
	message?: ReceivedMessageDto | null;
	disabled?: boolean;
	className?: string;
}

export function CopyFullMessageButton({ message, disabled = false, className }: CopyFullMessageButtonProps) {
	return (
		<CopyButton
			text={message ? buildFullMessageJson(message) : null}
			label="Copy as JSON"
			icon="data_object"
			copiedText="JSON copied"
			ariaLabel="Copy full message as JSON"
			disabled={disabled || !message}
			disabledTitle="Receive a message first to copy it as JSON"
			className={cx('min-w-[132px] px-2.5', className)}
		/>
	);
}
