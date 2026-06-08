namespace Order.Domain.Enums;

public enum OrderStatus
{
    Pending, 
    CheckingUser, 
    PaymentProcessing, 
    Paid, 
    Cancelled
}