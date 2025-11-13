namespace PaymentGateway.Domain.Enums;

public enum AcquiringStatus
{
    Authorized,     // 200 OK + authorized: true
    Declined,       // 200 OK + authorized: false
    BadRequest,     // 400
    Unavailable     // 503
}
