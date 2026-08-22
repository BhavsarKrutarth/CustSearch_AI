import { Injectable, isDevMode } from '@angular/core';

type LogContext = Readonly<Record<string, string | number | boolean | null>>;

/**
 * Provides minimal client diagnostics without accepting secrets or arbitrary payloads.
 * Authoritative diagnostic and audit records remain server side.
 */
@Injectable({ providedIn: 'root' })
export class Logger {
  debug(message: string, context: LogContext = {}): void {
    if (isDevMode()) {
      console.debug(message, context);
    }
  }

  error(message: string, context: LogContext = {}): void {
    console.error(message, context);
  }
}
