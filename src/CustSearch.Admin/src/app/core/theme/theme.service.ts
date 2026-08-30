import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { TenantThemeConfig, TenantThemeMode, TenantThemePalette, TENANT_THEME_KEYS } from './tenant-theme.models';

export type ThemePreference = 'light' | 'dark' | 'system';
type ResolvedTheme = Exclude<ThemePreference, 'system'>;

const STORAGE_KEY = 'custsearch.theme';
const TENANT_STORAGE_PREFIX = 'custsearch.tenant-theme.';
const HEX = /^#[0-9a-f]{6}$/i;
const DEFAULT_PALETTES:Record<TenantThemeMode,TenantThemePalette> = {
  dark:{primary:'#42d6c7',secondary:'#3ea6d8',sidebar:'#0d141d',sidebarText:'#dce8ef',sidebarMuted:'#8fa2b2',sidebarBorder:'#22303d',topbar:'#0b1118',background:'#0b1118',panel:'#111a24',panelHover:'#172431',panelRaised:'#192532',border:'#22303d',borderStrong:'#304152',text:'#eef4f8',textSecondary:'#a6b3c2',muted:'#6f8092',success:'#45d191',warning:'#f4bd4f',danger:'#ef6673',info:'#54a8ff',onPrimary:'#061916',buttonPrimary:'#42d6c7',buttonSecondary:'#3ea6d8',buttonSuccess:'#45d191',buttonWarning:'#f4bd4f',buttonDanger:'#ef6673',buttonNeutral:'#304152'},
  light:{primary:'#087f79',secondary:'#1777a8',sidebar:'#101c27',sidebarText:'#dce8ef',sidebarMuted:'#9aafbe',sidebarBorder:'#2a3d4b',topbar:'#f3f6f8',background:'#f3f6f8',panel:'#ffffff',panelHover:'#f1f7f7',panelRaised:'#f7fafb',border:'#d9e2e8',borderStrong:'#b9c8d2',text:'#16232d',textSecondary:'#536776',muted:'#718492',success:'#18794e',warning:'#9a6700',danger:'#bd3342',info:'#1769aa',onPrimary:'#ffffff',buttonPrimary:'#087f79',buttonSecondary:'#1777a8',buttonSuccess:'#18794e',buttonWarning:'#c58b16',buttonDanger:'#bd3342',buttonNeutral:'#536776'},
};
const TOKEN_MAP:Record<keyof TenantThemePalette,string> = {primary:'--cs-primary',secondary:'--cs-blue',sidebar:'--cs-sidebar',sidebarText:'--cs-sidebar-text',sidebarMuted:'--cs-sidebar-muted',sidebarBorder:'--cs-sidebar-border',topbar:'--cs-topbar',background:'--cs-bg',panel:'--cs-panel',panelHover:'--cs-panel-hover',panelRaised:'--cs-panel-raised',border:'--cs-border',borderStrong:'--cs-border-strong',text:'--cs-text',textSecondary:'--cs-secondary',muted:'--cs-muted',success:'--cs-success',warning:'--cs-warning',danger:'--cs-danger',info:'--cs-info',onPrimary:'--cs-on-primary',buttonPrimary:'--cs-btn-primary',buttonSecondary:'--cs-btn-secondary',buttonSuccess:'--cs-btn-success',buttonWarning:'--cs-btn-warning',buttonDanger:'--cs-btn-danger',buttonNeutral:'--cs-btn-neutral'};

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly media = this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)');
  private readonly systemTheme = signal<ResolvedTheme>(this.media?.matches ? 'dark' : 'light');
  private readonly storedPreference = this.readPreference();
  private readonly hasExplicitPreference = signal(this.storedPreference !== null);
  private readonly contextDefault = signal<ResolvedTheme>('light');
  private readonly tenantCodeState = signal<string|null>(null);
  private readonly tenantThemeState = signal<TenantThemeConfig>({ light:{}, dark:{} });
  readonly preference = signal<ThemePreference>(this.storedPreference ?? 'system');
  readonly resolvedTheme = computed<ResolvedTheme>(() =>
    this.hasExplicitPreference()
      ? (this.preference() === 'system' ? this.systemTheme() : this.preference() as ResolvedTheme)
      : this.contextDefault(),
  );
  readonly tenantCode = this.tenantCodeState.asReadonly();
  readonly tenantTheme = this.tenantThemeState.asReadonly();

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

  setTenantContext(tenantCode:string|null):void {
    const normalized=tenantCode?.trim().toUpperCase()||null;
    this.tenantCodeState.set(normalized);
    this.tenantThemeState.set(normalized?this.readTenantTheme(normalized):{light:{},dark:{}});
    this.apply();
  }

  paletteForEditor(mode:TenantThemeMode=this.resolvedTheme()):TenantThemePalette { return {...DEFAULT_PALETTES[mode],...(this.tenantThemeState()[mode]??{})}; }

  setTenantPalette(palette:Partial<TenantThemePalette>,mode:TenantThemeMode=this.resolvedTheme()):boolean {
    const tenantCode=this.tenantCodeState(); if(!tenantCode)return false;
    const safe=Object.fromEntries(Object.entries(palette).filter(([key,value])=>TENANT_THEME_KEYS.includes(key as keyof TenantThemePalette)&&typeof value==='string'&&HEX.test(value))) as Partial<TenantThemePalette>;
    const next={...this.tenantThemeState(),[mode]:{...this.tenantThemeState()[mode],...safe}} as TenantThemeConfig;
    this.tenantThemeState.set(next);
    try{this.document.defaultView?.localStorage.setItem(`${TENANT_STORAGE_PREFIX}${tenantCode}`,JSON.stringify(next));}catch{/* storage may be disabled */}
    this.apply(); return true;
  }

  resetTenantTheme():void {
    const tenantCode=this.tenantCodeState(); if(!tenantCode)return;
    this.tenantThemeState.set({light:{},dark:{}});
    try{this.document.defaultView?.localStorage.removeItem(`${TENANT_STORAGE_PREFIX}${tenantCode}`);}catch{/* storage may be disabled */}
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
    for(const key of TENANT_THEME_KEYS)root.style.removeProperty(TOKEN_MAP[key]);
    for(const [key,value] of Object.entries(this.tenantThemeState()[this.resolvedTheme()]))if(TENANT_THEME_KEYS.includes(key as keyof TenantThemePalette)&&typeof value==='string'&&HEX.test(value))root.style.setProperty(TOKEN_MAP[key as keyof TenantThemePalette],value);
  }

  private readTenantTheme(tenantCode:string):TenantThemeConfig { try{const value=JSON.parse(this.document.defaultView?.localStorage.getItem(`${TENANT_STORAGE_PREFIX}${tenantCode}`)??'null') as Partial<TenantThemeConfig>|null;return{light:this.clean(value?.light),dark:this.clean(value?.dark)};}catch{return{light:{},dark:{}}} }
  private clean(value:Partial<TenantThemePalette>|undefined):Partial<TenantThemePalette> { if(!value||typeof value!=='object')return{};return Object.fromEntries(Object.entries(value).filter(([key,item])=>TENANT_THEME_KEYS.includes(key as keyof TenantThemePalette)&&typeof item==='string'&&HEX.test(item))) as Partial<TenantThemePalette>; }
}
