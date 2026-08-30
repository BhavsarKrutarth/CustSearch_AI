export interface TenantThemePalette {
  primary:string; secondary:string; sidebar:string; sidebarText:string; sidebarMuted:string; sidebarBorder:string; topbar:string; background:string; panel:string; panelHover:string; panelRaised:string;
  border:string; borderStrong:string; text:string; textSecondary:string; muted:string; success:string; warning:string; danger:string; info:string;
  onPrimary:string; buttonPrimary:string; buttonSecondary:string; buttonSuccess:string; buttonWarning:string; buttonDanger:string; buttonNeutral:string;
}

export type TenantThemeMode = 'light'|'dark';
export type TenantThemeConfig = Record<TenantThemeMode,Partial<TenantThemePalette>>;

export const TENANT_THEME_KEYS:readonly (keyof TenantThemePalette)[] = [
  'primary','secondary','sidebar','sidebarText','sidebarMuted','sidebarBorder','topbar','background','panel','panelHover','panelRaised','border','borderStrong','text','textSecondary','muted',
  'success','warning','danger','info','onPrimary','buttonPrimary','buttonSecondary','buttonSuccess','buttonWarning','buttonDanger','buttonNeutral',
];
