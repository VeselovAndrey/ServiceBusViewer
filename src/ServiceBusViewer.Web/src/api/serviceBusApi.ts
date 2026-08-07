import type {
  ConnectRequestDto,
  EntityDetailsDto,
  EntityIdDto,
  ReceiveRequestDto,
  SessionStateDto,
  SendMessageRequestDto,
  ViewerState,
} from '../types/serviceBus';
import { ApiError, extractProblemMessages } from '../lib/problemDetails';

const apiBasePath = (import.meta.env.VITE_API_BASE_PATH ?? '/api').replace(
  /\/+$/,
  '',
);

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;

  try {
    const headers = new Headers(init?.headers);
    headers.set('Accept', 'application/json');
    if (init?.body) {
      headers.set('Content-Type', 'application/json');
    }

    response = await fetch(`${apiBasePath}${path}`, {
      credentials: 'include',
      headers,
      ...init,
    });
  } catch {
    throw new ApiError(['Unable to reach the Service Bus Viewer API.'], 0);
  }

  const rawText = response.status === 204 ? '' : await response.text();
  let payload: unknown = undefined;

  if (rawText) {
    try {
      payload = JSON.parse(rawText) as unknown;
    } catch {
      payload = rawText;
    }
  }

  if (!response.ok) {
    const messages = extractProblemMessages(payload);
    throw new ApiError(messages, response.status);
  }

  return payload as T;
}

export const serviceBusApi = {
  getSessionState() {
    return requestJson<SessionStateDto>('/session-state');
  },
  connect(request: ConnectRequestDto) {
    return requestJson<ViewerState>('/connection/connect', {
      body: JSON.stringify(request),
      method: 'POST',
    });
  },
  disconnect() {
    return requestJson<SessionStateDto>('/connection/disconnect', {
      method: 'POST',
    });
  },
  getViewer() {
    return requestJson<ViewerState>('/viewer');
  },
  refreshViewer() {
    return requestJson<ViewerState>('/viewer/refresh', {
      method: 'POST',
    });
  },
  selectEntity(entity: EntityIdDto) {
    return requestJson<ViewerState>('/viewer/select-entity', {
      body: JSON.stringify({
        selectedEntityName: entity.name,
        selectedEntityType: entity.type,
        selectedTopicName: entity.topicName,
      }),
      method: 'POST',
    });
  },
  receive(request: ReceiveRequestDto) {
    return requestJson<ViewerState>('/viewer/receive', {
      body: JSON.stringify(request),
      method: 'POST',
    });
  },
  send(request: SendMessageRequestDto) {
    return requestJson<ViewerState>('/viewer/send', {
      body: JSON.stringify(request),
      method: 'POST',
    });
  },
  getEntityDetails(type: string, name: string, topicName: string | null) {
    const searchParams = new URLSearchParams({ name, type });
    if (topicName) {
      searchParams.set('topicName', topicName);
    }

    return requestJson<EntityDetailsDto>(
      `/entities/details?${searchParams.toString()}`,
    );
  },
};
