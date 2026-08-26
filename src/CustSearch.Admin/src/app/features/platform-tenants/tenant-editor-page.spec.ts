import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { NEVER, of } from 'rxjs';
import { PlatformTenantApiService } from './platform-tenant-api.service';
import { TenantEditorPage } from './tenant-editor-page';

describe('TenantEditorPage',()=>{
  it('blocks custom quotas until an audit reason is supplied',async()=>{
    const create=vi.fn();const api={plans:()=>of([]),create};
    await TestBed.configureTestingModule({imports:[TenantEditorPage],providers:[provideRouter([]),{provide:ActivatedRoute,useValue:{snapshot:{paramMap:{get:()=>null}}}},{provide:PlatformTenantApiService,useValue:api}]}).compileComponents();
    const fixture=TestBed.createComponent(TenantEditorPage);fixture.detectChanges();
    const component=fixture.componentInstance as unknown as {form:FormGroup;save():void;error:()=>string};
    component.form.patchValue({legalName:'North Retail Ltd',displayName:'North',primaryContactName:'Asha',primaryEmail:'asha@example.test',countryCode:'IN',timeZone:'Asia/Kolkata',currencyCode:'INR',maxStores:'12',auditReason:'',adminUserName:'asha.owner',adminPassword:'InitialPass123',confirmAdminPassword:'InitialPass123'});
    component.save();
    expect(create).not.toHaveBeenCalled();expect(component.form.get('auditReason')?.hasError('auditReasonRequired')).toBe(true);expect(component.error()).toContain('audit reason');
  });

  it('enables password reset after valid matching input even while administrator lookup is pending',async()=>{
    const tenant={id:7,version:'version',legalName:'North Retail Ltd',displayName:'North',primaryContactName:'Asha',primaryEmail:'asha@example.test',primaryMobile:null,countryCode:'IN',timeZone:'Asia/Kolkata',currencyCode:'INR'};
    const api={plans:()=>of([]),get:()=>of(tenant),administrator:()=>NEVER};
    await TestBed.configureTestingModule({imports:[TenantEditorPage],providers:[provideRouter([]),{provide:ActivatedRoute,useValue:{snapshot:{paramMap:{get:()=> '7'}}}},{provide:PlatformTenantApiService,useValue:api}]}).compileComponents();
    const fixture=TestBed.createComponent(TenantEditorPage);fixture.detectChanges();
    const component=fixture.componentInstance as unknown as {passwordForm:FormGroup};
    component.passwordForm.patchValue({newPassword:'ReplacementPass456',confirmNewPassword:'ReplacementPass456'});fixture.detectChanges();
    const submitButtons=fixture.nativeElement.querySelectorAll('button[type="submit"]') as NodeListOf<HTMLButtonElement>;
    expect(submitButtons.item(submitButtons.length-1).disabled).toBe(false);
  });
});
