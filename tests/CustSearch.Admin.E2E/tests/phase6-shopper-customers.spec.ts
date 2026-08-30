import { expect, Page, Route, test } from '@playwright/test';

function jwtWithFutureExpiry(): string {
  const header = Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url');
  const payload = Buffer.from(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600 })).toString('base64url');
  return `${header}.${payload}.signature`;
}

const phaseSixPermissions = [
  'TenantDashboard.View',
  'Customers.View', 'Customers.Create', 'Customers.Edit',
  'Visitors.View', 'Visitors.Convert',
];

interface CustomerState {
  id:number; customerCode:string; firstName:string; lastName:string|null; mobile:string|null; email:string|null;
  notes:string|null; isActive:boolean; storeIds:number[]; primaryStoreId:number|null; createdUtc:string; updatedUtc:string;
}
interface VisitorState {
  id:number; visitorCode:string; storeId:number; firstSeenUtc:string; lastSeenUtc:string; isActive:boolean;
  convertedCustomerId:number|null; convertedUtc:string|null; createdUtc:string; updatedUtc:string;
}
interface MockState { customers:CustomerState[]; visitors:VisitorState[]; calls:string[]; bodies:unknown[]; }

const json = (route:Route, body:unknown, status=200) => route.fulfill({ status, contentType:'application/json', body:JSON.stringify(body) });

function identity(overrides:Record<string,unknown>={}) {
  return { userId:501, tenantId:25, tenantCode:'DEMO-STORE', userName:'owner', displayName:'Demo Shop Owner',
    email:'owner@example.test', isPlatformAdmin:false, roles:['TenantAdmin'], permissions:phaseSixPermissions, storeIds:[101], ...overrides };
}

