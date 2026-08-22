import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';
import { AuthSessionService } from '../../core/auth/auth-session.service';

/** Renders an action template only when the in-memory server session contains its permission. */
@Directive({ selector: '[appHasPermission]' })
export class HasPermissionDirective {
  readonly appHasPermission = input.required<string>();
  private readonly session = inject(AuthSessionService);
  private readonly template = inject(TemplateRef<unknown>);
  private readonly container = inject(ViewContainerRef);
  private rendered = false;

  constructor() {
    effect(() => {
      const allowed = this.session.hasPermission(this.appHasPermission());
      if (allowed && !this.rendered) {
        this.container.createEmbeddedView(this.template);
        this.rendered = true;
      } else if (!allowed && this.rendered) {
        this.container.clear();
        this.rendered = false;
      }
    });
  }
}
