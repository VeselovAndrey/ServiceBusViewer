import type {
	ApplicationPropertyInputDto,
	ApplicationPropertyType,
	SendMessagePropertiesDto,
} from '../types/serviceBus';

const applicationPropertyTypeNames: ReadonlySet<string> = new Set([
	'String',
	'Bool',
	'Byte',
	'ByteArray',
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
	'Null',
	'Unknown',
]);

const supportedPropertyKeys = [
	'messageId',
	'sessionId',
	'correlationId',
	'contentType',
	'scheduledEnqueueTime',
	'timeToLive',
	'to',
	'replyTo',
	'subject',
	'partitionKey',
] as const satisfies readonly (keyof SendMessagePropertiesDto)[];

export type SendWindowJsonImport = {
	body: string | null;
	properties: Partial<SendMessagePropertiesDto> | null;
	applicationProperties: ApplicationPropertyInputDto[] | null;
};

export type SendWindowJsonImportResult =
	| { ok: true; value: SendWindowJsonImport }
	| { ok: false; error: string };

function isRecord(value: unknown): value is Record<string, unknown> {
	if (value === null || typeof value !== 'object') {
		return false;
	}

	const prototype = Object.getPrototypeOf(value);
	return prototype === Object.prototype || prototype === null;
}

function toSendValue(value: unknown): string {
	if (value === null || value === undefined) {
		return '';
	}

	if (typeof value === 'string' || typeof value === 'boolean') {
		return String(value);
	}

	if (typeof value === 'number') {
		return Number.isFinite(value) ? String(value) : JSON.stringify(value);
	}

	if (typeof value === 'bigint') {
		return value.toString();
	}

	return JSON.stringify(value);
}

function toDatetimeLocalValue(iso: string): string {
	const date = new Date(iso);
	if (Number.isNaN(date.getTime())) {
		return '';
	}

	const pad = (component: number) => String(component).padStart(2, '0');
	return [
		`${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`,
		`${pad(date.getHours())}:${pad(date.getMinutes())}`,
	].join('T');
}

function parseStringProperty(
	properties: Record<string, unknown>,
	key: (typeof supportedPropertyKeys)[number],
): string | undefined {
	if (!(key in properties)) {
		return undefined;
	}

	const raw = properties[key];
	if (raw === null || raw === undefined) {
		return '';
	}

	if (typeof raw !== 'string') {
		throw new Error(`Property '${key}' must be a string.`);
	}

	if (key === 'scheduledEnqueueTime') {
		return toDatetimeLocalValue(raw);
	}

	return raw;
}

function parseApplicationProperties(
	raw: Record<string, unknown> | null,
): ApplicationPropertyInputDto[] {
	if (raw === null) {
		return [];
	}

	const rows: ApplicationPropertyInputDto[] = [];
	for (const [key, entry] of Object.entries(raw)) {
		if (key === 'Diagnostic-Id') {
			continue;
		}

		if (!isRecord(entry) || !('value' in entry)) {
			throw new Error(`Application property '${key}' must be an object with a value.`);
		}

		const type = entry.type;
		if (typeof type !== 'string' || !applicationPropertyTypeNames.has(type)) {
			throw new Error(`Application property '${key}' has an unsupported type.`);
		}

		rows.push({
			key,
			type: type as ApplicationPropertyType,
			value: toSendValue(entry['value']),
		});
	}

	return rows;
}

export function parseSendWindowJson(text: string): SendWindowJsonImportResult {
	let parsed: unknown;
	try {
		parsed = JSON.parse(text);
	} catch {
		return { ok: false, error: '' };
	}

	if (!isRecord(parsed)) {
		return { ok: false, error: 'The pasted JSON must be an object with message fields.' };
	}

	try {
		const value: SendWindowJsonImport = {
			body: null,
			properties: null,
			applicationProperties: null,
		};

		if ('body' in parsed) {
			if (typeof parsed.body !== 'string') {
				throw new Error("The 'body' field must be a string.");
			}
			value.body = parsed.body;
		}

		if ('properties' in parsed) {
			const rawProperties = parsed.properties;
			if (!isRecord(rawProperties)) {
				throw new Error("'properties' must be an object.");
			}

			const patch: Partial<SendMessagePropertiesDto> = {};
			let hasSupportedProperty = false;
			for (const key of supportedPropertyKeys) {
				const mapped = parseStringProperty(rawProperties, key);
				if (mapped !== undefined) {
					patch[key] = mapped;
					hasSupportedProperty = true;
				}
			}
			value.properties = hasSupportedProperty ? patch : {};
		}

		if ('applicationProperties' in parsed) {
			const rawApplicationProperties = parsed.applicationProperties;
			if (rawApplicationProperties !== null && !isRecord(rawApplicationProperties)) {
				throw new Error("'applicationProperties' must be an object or null.");
			}
			value.applicationProperties = parseApplicationProperties(
				rawApplicationProperties === null ? null : rawApplicationProperties,
			);
		}

		return { ok: true, value };
	} catch (error: unknown) {
		const message =
			error instanceof Error ? error.message : 'The pasted JSON does not match the expected message format.';
		return { ok: false, error: message };
	}
}
