using System;
using System.Collections.Generic;

namespace Restaurant.Entities
{
    public class Order
    {
        public int Id { get; set; }

        // --- Связи ---
        public int? TableId { get; set; }
        public Table? Table { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        // --- Дата / статус ---
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsClosed { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.New;
        public Place Place { get; set; }

        // --- Оплата ---
        public PaymentType PaymentType { get; set; }
        public decimal PaidCash { get; set; }
        public decimal PaidCard { get; set; }

        // --- Финансы (ВАЖНО) ---
        public decimal ItemsTotal { get; set; }      // сумма товаров
        public decimal ServicePercent { get; set; }  // % сервиса
        public decimal ServiceAmount { get; set; }   // сумма сервиса
        public decimal Discount { get; set; }        // скидка (₸)
        public decimal TotalAmount { get; set; }     // К ОПЛАТЕ

        // --- Смена ---
        public int? ShiftId { get; set; }
        public Shift? Shift { get; set; }
    }




    public enum Place
    {
        DineIn,
        Takeaway,
        Delivery
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

