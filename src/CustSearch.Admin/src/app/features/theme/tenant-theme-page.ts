import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { TenantThemeMode, TenantThemePalette } from '../../core/theme/tenant-theme.models';
import { ThemeService } from '../../core/theme/theme.service';

interface ThemeField { key:keyof TenantThemePalette; label:string; group:string; }

@Component({
  selector:'app-tenant-theme-page',
  imports:[AdminShell,FormsModule],
  templateUrl:'./tenant-theme-page.html',
  styleUrl:'./tenant-theme-page.scss',
  changeDetection:ChangeDetectionStrategy.OnPush,
})
export class TenantThemePage implements OnInit {
  protected readonly theme=inject(ThemeService);
  protected readonly mode=signal<TenantThemeMode>(this.theme.resolvedTheme());
  protected readonly palette=signal<TenantThemePalette>(this.theme.paletteForEditor());
  protected readonly message=signal('');
  protected readonly fields:readonly ThemeField[]=[
    {key:'primary',label:'Primary brand',group:'Brand'}, {key:'secondary',label:'Secondary brand',group:'Brand'}, {key:'sidebar',label:'Sidebar',group:'Surface'},
    {key:'sidebarText',label:'Sidebar text',group:'Surface'}, {key:'sidebarMuted',label:'Sidebar muted text',group:'Surface'}, {key:'sidebarBorder',label:'Sidebar border',group:'Surface'},
    {key:'topbar',label:'Top bar',group:'Surface'}, {key:'background',label:'Workspace background',group:'Surface'}, {key:'panel',label:'Panel',group:'Surface'},
    {key:'panelHover',label:'Panel hover',group:'Surface'}, {key:'panelRaised',label:'Raised panel',group:'Surface'}, {key:'border',label:'Border',group:'Surface'},
    {key:'borderStrong',label:'Strong border',group:'Surface'}, {key:'text',label:'Primary text',group:'Text'}, {key:'textSecondary',label:'Secondary text',group:'Text'},
    {key:'muted',label:'Muted text',group:'Text'}, {key:'buttonPrimary',label:'Primary button',group:'Actions'}, {key:'buttonSecondary',label:'Secondary button',group:'Actions'},
    {key:'buttonSuccess',label:'Success button',group:'Actions'}, {key:'buttonWarning',label:'Warning button',group:'Actions'}, {key:'buttonDanger',label:'Danger button',group:'Actions'},
    {key:'buttonNeutral',label:'Neutral button',group:'Actions'}, {key:'success',label:'Success status',group:'Status'}, {key:'warning',label:'Warning status',group:'Status'},
    {key:'danger',label:'Danger status',group:'Status'}, {key:'info',label:'Info status',group:'Status'}, {key:'onPrimary',label:'Primary button text',group:'Actions'},
  ];

  ngOnInit():void { this.loadMode(this.theme.resolvedTheme()); }
  protected changeMode(value:string):void { const mode=value as TenantThemeMode; this.mode.set(mode); this.palette.set(this.theme.paletteForEditor(mode)); this.message.set(''); }
  protected setColor(key:keyof TenantThemePalette,value:string):void { this.palette.update(current=>({...current,[key]:value})); this.message.set('Preview updated. Apply the theme to save it for this workspace.'); }
  protected apply():void { this.theme.setTenantPalette(this.palette(),this.mode()); this.message.set(`Workspace ${this.mode()} theme applied for this tenant.`); }
  protected reset():void { this.theme.resetTenantTheme(); this.loadMode(this.mode()); this.message.set('CustSearch AI default theme restored for this tenant.'); }
  protected groupStart(index:number):boolean { return index===0||this.fields[index-1].group!==this.fields[index].group; }
  private loadMode(mode:TenantThemeMode):void { this.mode.set(mode); this.palette.set(this.theme.paletteForEditor(mode)); }
}
