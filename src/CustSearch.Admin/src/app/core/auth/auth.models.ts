/** Describes the authoritative user, tenant, role, permission and store scope returned by the API. */
export interface CurrentUser {
  userId: number;
  tenantId: number | null;
  tenantCode: string | null;
  userName: string;
  displayName: string;
  email: string;
  isPlatformAdmin: boolean;
  roles: string[];
  permissions: string[];
  storeIds: number[];
}

/** Carries a short-lived access token and the server-authoritative identity attached to it. */
export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresUtc: string;
  user: CurrentUser;
}

/** Represents the validated current session returned by GET /api/auth/me. */
export interface CurrentSessionResponse {
  user: CurrentUser;
  accessTokenExpiresUtc: string;
}

/** Standardizes paging and sorting values shared by future typed feature clients. */
export interface PageQuery {
  pageNumber: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  filters?: Readonly<Record<string, string | number | boolean>>;
}

/** Standardizes paged API results so every future feature client consumes one shape. */
export interface PageResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
}

/** Describes the safe error envelope returned by the API without exposing internal details. */
export interface ApiErrorResponse {
  code: string;
  message: string;
  correlationId: string;
}
