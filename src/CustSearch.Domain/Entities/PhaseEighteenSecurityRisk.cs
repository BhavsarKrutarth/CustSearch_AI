using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

public sealed record SecurityRiskRule(
    int Version,decimal RiskThreshold,decimal HighValueThreshold,int CheckoutCorrelationWindowMinutes,
    int ExitGracePeriodSeconds,decimal ExitWeight=25m,decimal PossessionWeight=25m,
    decimal CheckoutAbsenceWeight=15m,decimal PaymentMismatchWeight=25m,decimal HighValueWeight=10m,
    decimal RfidWeight=10m,decimal OcclusionPenaltyWeight=20m,decimal CameraGapPenaltyWeight=20m);

public sealed record SecurityRiskSignals(
    bool ExitObserved,bool ProbablePossession,bool PutBackObserved,bool CheckoutVisited,bool PaidMatch,
    bool StaffHandling,bool GroupHandoff,bool CrowdedAisle,bool ReEntry,bool RfidEasSignal,
    decimal ObservationConfidence,decimal OcclusionQuality,int CameraGapSeconds,decimal?ProductValue);

public sealed record SecurityRiskEvaluation(
    decimal Score,SecuritySeverity Severity,bool Candidate,bool Suppressed,string Reason);

/// <summary>Authoritative, versioned server rule engine. AI and Angular never calculate this score.</summary>
public static class SecurityRiskEngine
{
    public static SecurityRiskEvaluation Evaluate(SecurityRiskRule rule,SecurityRiskSignals signals)
    {
        ArgumentNullException.ThrowIfNull(rule);ArgumentNullException.ThrowIfNull(signals);
        if(rule.Version<=0||rule.RiskThreshold is<0 or>100||signals.ObservationConfidence is<0 or>1||signals.OcclusionQuality is<0 or>1)throw new ArgumentOutOfRangeException(nameof(rule));
        if(!signals.ExitObserved)return Result(0,false,"No exit crossing was observed.");
        if(signals.PutBackObserved)return Result(0,true,"A probable put-back followed the pickup signal.");
        if(signals.StaffHandling)return Result(0,true,"Authorized staff handling/restocking signal suppressed the candidate.");
        if(signals.PaidMatch)return Result(0,true,"A compatible paid checkout suppressed the candidate.");

        decimal score=rule.ExitWeight;
        if(signals.ProbablePossession)score+=rule.PossessionWeight*signals.ObservationConfidence;
        if(!signals.CheckoutVisited)score+=rule.CheckoutAbsenceWeight;
        score+=rule.PaymentMismatchWeight;
        if(signals.ProductValue>=rule.HighValueThreshold)score+=rule.HighValueWeight;
        if(signals.RfidEasSignal)score+=rule.RfidWeight;
        score-=rule.OcclusionPenaltyWeight*(1-signals.OcclusionQuality);
        if(signals.CameraGapSeconds>0)score-=Math.Min(rule.CameraGapPenaltyWeight,signals.CameraGapSeconds/5m);
        if(signals.GroupHandoff)score-=15m;
        if(signals.CrowdedAisle)score-=10m;
        if(signals.ReEntry)score-=15m;
        score=Math.Clamp(decimal.Round(score,3),0,100);
        var severity=score switch{>=90=>SecuritySeverity.Critical,>=75=>SecuritySeverity.High,>=50=>SecuritySeverity.Medium,_=>SecuritySeverity.Low};
        return new(score,severity,score>=rule.RiskThreshold,false,score>=rule.RiskThreshold?"Reviewable security candidate threshold reached.":"Risk remained below the configured threshold.");
    }

    private static SecurityRiskEvaluation Result(decimal score,bool suppressed,string reason)=>new(score,SecuritySeverity.Low,false,suppressed,reason);
}

public static class SecurityIncidentStateMachine
{
    public static bool CanTransition(SecurityIncidentStatus from,SecurityIncidentStatus to)=>from switch
    {
        SecurityIncidentStatus.Observed=>to==SecurityIncidentStatus.Candidate,
        SecurityIncidentStatus.Candidate=>to is SecurityIncidentStatus.Alerted or SecurityIncidentStatus.UnderReview or SecurityIncidentStatus.Resolved,
        SecurityIncidentStatus.Alerted=>to is SecurityIncidentStatus.Acknowledged or SecurityIncidentStatus.Resolved,
        SecurityIncidentStatus.Acknowledged=>to is SecurityIncidentStatus.UnderReview or SecurityIncidentStatus.Resolved,
        SecurityIncidentStatus.UnderReview=>to is SecurityIncidentStatus.ConfirmedLoss or SecurityIncidentStatus.FalsePositive or SecurityIncidentStatus.Resolved,
        SecurityIncidentStatus.ConfirmedLoss or SecurityIncidentStatus.FalsePositive or SecurityIncidentStatus.Resolved=>to==SecurityIncidentStatus.Archived,
        _=>false,
    };

    public static void RequireTransition(SecurityIncidentStatus from,SecurityIncidentStatus to,bool humanConfirmed,string?reason)
    {
        if(!CanTransition(from,to))throw new InvalidOperationException($"Transition from {from} to {to} is not allowed.");
        if(to==SecurityIncidentStatus.ConfirmedLoss&&!humanConfirmed)throw new InvalidOperationException("ConfirmedLoss requires an authorized human decision.");
        if((to is SecurityIncidentStatus.ConfirmedLoss or SecurityIncidentStatus.FalsePositive)&&string.IsNullOrWhiteSpace(reason))throw new InvalidOperationException("A review reason is required.");
    }
}
