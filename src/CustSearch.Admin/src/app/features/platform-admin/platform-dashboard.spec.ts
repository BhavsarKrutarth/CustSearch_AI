import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PlatformTenantApiService } from '../platform-tenants/platform-tenant-api.service';
import { PlatformDashboard } from './platform-dashboard';

describe('PlatformDashboard',()=>{it('renders authoritative API metrics',async()=>{const api={dashboard:()=>of({totalTenants:12,activeTenants:8,trialTenants:2,suspendedTenants:1,inactiveTenants:1,monthlyRecurringRevenue:45000,totalTenantUsers:250,totalCameras:80})};await TestBed.configureTestingModule({imports:[PlatformDashboard],providers:[provideRouter([]),{provide:PlatformTenantApiService,useValue:api}]}).compileComponents();const fixture=TestBed.createComponent(PlatformDashboard);fixture.detectChanges();const text=(fixture.nativeElement as HTMLElement).textContent??'';expect(text).toContain('12');expect(text).toContain('45,000');expect(text).toContain('250');});});
