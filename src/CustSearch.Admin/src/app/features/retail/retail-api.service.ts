import { Injectable, inject } from '@angular/core';
import { PageQuery, PageResponse } from '../../core/auth/auth.models';
import { TenantApiClient } from '../../core/api/tenant-api.client';

export interface ProductListItem { id:number; productCode:string; barcode:string|null; name:string; categoryId:number; categoryName:string; brand:string|null; unitName:string; salePrice:number; taxPercent:number|null; isActive:boolean; }
export interface ProductDetail extends ProductListItem { description:string|null; costPrice:number|null; stores:{storeId:number;isActive:boolean}[]; createdUtc:string; updatedUtc:string; }
export interface ProductSaveRequest { productCode:string; barcode:string|null; name:string; description:string|null; categoryId:number; brand:string|null; unitName:string; salePrice:number; costPrice:number|null; taxPercent:number|null; storeIds?:number[]; isActive?:boolean; }

export type RetailInvoiceStatus=1|2|3|4|5;
export interface RetailInvoiceListItem { id:number; invoiceNumber:string; storeId:number; customerId:number|null; customerCode:string|null; customerName:string|null; invoiceUtc:string; grandTotal:number; paidAmount:number; balanceAmount:number; status:RetailInvoiceStatus; }
export interface RetailInvoiceItem { id:number; productId:number|null; productCode:string; productName:string; categoryId:number|null; categoryName:string|null; quantity:number; unitPrice:number; discountAmount:number; taxPercent:number; taxAmount:number; lineSubtotal:number; lineTotal:number; }
export interface RetailPayment { id:number; paymentReference:string; paymentMethod:number; amount:number; paymentUtc:string; status:number; externalTransactionId:string|null; notes:string|null; receivedByUserId:number; }
export interface RetailParticipant { customerId:number; customerCode:string; customerName:string; participationType:number; isPayer:boolean; }
export interface RetailAttribution { id:number; invoiceItemId:number; customerId:number; customerCode:string; customerName:string; attributionType:number; quantityAttributed:number|null; amountAttributed:number; source:number; createdByUserId:number; createdUtc:string; }
export interface RetailInvoiceDetail extends RetailInvoiceListItem { householdId:number|null; customerVisitId:number|null; visitPartyId:number|null; subtotal:number; discountAmount:number; taxAmount:number; notes:string|null; items:RetailInvoiceItem[]; payments:RetailPayment[]; participants:RetailParticipant[]; attributions:RetailAttribution[]; attributedTotal:number; unattributedTotal:number; createdUtc:string; updatedUtc:string; cancelledUtc:string|null; cancellationReason:string|null; }
export interface InvoiceItemInput { productId:number; quantity:number; discountAmount:number; }
export interface CreateInvoiceRequest { storeId:number; customerId:number|null; householdId:number|null; customerVisitId:number|null; visitPartyId:number|null; notes:string|null; items:InvoiceItemInput[]; }
export interface PurchaseHistory { customerId:number; invoiceCount:number; payerSpend:number; explicitAttributedSpend:number; lastPurchaseUtc:string|null; lastPurchaseStoreId:number|null; recentInvoices:{invoiceId:number;invoiceNumber:string;storeId:number;invoiceUtc:string;status:number;grandTotal:number;payerAmount:number;attributedAmount:number}[]; }
export interface HouseholdPurchaseSummary { householdId:number; invoiceCount:number; verifiedMemberAttributedSpend:number; lastPurchaseUtc:string|null; }
export interface RetailSalesSummary { grossSales:number; discounts:number; tax:number; netSales:number; paidAmount:number; outstandingAmount:number; invoiceCount:number; }
export interface RetailBreakdownItem { id:number; code:string; name:string; netSales:number; invoiceCount:number; }
export interface RetailPaymentSummaryItem { paymentMethod:number; amount:number; paymentCount:number; }

@Injectable({providedIn:'root'})
export class RetailApiService {
  private readonly api=inject(TenantApiClient);
  searchProducts(query:PageQuery){return this.api.getPage<ProductListItem>('products',query);}
  getProduct(id:number){return this.api.get<ProductDetail>(`products/${id}`);}
  createProduct(body:ProductSaveRequest){return this.api.post<ProductDetail>('products',body);}
  updateProduct(id:number,body:Omit<ProductSaveRequest,'productCode'|'storeIds'>){return this.api.put<ProductDetail>(`products/${id}`,body);}
  setProductStores(id:number,storeIds:number[]){return this.api.put<ProductDetail>(`products/${id}/stores`,{storeIds});}

  searchInvoices(query:PageQuery){return this.api.getPage<RetailInvoiceListItem>('retail/invoices',query);}
  getInvoice(id:number){return this.api.get<RetailInvoiceDetail>(`retail/invoices/${id}`);}
  createInvoice(body:CreateInvoiceRequest){return this.api.post<RetailInvoiceDetail>('retail/invoices',body);}
  updateInvoice(id:number,body:Omit<CreateInvoiceRequest,'storeId'>){return this.api.put<RetailInvoiceDetail>(`retail/invoices/${id}`,body);}
  finalizeInvoice(id:number){return this.api.post<RetailInvoiceDetail>(`retail/invoices/${id}/finalize`);}
  cancelInvoice(id:number,reason:string){return this.api.post<RetailInvoiceDetail>(`retail/invoices/${id}/cancel`,{reason});}
  addPayment(id:number,body:{paymentReference:string|null;paymentMethod:number;amount:number;paymentUtc:string|null;externalTransactionId:string|null;notes:string|null}){return this.api.post<RetailInvoiceDetail>(`retail/invoices/${id}/payments`,body);}
  saveParticipant(id:number,body:{customerId:number;participationType:number;isPayer:boolean}){return this.api.post<RetailInvoiceDetail>(`retail/invoices/${id}/participants`,body);}
  saveAttribution(id:number,body:{invoiceItemId:number;customerId:number;attributionType:number;quantityAttributed:number|null;amountAttributed:number;source:number}){return this.api.post<RetailInvoiceDetail>(`retail/invoices/${id}/attributions`,body);}

  customerPurchaseHistory(customerId:number){return this.api.get<PurchaseHistory>(`customers/${customerId}/purchase-history`);}
  householdPurchaseSummary(householdId:number){return this.api.get<HouseholdPurchaseSummary>(`households/${householdId}/purchase-summary`);}
  salesSummary(query=''){return this.api.get<RetailSalesSummary>(`retail/reports/summary${query}`);}
  salesByProduct(query=''){return this.api.get<RetailBreakdownItem[]>(`retail/reports/products${query}`);}
  salesByCategory(query=''){return this.api.get<RetailBreakdownItem[]>(`retail/reports/categories${query}`);}
  paymentSummary(query=''){return this.api.get<RetailPaymentSummaryItem[]>(`retail/reports/payments${query}`);}
}
