function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function collectValidationErrors(errors: unknown): string[] {
  if (!isRecord(errors)) {
    return [];
  }

  const messages: string[] = [];
  for (const value of Object.values(errors)) {
    if (Array.isArray(value)) {
      for (const item of value) {
        if (typeof item === 'string' && item.trim().length > 0) {
          messages.push(item);
        }
      }
    }
  }

  return messages;
}

export class ApiError extends Error {
  readonly messages: string[];
  readonly status: number;

  constructor(messages: string[], status: number) {
    super(messages[0] ?? 'Request failed.');
    this.messages = messages;
    this.status = status;
  }
}

export function extractProblemMessages(problem: unknown): string[] {
  if (problem instanceof ApiError) {
    return problem.messages;
  }

  if (typeof problem === 'string' && problem.trim().length > 0) {
    return [problem];
  }

  if (!isRecord(problem)) {
    return ['Something went wrong.'];
  }

  const validationMessages = collectValidationErrors(problem.errors);
  if (validationMessages.length > 0) {
    return validationMessages;
  }

  if (typeof problem.detail === 'string' && problem.detail.trim().length > 0) {
    return [problem.detail];
  }

  if (typeof problem.title === 'string' && problem.title.trim().length > 0) {
    return [problem.title];
  }

  return ['Something went wrong.'];
}

export function getErrorMessages(error: unknown): string[] {
  if (error instanceof ApiError) {
    return error.messages;
  }

  if (error instanceof Error && error.message.trim().length > 0) {
    return [error.message];
  }

  return extractProblemMessages(error);
}
