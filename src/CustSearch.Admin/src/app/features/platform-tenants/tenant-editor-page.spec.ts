import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PlatformTenantApiService } from './platform-tenant-api.service';
import { TenantEditorPage } from './tenant-editor-page';

describe('TenantEditorPage',()=>{it('blocks custom quotas until an audit reason is supplied',async()=>{const create=vi.fn();const api={plans:()=>of([]),create};await TestBed.configureTestingModule({imports:[TenantEditorPage],providers:[provideRouter([]),{provide:ActivatedRoute,useValue:{snapshot:{paramMap:{get:()=>null}}}},{provide:PlatformTenantApiService,useValue:api}]}).compileComponents();const fixture=TestBed.createComponent(TenantEditorPage);fixture.detectChanges();const component=fixture.componentInstance as unknown as {form:FormGroup;save():void;error:()=>string};component.form.patchValue({legalName:'North Retail Ltd',displayName:'North',primaryContactName:'Asha',primaryEmail:'asha@example.test',countryCode:'IN',timeZone:'Asia/Kolkata',currencyCode:'INR',maxStores:'12',auditReason:''});component.save();expect(create).not.toHaveBeenCalled();expect(component.form.get('auditReason')?.hasError('auditReasonRequired')).toBe(true);expect(component.error()).toContain('audit reason');});});
