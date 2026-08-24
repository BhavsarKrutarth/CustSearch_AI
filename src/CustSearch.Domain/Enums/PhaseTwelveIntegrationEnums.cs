namespace CustSearch.Domain.Enums;

public enum IntegrationType:byte { InboundWebhook=1,OutboundWebhook=2,Bidirectional=3,Api=4 }
public enum IntegrationInboundEventStatus:byte { Accepted=1,Processed=2,Failed=3 }
public enum IntegrationOutboxStatus:byte { Pending=1,Processing=2,Delivered=3,Failed=4,Retrying=5,DeadLetter=6 }
public enum IntegrationDirection:byte { Inbound=1,Outbound=2 }
public enum IntegrationDeliveryStatus:byte { Accepted=1,Delivered=2,Failed=3,Retrying=4,DeadLetter=5 }
