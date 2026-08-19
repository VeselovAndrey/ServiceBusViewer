import type { ReceivedMessageDto } from '../types/serviceBus';

export function buildFullMessageJson(message: ReceivedMessageDto): string {
	return JSON.stringify(
		{
			body: message.body,
			properties: message.properties,
			applicationProperties: message.applicationProperties,
		},
		undefined,
		2,
	);
}