async function mockPhaseSixApi(page:Page, user=identity()):Promise<MockState> {
  const state:MockState={
    customers:[{id:901,customerCode:'CUST-001',firstName:'Priya',lastName:'Shah',mobile:'9876543210',email:'priya@example.test',notes:'Existing CRM customer',isActive:true,storeIds:[101],primaryStoreId:101,createdUtc:'2026-08-23T08:00:00Z',updatedUtc:'2026-08-23T09:00:00Z'}],
    visitors:[{id:1001,visitorCode:'VIS-001',storeId:101,firstSeenUtc:'2026-08-23T09:00:00Z',lastSeenUtc:'2026-08-23T09:10:00Z',isActive:true,convertedCustomerId:null,convertedUtc:null,createdUtc:'2026-08-23T09:00:00Z',updatedUtc:'2026-08-23T09:10:00Z'}],
    calls:[],bodies:[],
  };

  await page.route('**/api/auth/login', route=>json(route,{accessToken:jwtWithFutureExpiry(),accessTokenExpiresUtc:new Date(Date.now()+3_600_000).toISOString(),user}));
  await page.route('**/api/auth/refresh', route=>json(route,{accessToken:jwtWithFutureExpiry(),accessTokenExpiresUtc:new Date(Date.now()+3_600_000).toISOString(),user}));
  await page.route('**/api/auth/me', route=>json(route,{user,accessTokenExpiresUtc:new Date(Date.now()+3_600_000).toISOString()}));
  await page.route('**/api/tenant/**', async route=>{
    const req=route.request(); const url=new URL(req.url()); const path=url.pathname.replace('/api/tenant/',''); const method=req.method();
    state.calls.push(`${method} ${path}${url.search}`);
    if(method!=='GET') state.bodies.push(req.postDataJSON());

    if(path==='dashboard/summary'&&method==='GET') return json(route,{activeUsers:2,activeStores:1,activeStaff:1,activeCategories:2,openShifts:0,activePresenceSessions:0});
    if(path==='customers'&&method==='GET') return json(route,{data:state.customers,pageNumber:Number(url.searchParams.get('pageNumber')??1),pageSize:Number(url.searchParams.get('pageSize')??25),totalCount:state.customers.length,totalPages:1});
    if(path==='customers'&&method==='POST'){
      const body=req.postDataJSON() as {customerCode:string|null;firstName:string;lastName:string|null;mobile:string|null;email:string|null;notes:string|null;storeIds:number[];primaryStoreId:number|null};
      const created:CustomerState={id:902,customerCode:body.customerCode||'CUST-AUTO-002',firstName:body.firstName,lastName:body.lastName,mobile:body.mobile,email:body.email,notes:body.notes,isActive:true,storeIds:body.storeIds,primaryStoreId:body.primaryStoreId,createdUtc:new Date().toISOString(),updatedUtc:new Date().toISOString()};
      state.customers.push(created); return json(route,created,201);
    }
    const smart=path.match(/^customers\/(\d+)\/smart-profile$/);
    if(smart&&method==='GET'){
      const customer=state.customers.find(x=>x.id===Number(smart[1]))!;
      return json(route,{customer,convertedAnonymousVisitorCount:1,lastAnonymousVisitorSeenUtc:'2026-08-23T09:10:00Z',hasMobile:true,hasEmail:true,availableSections:['Identity','Contact','Store visibility','Anonymous visitor conversions'],plannedEnrichmentSections:['Households (Phase 7)','Visits (Phase 7)','Purchase history (Phase 8)','Preferences (Phase 10)']});
    }
    const customer=path.match(/^customers\/(\d+)$/);
    if(customer&&method==='GET') return json(route,state.customers.find(x=>x.id===Number(customer[1]))!);
    if(customer&&method==='PUT'){
      const target=state.customers.find(x=>x.id===Number(customer[1]))!; Object.assign(target,req.postDataJSON(),{updatedUtc:new Date().toISOString()}); return json(route,target);
    }
    const stores=path.match(/^customers\/(\d+)\/stores$/);
    if(stores&&method==='PUT'){
      const target=state.customers.find(x=>x.id===Number(stores[1]))!; const body=req.postDataJSON() as {storeIds:number[];primaryStoreId:number|null}; target.storeIds=body.storeIds;target.primaryStoreId=body.primaryStoreId;return json(route,target);
    }
    if(path==='visitors'&&method==='GET') return json(route,{data:state.visitors,pageNumber:1,pageSize:25,totalCount:state.visitors.length,totalPages:1});
    const convert=path.match(/^visitors\/(\d+)\/convert$/);
    if(convert&&method==='POST'){
      const visitor=state.visitors.find(x=>x.id===Number(convert[1]))!; const body=req.postDataJSON() as {customerId:number|null;firstName:string|null;lastName:string|null;mobile:string|null;email:string|null;notes:string|null};
      let target=body.customerId?state.customers.find(x=>x.id===body.customerId):undefined;
      if(!target){target={id:903,customerCode:'CUST-CONVERTED',firstName:body.firstName??'Converted',lastName:body.lastName,mobile:body.mobile,email:body.email,notes:body.notes,isActive:true,storeIds:[visitor.storeId],primaryStoreId:visitor.storeId,createdUtc:new Date().toISOString(),updatedUtc:new Date().toISOString()};state.customers.push(target);}
      visitor.convertedCustomerId=target!.id;visitor.convertedUtc=new Date().toISOString();visitor.isActive=false;return json(route,target!);
    }
    return json(route,{message:`Unhandled Phase 6 E2E route ${method} ${path}`},501);
  });
  return state;
}

async function signIn(page:Page):Promise<void>{
  await page.goto('/login'); await page.getByLabel('Tenant code').fill('DEMO-STORE'); await page.getByLabel('Email or username').fill('owner'); await page.getByLabel('Password').fill('safe-e2e-password'); await page.getByRole('button',{name:'Sign in'}).click();
  await expect(page).toHaveURL(/\/customer-admin\/dashboard$/); await expect(page.locator('app-phase-five-dashboard')).toBeVisible();
}
async function openDashboardLink(page:Page,name:'Customers'|'Visitors'):Promise<void>{
  await page.locator('app-phase-five-dashboard main nav.nav').getByRole('link',{name,exact:true}).click();
}

