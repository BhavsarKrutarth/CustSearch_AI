import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CustomerDashboard } from './customer-dashboard';
describe('CustomerDashboard',()=>{it('renders customer KPIs and accessible theme control',async()=>{await TestBed.configureTestingModule({imports:[CustomerDashboard],providers:[provideRouter([])]}).compileComponents();const fixture=TestBed.createComponent(CustomerDashboard);fixture.detectChanges();const el=fixture.nativeElement as HTMLElement;expect(el.querySelectorAll('app-stat-card')).toHaveLength(4);expect(el.querySelector('select[aria-label="Color theme"]')).toBeTruthy();expect(el.textContent).toContain('Total Customers');});});
