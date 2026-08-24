/** Extracts a safe message from unknown HTTP/client failures without using untyped any. */
export function preferenceErrorMessage(error:unknown,fallback:string):string{
  if(typeof error==='object'&&error!==null){
    const record=error as Record<string,unknown>;
    const nested=record['error'];
    if(typeof nested==='object'&&nested!==null){const message=(nested as Record<string,unknown>)['message'];if(typeof message==='string'&&message.trim())return message;}
    const message=record['message'];if(typeof message==='string'&&message.trim())return message;
  }
  return fallback;
}