test('customer search and create use tenant-safe API without browser TenantId',async({page})=>{
  const state=await mockPhaseSixApi(page);await signIn(page);await openDashboardLink(page,'Customers');
  await expect(page).toHaveURL(/\/customer-admin\/customers$/);await expect(page.getByText('CUST-001',{exact:true})).toBeVisible();
  await page.getByPlaceholder('Search code, name, mobile or email').fill('Priya');await page.getByRole('button',{name:'Search'}).click();
  await page.getByPlaceholder('First name').fill('Ravi');await page.getByPlaceholder('Last name').fill('Patel');await page.locator('input[formcontrolname="mobile"]').fill('9000000002');await page.getByPlaceholder('Store IDs comma separated').fill('101');await page.getByPlaceholder('Primary store ID').fill('101');await page.getByRole('button',{name:'Create customer'}).click();
  await expect(page.getByText('Customer created.',{exact:true})).toBeVisible();await expect.poll(()=>state.customers.length).toBe(2);
  expect(state.calls.some(x=>/tenantId/i.test(x))).toBe(false);expect(JSON.stringify(state.bodies)).not.toMatch(/tenantId/i);
});

test('smart profile edits factual CRM fields and customer store visibility',async({page})=>{
  const state=await mockPhaseSixApi(page);await signIn(page);await openDashboardLink(page,'Customers');
  await page.getByRole('link',{name:'CUST-001',exact:true}).click();await expect(page.getByRole('heading',{name:'Smart Customer Profile'})).toBeVisible();
  await expect(page.getByRole('heading',{name:'Available now'})).toBeVisible();
  await page.getByPlaceholder('First name').fill('Priya Updated');await page.getByRole('button',{name:'Save profile'}).click();await expect(page.getByText('Customer profile saved.',{exact:true})).toBeVisible();
  await page.getByPlaceholder('Store IDs comma separated').fill('101');await page.getByPlaceholder('Primary store ID').fill('101');await page.getByRole('button',{name:'Save stores'}).click();await expect(page.getByText('Customer store visibility saved.',{exact:true})).toBeVisible();
  await expect.poll(()=>state.customers[0].firstName).toBe('Priya Updated');
});

test('anonymous visitor conversion is explicit and creates a customer only after operator action',async({page})=>{
  const state=await mockPhaseSixApi(page);await signIn(page);await openDashboardLink(page,'Visitors');
  await expect(page.getByText('VIS-001',{exact:true})).toBeVisible();await page.getByRole('button',{name:'Convert'}).click();
  await page.getByPlaceholder('New customer first name').fill('Neha');await page.getByPlaceholder('Last name').fill('Mehta');await page.getByRole('button',{name:'Convert visitor'}).click();
  await expect(page.getByText('Visitor converted to CUST-CONVERTED.',{exact:true})).toBeVisible();await expect.poll(()=>state.visitors[0].convertedCustomerId).toBe(903);
});

test('customer route permission guard denies a tenant user without Customers.View',async({page})=>{
  await mockPhaseSixApi(page,identity({roles:['StoreManager'],permissions:['TenantDashboard.View','Visitors.View'],storeIds:[101]}));await signIn(page);
  await page.goto('/customer-admin/customers');await expect(page).toHaveURL(/\/access-denied$/);await expect(page.getByText(/access denied/i).first()).toBeVisible();
});

test('store-scoped customer list trusts server-authorized result set and never requests another tenant',async({page})=>{
  const state=await mockPhaseSixApi(page,identity({roles:['StoreManager'],permissions:['TenantDashboard.View','Customers.View'],storeIds:[101]}));
  state.customers=[state.customers[0]];await signIn(page);await openDashboardLink(page,'Customers');await expect(page.getByText('Priya Shah',{exact:true})).toBeVisible();await expect(page.getByText('Forbidden Customer',{exact:true})).toHaveCount(0);
  expect(state.calls.some(x=>/tenantId/i.test(x))).toBe(false);
});
