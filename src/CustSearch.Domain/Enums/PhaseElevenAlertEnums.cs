namespace CustSearch.Domain.Enums;

/// <summary>Operational importance used to order and present an alert.</summary>
public enum AlertSeverity : byte { Info=1, Warning=2, Critical=3 }

/// <summary>Authoritative alert lifecycle stored independently from delivery attempts.</summary>
public enum AlertStatus : byte { New=1, Delivered=2, Acknowledged=3, Resolved=4, Expired=5 }

/// <summary>Reliable notification delivery lifecycle for a single channel message.</summary>
public enum NotificationOutboxStatus : byte { Pending=1, Processing=2, Delivered=3, Failed=4, Retrying=5, DeadLetter=6 }
