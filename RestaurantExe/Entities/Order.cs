using System;
using System.Collections.Generic;

namespace Restaurant.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public Table Table { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsClosed { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        public PaymentType PaymentType { get; set; }
        public decimal PaidCash { get; set; } = 0m;
        public decimal PaidCard { get; set; } = 0m;
        public decimal Discount { get; set; } = 0; // в тенге или %
        public decimal TotalPrice { get; set; }

        public int? ShiftId { get; set; }
        public Shift? Shift { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.New; // Новый статус по умолчанию

        //public Place Place { get; set; }

    }

   

    public enum PaymentType
    {
        Cash,
        Card,
        Online,
        Mixed
    }

    public enum OrderStatus
    {
        New,        
        Completed,
        Cancelled // Новый статус
    }


}

