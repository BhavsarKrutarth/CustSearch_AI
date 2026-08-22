import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('persists and applies an explicit theme', () => {
    const service = TestBed.inject(ThemeService);
    service.setPreference('dark');
    expect(service.resolvedTheme()).toBe('dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(localStorage.getItem('custsearch.theme')).toBe('dark');
  });

  it('accepts system preference', () => {
    const service = TestBed.inject(ThemeService);
    service.setPreference('system');
    expect(['light', 'dark']).toContain(service.resolvedTheme());
  });

  it('does not replace an explicitly stored preference with a route default', () => {
    localStorage.setItem('custsearch.theme', 'light');
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
    const service = TestBed.inject(ThemeService);
    service.applyContextDefault('dark');
    expect(service.preference()).toBe('light');
    expect(service.resolvedTheme()).toBe('light');
  });
});
