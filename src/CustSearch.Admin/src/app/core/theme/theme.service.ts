import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark' | 'system';
type ResolvedTheme = Exclude<ThemePreference, 'system'>;

const STORAGE_KEY = 'custsearch.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly media = this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)');
  private readonly systemTheme = signal<ResolvedTheme>(this.media?.matches ? 'dark' : 'light');
  private readonly storedPreference = this.readPreference();
  private readonly hasExplicitPreference = signal(this.storedPreference !== null);
  private readonly contextDefault = signal<ResolvedTheme>('light');
  readonly preference = signal<ThemePreference>(this.storedPreference ?? 'system');
  readonly resolvedTheme = computed<ResolvedTheme>(() =>
    this.hasExplicitPreference()
      ? (this.preference() === 'system' ? this.systemTheme() : this.preference() as ResolvedTheme)
      : this.contextDefault(),
  );

  private readonly onSystemChange = (event: MediaQueryListEvent): void => {
    this.systemTheme.set(event.matches ? 'dark' : 'light');
    this.apply();
  };

  constructor() {
    this.media?.addEventListener?.('change', this.onSystemChange);
    this.apply();
  }

  setPreference(preference: ThemePreference): void {
    this.hasExplicitPreference.set(true);
    this.preference.set(preference);
    try { this.document.defaultView?.localStorage.setItem(STORAGE_KEY, preference); } catch { /* storage may be disabled */ }
    this.apply();
  }

  applyContextDefault(theme: ResolvedTheme): void {
    this.contextDefault.set(theme);
    this.apply();
  }

  private readPreference(): ThemePreference | null {
    try {
      const saved = this.document.defaultView?.localStorage.getItem(STORAGE_KEY);
      if (saved === 'light' || saved === 'dark' || saved === 'system') return saved;
    } catch { /* use the safe default */ }
    return null;
  }

  private apply(): void {
    const root = this.document.documentElement;
    root.dataset['theme'] = this.resolvedTheme();
    root.style.colorScheme = this.resolvedTheme();
  }
}
