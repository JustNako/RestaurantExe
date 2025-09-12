using System;
using System.Collections.Generic;

namespace Restaurant.ViewModels
{
    public class ZReportViewModel
    {
        public DateTime ShiftStart { get; set; }
        public DateTime ShiftEnd { get; set; }

        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public decimal CashPayments { get; set; }       // временно как количество
        public decimal CardPayments { get; set; }       // если будет тип оплаты

        public decimal MixedCashPayments { get; set; }   // сумма наличными внутри смешанных
        public decimal MixedCardPayments { get; set; }   // сумма картой внутри смешанных
        public decimal MixedTotalPayments { get; set; }  // суммарно смешанные

        public decimal TotalDiscounts { get; set; } // если будет реализовано

        public List<string> Notes { get; set; } = new(); // служебные действия, пока заглушка
    }
}
