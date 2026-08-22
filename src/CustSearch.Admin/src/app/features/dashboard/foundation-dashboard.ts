import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-foundation-dashboard',
  templateUrl: './foundation-dashboard.html',
  styleUrl: './foundation-dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FoundationDashboard {
  protected readonly capabilities = [
    'ASP.NET Core 8 API and Worker',
    'Angular standalone application',
    'EF Core and Dapper SQL infrastructure',
    'Structured correlation-aware logging',
  ];
}
