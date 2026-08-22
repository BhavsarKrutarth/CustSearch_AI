import { ChangeDetectionStrategy, Component } from '@angular/core';
import { AdminShell } from '../../shared/admin-shell/admin-shell';
import { StatCard } from '../../shared/stat-card/stat-card';

@Component({ selector: 'app-customer-dashboard', imports: [AdminShell, StatCard], templateUrl: './customer-dashboard.html', styleUrl: './customer-dashboard.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class CustomerDashboard {
  protected readonly stats = [
    { label:'Total Sales', value:'₹286,540', trend:'+12.6%', icon:'₹' },
    { label:'Total Customers', value:'12,540', trend:'+8.3%', icon:'♙' },
    { label:'Households', value:'4,128', trend:'+4.8%', icon:'⌂' },
    { label:'Avg. Order Value', value:'₹72.46', trend:'+6.4%', icon:'◇' },
  ];
  protected readonly customers = [
    ['Amit Sharma','Premium','₹12,540','Active'], ['Priya Mehta','Loyal','₹8,700','Active'], ['Rajiv Verma','Regular','₹4,900','Active'], ['Neha Kapoor','Prospect','₹1,230','Pending'], ['Suresh Reddy','Regular','₹3,300','Inactive'],
  ];
}
