namespace CustSearch.Domain.Enums;

public enum OperationalScope:byte { Platform=1,Tenant=2,Store=3 }
public enum RetentionDomain:byte { Alerts=1,IntegrationLogs=2,Exports=3,CctvOperationalData=4,RecognitionData=5,Audit=6,TemporaryEvidence=7 }

