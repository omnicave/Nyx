using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace orleansdata.models.dao;

[Table("product")]
public class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("description")]
    public string Description { get; set; }
}

[Table("order")]
public class Order
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    
    [Column("productId")]
    public Product Product { get; set; }
    public Guid ProductId { get; set; }
    
    [Column("count")]
    public int Count { get; set; }
}

[Table("orderEvents")]
public class OrderEvents
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    
    [Column("orderId")]
    public Order Order { get; set; }
    public Guid OrderId { get; set; }
    
    [Column("type")]
    public OrderEventType Type { get; set; }
    
    [Column("eventDateTime")]
    public DateTimeOffset DateTime { get; set; }
    
    [Column("notes")]
    public string Notes { get; set; }
}

public enum OrderEventType
{
    Created,
    Processing,
    Processed,
    WaitingForPickup,
    Delivering,
    Delivered,
}

