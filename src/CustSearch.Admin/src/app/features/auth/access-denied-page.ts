import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Explains an API or route-level 403 without pretending that hidden navigation is security. */
@Component({
  selector: 'app-access-denied-page',
  imports: [RouterLink],
  template: `<main class="denied"><p>403</p><h1>Access denied</h1><span>Your account does not have permission for this area.</span><a routerLink="/login">Return to sign in</a></main>`,
  styles: [`.denied{min-height:100vh;display:grid;place-content:center;text-align:center;gap:.75rem;background:var(--color-canvas);color:var(--color-text);padding:2rem}.denied p{color:var(--color-primary);font-size:1.1rem;font-weight:800;margin:0}.denied h1{font-size:clamp(2rem,6vw,4rem);margin:0}.denied span{color:var(--color-text-muted)}.denied a{background:var(--color-primary);border-radius:var(--radius-sm);color:var(--color-on-primary);font-weight:700;padding:.75rem 1rem;text-decoration:none}`],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessDeniedPage {}
