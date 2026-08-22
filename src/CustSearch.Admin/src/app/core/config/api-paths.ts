/**
 * Same-origin paths keep development proxy and IIS deployment behavior aligned.
 */
export const API_PATHS = {
  apiBase: '/api',
  realtimeHub: '/hubs/realtime',
} as const;
