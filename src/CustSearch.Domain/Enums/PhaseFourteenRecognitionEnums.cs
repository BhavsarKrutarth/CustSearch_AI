namespace CustSearch.Domain.Enums;

public enum RecognitionConsentType:byte{BiometricRecognition=1}
public enum BiometricTemplateStatus:byte{Active=1,Disabled=2,Deleted=3}
public enum RecognitionCandidateStatus:byte{PendingReview=1,Ambiguous=2,Accepted=3,Rejected=4,Expired=5}
