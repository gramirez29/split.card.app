const API_URL = process.env.EXPO_PUBLIC_API_URL;

if (!API_URL) {
  throw new Error('EXPO_PUBLIC_API_URL is not set. Define it in your .env file.');
}

export const config = {
  apiUrl: API_URL,
} as const;
