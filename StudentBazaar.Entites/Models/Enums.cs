namespace StudentBazaar.Entities.Models
{
    public enum UserRole
    {
        Student,
        Shipper,
        Admin
    }

    public enum ListingCondition
    {
        New,
        Excellent,
        Good,
        Fair,
        Poor
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Shipped,
        Delivered,
        Cancelled,
        Completed
    }

    public enum PaymentMethod
    {
        Online,
        CashOnDelivery,
        PayPal,
        VodafoneCash,
        BankTransfer
    }

    public enum ShipmentStatus
    {
        AwaitingPickup,
        InTransit,
        Delivered,
        Delayed,
        Failed
    }

    public enum ListingStatus
    {
        Available,
        Sold,
        Hidden,
        Reserved
    }
}
