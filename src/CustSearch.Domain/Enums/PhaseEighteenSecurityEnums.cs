namespace CustSearch.Domain.Enums;

public enum SecurityObservationType:byte
{
    PersonEntry=1,PersonExit=2,ShelfInteraction=3,ProbablePickup=4,ProbablePutBack=5,
    CheckoutZoneVisit=6,TrackContinuity=7,OcclusionQuality=8,ProbableItemAssociation=9,RfidEasSignal=10,
}

public enum SecurityIncidentStatus:byte
{
    Observed=1,Candidate=2,Alerted=3,Acknowledged=4,UnderReview=5,
    ConfirmedLoss=6,FalsePositive=7,Resolved=8,Archived=9,
}

public enum SecuritySeverity:byte{Low=1,Medium=2,High=3,Critical=4,Emergency=5}
public enum SecurityPaymentMatchStatus:byte{NotChecked=1,NoMatch=2,PartialMatch=3,PaidMatch=4,CancelledOrRefunded=5}
public enum SecurityNotificationStatus:byte{Queued=1,Processing=2,Sent=3,Failed=4,DeadLetter=5}
