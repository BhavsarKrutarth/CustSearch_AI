import { AlertRealtimeEventV1, AlertView } from './alerts-api.service';
import { AlertEventDeduplicator } from './alerts-realtime.service';

describe('AlertEventDeduplicator',()=>{
  it('suppresses duplicate live/recovery delivery and advances the cursor',()=>{const dedupe=new AlertEventDeduplicator();expect(dedupe.accept(event(12))).toBe(true);expect(dedupe.accept(event(12))).toBe(false);expect(dedupe.accept(event(13))).toBe(true);expect(dedupe.cursor).toBe(13);});
  it('rejects invalid event versions and resets tenant-sensitive state',()=>{const dedupe=new AlertEventDeduplicator();expect(dedupe.accept({...event(2),contractVersion:2} as unknown as AlertRealtimeEventV1)).toBe(false);expect(dedupe.accept(event(3))).toBe(true);dedupe.reset();expect(dedupe.cursor).toBe(0);expect(dedupe.accept(event(3))).toBe(true);});
  const event=(eventId:number):AlertRealtimeEventV1=>({eventId,eventType:'alert.created',contractVersion:1,occurredUtc:'2026-08-24T15:00:00Z',tenantId:4,storeId:7,correlationId:'p11-test',alert:alert(eventId)});
  const alert=(id:number):AlertView=>({id,alertType:'vip.customer',storeId:7,severity:2,title:'VIP customer',message:'Customer returned.',entityType:'Customer',entityId:'9',createdUtc:'2026-08-24T15:00:00Z',acknowledgedUtc:null,acknowledgedByUserId:null,resolvedUtc:null,status:1,correlationId:'p11-test',deduplicationKey:`vip-${id}`});
});
