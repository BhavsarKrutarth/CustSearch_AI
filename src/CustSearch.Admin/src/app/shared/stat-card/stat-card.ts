import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({ selector: 'app-stat-card', template: `<article class="stat-card"><div class="icon" aria-hidden="true">{{ icon() }}</div><p>{{ label() }}</p><strong>{{ value() }}</strong><small [class.down]="trend().startsWith('-')">{{ trend() }} <span>vs last month</span></small></article>`, styleUrl: './stat-card.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class StatCard { readonly label=input.required<string>(); readonly value=input.required<string>(); readonly trend=input.required<string>(); readonly icon=input('◇'); }
