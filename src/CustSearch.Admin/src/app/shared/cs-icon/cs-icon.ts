import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Lightweight inline SVG icon set; it avoids a font dependency while keeping controls consistent. */
@Component({
  selector: 'app-cs-icon',
  template: `<svg class="cs-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    @switch (name()) {
      @case ('dashboard') { <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/> }
      @case ('users') { <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/> }
      @case ('household') { <path d="m3 11 9-8 9 8"/><path d="M5 10v10h14V10M9 20v-5h6v5"/><circle cx="12" cy="10" r="1"/> }
      @case ('visits') { <circle cx="12" cy="12" r="8"/><path d="M12 8v4l3 2"/> }
      @case ('party') { <circle cx="8" cy="8" r="3"/><circle cx="16" cy="8" r="3"/><path d="M2.5 20a5.5 5.5 0 0 1 11 0M10.5 20a5.5 5.5 0 0 1 11 0"/> }
      @case ('store') { <path d="M3 10h18M5 10v10h14V10M4 10l1-6h14l1 6M8 20v-5h8v5"/> }
      @case ('staff') { <circle cx="12" cy="8" r="3"/><path d="M5 21a7 7 0 0 1 14 0M3 4h3M18 4h3M4 1v6M20 1v6"/> }
      @case ('billing') { <rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 10h18M7 15h3"/> }
      @case ('alert') { <path d="M10.3 4.3 2.5 18a2 2 0 0 0 1.7 3h15.6a2 2 0 0 0 1.7-3L13.7 4.3a2 2 0 0 0-3.4 0Z"/><path d="M12 9v4M12 17h.01"/> }
      @case ('camera') { <path d="M4 7h3l2-2h6l2 2h3a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2Z"/><circle cx="12" cy="13" r="3"/> }
      @case ('live') { <circle cx="12" cy="12" r="8"/><circle cx="12" cy="12" r="3"/><path d="M4 4 2 2M20 4l2-2M4 20l-2 2M20 20l2 2"/> }
      @case ('report') { <path d="M6 3h9l3 3v15H6zM9 12h6M9 16h6M9 8h3"/> }
      @case ('settings') { <circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .34 1.88l.06.06-1.7 1.7-.06-.06a1.7 1.7 0 0 0-1.88-.34 1.7 1.7 0 0 0-1.03 1.56V20h-2.4v-.2a1.7 1.7 0 0 0-1.03-1.56 1.7 1.7 0 0 0-1.88.34l-.06.06-1.7-1.7.06-.06A1.7 1.7 0 0 0 8.4 15a1.7 1.7 0 0 0-1.56-1.03H6v-2.4h.2A1.7 1.7 0 0 0 7.76 10a1.7 1.7 0 0 0-.34-1.88l-.06-.06 1.7-1.7.06.06A1.7 1.7 0 0 0 11 6.1 1.7 1.7 0 0 0 12.03 4.5V4h2.4v.2A1.7 1.7 0 0 0 15.46 5.76a1.7 1.7 0 0 0 1.88-.34l.06-.06 1.7 1.7-.06.06A1.7 1.7 0 0 0 18.7 9a1.7 1.7 0 0 0 1.56 1.03h.2v2.4h-.2A1.7 1.7 0 0 0 19.4 15Z"/> }
      @case ('integration') { <path d="M8 12h8M12 8v8M6 4h12v4H6zM6 16h12v4H6z"/> }
      @case ('voice') { <rect x="9" y="3" width="6" height="11" rx="3"/><path d="M5 11a7 7 0 0 0 14 0M12 18v3M8 21h8"/> }
      @case ('security') { <path d="M12 3 20 6v5c0 5-3.4 8.3-8 10-4.6-1.7-8-5-8-10V6z"/><path d="m8.5 12 2.2 2.2 4.8-5"/> }
      @case ('tenant') { <path d="M4 21V4l8-2 8 2v17M2 21h20M8 7h1M15 7h1M8 11h1M15 11h1M8 15h1M15 15h1"/> }
      @case ('operations') { <path d="M4 19V9M10 19V5M16 19v-8M22 19V3"/> }
    }
  </svg>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CsIcon { readonly name = input.required<string>(); }
