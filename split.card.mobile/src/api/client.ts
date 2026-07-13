import * as SecureStore from 'expo-secure-store';
// SDK 54 movió la API clásica (cacheDirectory, downloadAsync) fuera del export por
// default de 'expo-file-system' hacia una API nueva basada en clases File/Directory.
// El subpath /legacy preserva la API clásica — si Expo llega a quitar este subpath en
// una versión futura, esta función necesita reescribirse contra File/Paths.
import * as FileSystem from 'expo-file-system/legacy';
import { config } from '@/constants/config';
import type { ApiResult } from '@/types/api';

const AUTH_TOKEN_KEY = 'splitcard_auth_token';
const REQUEST_TIMEOUT_MS = 15000;

async function getAuthToken(): Promise<string | null> {
  return SecureStore.getItemAsync(AUTH_TOKEN_KEY);
}

export async function setAuthToken(token: string): Promise<void> {
  await SecureStore.setItemAsync(AUTH_TOKEN_KEY, token);
}

export async function clearAuthToken(): Promise<void> {
  await SecureStore.deleteItemAsync(AUTH_TOKEN_KEY);
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  requiresAuth?: boolean;
}

export async function apiRequest<T>(
  path: string,
  options: RequestOptions = {},
): Promise<ApiResult<T>> {
  const { method = 'GET', body, requiresAuth = true } = options;

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };

  if (requiresAuth) {
    const token = await getAuthToken();

    if (!token) {
      return { ok: false, error: { status: 401, message: 'No authentication token found.' } };
    }

    headers.Authorization = `Bearer ${token}`;
  }

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

  try {
    const response = await fetch(`${config.apiUrl}${path}`, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
      signal: controller.signal,
    });

    clearTimeout(timeoutId);

    if (!response.ok) {
      const message = await response.text();
      return { ok: false, error: { status: response.status, message: message || response.statusText } };
    }

    if (response.status === 204) {
      return { ok: true, data: undefined as T };
    }

    const data = (await response.json()) as T;
    return { ok: true, data };
  } catch (error) {
    clearTimeout(timeoutId);
    const message = error instanceof Error ? error.message : 'Unknown network error.';
    return { ok: false, error: { status: 0, message } };
  }
}

/**
 * For binary responses (PDF) — apiRequest<T> always calls response.json(), which fails
 * on binary content, so this is a separate path. Downloads straight to disk via
 * FileSystem.downloadAsync (supports custom headers, so the Bearer token attaches the
 * same way apiRequest does it) and returns the local file:// URI to hand to
 * expo-sharing afterward.
 */
export async function apiDownloadFile(path: string, localFileName: string): Promise<ApiResult<string>> {
  const token = await getAuthToken();

  if (!token) {
    return { ok: false, error: { status: 401, message: 'No authentication token found.' } };
  }

  const localUri = `${FileSystem.cacheDirectory}${localFileName}`;

  try {
    const result = await FileSystem.downloadAsync(`${config.apiUrl}${path}`, localUri, {
      headers: { Authorization: `Bearer ${token}` },
    });

    if (result.status !== 200) {
      return { ok: false, error: { status: result.status, message: `Download failed with status ${result.status}.` } };
    }

    return { ok: true, data: result.uri };
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Unknown error downloading file.';
    return { ok: false, error: { status: 0, message } };
  }
}
