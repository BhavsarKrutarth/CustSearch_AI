namespace CustSearch.Domain.Enums;

public enum CameraStatus { Offline=1,Online=2,Degraded=3,Maintenance=4 }
public enum CameraDirection { Entry=1,Exit=2,Bidirectional=3,Internal=4 }
public enum CameraZoneType { Entry=1,Exit=2,Checkout=3,Shelf=4,Category=5,Restricted=6,StaffArea=7,HighValue=8,BlindLowConfidence=9,Custom=10 }
public enum PersonTrackingState { Active=1,Handoff=2,Ended=3,Lost=4 }
public enum CameraPreviewSessionStatus { Active=1,Ended=2,Expired=3 }
public enum TrackingSubjectKind { Anonymous=1,Customer=2,Staff=3 }
public enum CameraOperationalEventStatus { Accepted=1,Processed=2,Duplicate=3,Rejected=4 }
