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

  it('applies a tenant palette without leaking it to another tenant', () => {
    const service = TestBed.inject(ThemeService);
    service.applyContextDefault('dark');
    service.setTenantContext('TENANT-A');
    expect(service.setTenantPalette({ primary: '#123456', buttonDanger: '#654321' }, 'dark')).toBe(true);
    expect(document.documentElement.style.getPropertyValue('--cs-primary')).toBe('#123456');
    expect(document.documentElement.style.getPropertyValue('--cs-btn-danger')).toBe('#654321');

    service.setTenantContext('TENANT-B');
    expect(document.documentElement.style.getPropertyValue('--cs-primary')).toBe('');
    expect(document.documentElement.style.getPropertyValue('--cs-btn-danger')).toBe('');
  });

  it('keeps tenant light and dark palettes separate', () => {
    const service = TestBed.inject(ThemeService);
    service.setTenantContext('TENANT-A');
    service.setTenantPalette({ primary: '#123456' }, 'dark');
    service.setTenantPalette({ primary: '#abcdef' }, 'light');
    expect(service.paletteForEditor('dark').primary).toBe('#123456');
    expect(service.paletteForEditor('light').primary).toBe('#abcdef');
  });
});
